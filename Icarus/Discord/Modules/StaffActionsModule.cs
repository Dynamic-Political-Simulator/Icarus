using Discord;
using Icarus.Context;
using Icarus.Context.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Rest;
using Discord.WebSocket;
using Icarus.Discord.CustomPreconditions;
using Icarus.Services;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;

namespace Icarus.Discord.Modules
{
	[Group("staff-action", "Staff action commands")]
	public class StaffActionPublicModule : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly StaffActionService _staffActionService;
		private readonly DiscordSocketClient _client;

		public StaffActionPublicModule(StaffActionService s, DiscordSocketClient client)
		{
			_staffActionService = s;
			_client = client;
		}

        [SlashCommand("my-oldest", "Shows the oldest Staff Action assigned to you.")]
        // [RequireAdmin]
        public async Task ViewOldestAssignedStaffAction()
		{
            var sa = _staffActionService.GetMyOldestTodoStaffAction(Context.User.Id);

			if (sa == null) 
			{
				await RespondAsync("Could not find an SA assigned to you.", ephemeral: false);
				return;
			}

            var embedBuilder = _staffActionService.BuildStaffActionMessage(sa);

            await RespondAsync(embed: embedBuilder.Build(), ephemeral: false);
        }

        [SlashCommand("fetch", "Gets the SA by ID.")]
        // [RequireAdmin]
        public async Task Fetch(int id, bool ephemeral = true)
		{
            var sa = _staffActionService.GetStaffActionById(id);

			using var db = new IcarusContext();
			var runner = db.Users.SingleOrDefault(du => du.DiscordId == Context.User.Id.ToString());

			if (!runner.CanUseAdminCommands && sa.AssignedToId != Context.User.Id.ToString()) {
				await RespondAsync("You cannot view this Staff Action.", ephemeral: true);
				return;
			}

			if (!runner.CanUseAdminCommands && !ephemeral) {
				await RespondAsync("You can only run this command ephemerally.", ephemeral: true);
				return;
			}

            var embedBuilder = _staffActionService.BuildStaffActionMessage(sa);

            await RespondAsync(embed: embedBuilder.Build(), ephemeral: ephemeral);
        }

        [SlashCommand("oldest", "Shows the oldest Staff Action.")]
        [RequireAdmin]
        public async Task ViewOldestStaffAction()
		{
			var sa = _staffActionService.GetOldestTodoStaffAction();

			var embedBuilder = _staffActionService.BuildStaffActionMessage(sa);

			await RespondAsync(embed: embedBuilder.Build(), ephemeral: false);
		}

        [SlashCommand("todo", "Shows all todo and in progress Staff Actions.")]
        [RequireAdmin]
        public async Task ViewTodoStaffActions()
		{
			var result = _staffActionService.GetTodoStaffActions();

			var embedBuilder = new EmbedBuilder();

			var sb = new StringBuilder();

			foreach (var action in result)
			{
				var user = await _client.GetUserAsync(ulong.Parse(action.SubmitterId));
				sb.AppendLine($"{action.StaffActionId} - {action.Status} - Submitted by: {user.Username}");
			}

			embedBuilder.WithTitle("Staff Actions TODO");
			embedBuilder.WithDescription(sb.ToString());

			await RespondAsync(embed: embedBuilder.Build(), ephemeral: false);
		}

		[SlashCommand("add-channel", "Adds the current channel as the staff action channel")]
		[RequireAdmin]
		public async Task CreateStaffActionChannel()
		{
			using var db = new IcarusContext();
			var existingStaffActionChannel = db.StaffActionChannels.ToList();

			if (existingStaffActionChannel.Count != 0)
			{
				await RespondAsync("There's already an existing StaffAction channel.");
				return;
			}

			var newStaffActionChannel = new StaffActionChannel()
			{
				ChannelId = Context.Channel.Id.ToString(),
				ServerId = Context.Guild.Id.ToString()
			};

			await db.StaffActionChannels.AddAsync(newStaffActionChannel);
			await db.SaveChangesAsync();

			await RespondAsync("Staff Action Channel added!");
		}

		[SlashCommand("respond", "Responds to a staff action")]
		// [RequireAdmin]
		public async Task RespondToTask(
			[Summary("ID", "The id of action")]
			long id,
		[Summary("Status", "The status of action"),
			Choice("TODO", "TODO"),
			Choice("IN PROGRESS", "IN_PROGRESS"),
			Choice("DONE", "DONE")]
		string stringStatus,
		[Summary("Response", "The response to action")]
		string response)
		{
			using var db = new IcarusContext();
			var staffAction = db.StaffActions.SingleOrDefault(sa => sa.StaffActionId == id);

			if (staffAction == null)
			{
				await RespondAsync($"No Staff Action with ID: {id}", ephemeral:true);
			}
			if (response.Length > 1800)
			{
				await RespondAsync("Description must be shorter than 1000 characters.", ephemeral: true);
				return;
			}
			if (!Enum.TryParse(stringStatus, true, out StaffActionStatus status))
			{
				await RespondAsync("Not a valid attribute.", ephemeral: true);
				return;
			}
			var runner = db.Users.SingleOrDefault(du => du.DiscordId == Context.User.Id.ToString());
			if (!runner.CanUseAdminCommands && staffAction.AssignedTo.DiscordId != Context.User.Id.ToString())
			{
				await RespondAsync("You cannot respond to this Staff Action.", ephemeral: true);
				return;
			}


			staffAction.Status = status;
			staffAction.ActionResponse = response;
			staffAction.AssignedTo ??= runner;

			db.StaffActions.Update(staffAction);
			await db.SaveChangesAsync();

			var StaffChannels = db.StaffActionChannels.ToList();
			var embedBuilder = _staffActionService.BuildStaffActionMessage(staffAction);
			foreach (var staffChannel in StaffChannels.Select(staffchannel => _client.GetChannel(ulong.Parse(staffchannel.ChannelId)) as SocketTextChannel))
			{
				try
				{
					var message = await staffChannel.GetMessageAsync(ulong.Parse(staffAction.MessageId)) as RestUserMessage;
					await message.ModifyAsync(x => x.Embed = embedBuilder.Build());
				}
				catch (NullReferenceException e)
				{
					Console.WriteLine(e.StackTrace);
					//nothing, I just don't want it to crash the command
				}
			}

			await _staffActionService.NotifyUser(staffAction);

			await RespondAsync("Response received.", ephemeral: true);
		}

		[SlashCommand("assign", "Assigns a user to a staff action")]
		[RequireAdmin]
		public async Task AssignAction(
		[Summary("ID", "The ID of action")]
		long id,
		[Summary("User", "The staff member assigned to action")]
		SocketGuildUser mention)
		{
			using var db = new IcarusContext();
			var staffAction = db.StaffActions.SingleOrDefault(sa => sa.StaffActionId == id);

			var mentionedUser = db.Users.SingleOrDefault(du => du.DiscordId == mention.Id.ToString());

			if (staffAction == null)
			{
				await RespondAsync($"No Staff Action with ID: {id}", ephemeral: true);
			}

			if (mentionedUser == null)
			{
				await RespondAsync("Could not find mentioned user.", ephemeral: true);
				return;
			}

			// if (!mentionedUser.CanUseAdminCommands)
			// {
			// 	await RespondAsync("This user is not allowed to use staff commands.", ephemeral: true);
			// 	return;
			// }

			staffAction.AssignedTo = mentionedUser;

			db.StaffActions.Update(staffAction);
			await db.SaveChangesAsync();

			var StaffChannels = db.StaffActionChannels.ToList();
			var embedBuilder = _staffActionService.BuildStaffActionMessage(staffAction);

			foreach (var staffChannel in StaffChannels.Select(staffchannel => _client.GetChannel(ulong.Parse(staffchannel.ChannelId)) as SocketTextChannel))
			{
				try
				{
					var message = await staffChannel.GetMessageAsync(ulong.Parse(staffAction.MessageId)) as RestUserMessage;
					await message.ModifyAsync(x => x.Embed = embedBuilder.Build());
				}
				catch (NullReferenceException e)
				{
					Console.WriteLine(e.StackTrace);
					//nothing, I just don't want it to crash the command
				}
			}
			await _staffActionService.NotifyUser(staffAction);

			await _staffActionService.NotifyStaff(staffAction);

			await RespondAsync($"Asigned Staff Action to {mention}", ephemeral: true);
		}

		[SlashCommand("submit", "Submits a staff action")]
		[RequireProfile]
		public async Task SubmitAction(
		[Summary("Title", "Action title (must be shorter than 200 characters)")]
		string title,
		[Summary("Description", "Action description (must be shorter than 1800 characters)")]
		string description)
		{
			await DeferAsync(ephemeral: true);

			using var db = new IcarusContext();
			var discordUser = db.Users.SingleOrDefault(du => du.DiscordId == Context.User.Id.ToString());

			if (title.Length > 200)
			{
				await FollowupAsync("Title must be shorter than 200 characters.", ephemeral: true);
				return;
			}

			if (description.Length > 1000)
			{
				await FollowupAsync("Description must be shorter than 1800 characters.", ephemeral: true);
				return;
			}

			StaffAction action = new()
			{
				ActionTitle = title,
				ActionDescription = description,
				Status = StaffActionStatus.TODO,
				SubmitterId = discordUser.DiscordId,
				Submitter = discordUser
			};

			db.StaffActions.Add(action);
			await db.SaveChangesAsync();

			var StaffChannels = db.StaffActionChannels.ToList();

			var embedBuilder = _staffActionService.BuildStaffActionMessage(action);

			foreach (var staffChannel in StaffChannels.Select(staffchannel => _client.GetChannel(ulong.Parse(staffchannel.ChannelId)) as SocketTextChannel))
			{
				try
				{
					var message = await staffChannel.SendMessageAsync("", false, embedBuilder.Build());
					action.MessageId = message.Id.ToString();
				}
				catch (NullReferenceException e)
				{
					Console.WriteLine(e.StackTrace);
					//nothing, I just don't want it to crash the command
				}
			}

			db.StaffActions.Update(action);
			await db.SaveChangesAsync();

			await FollowupAsync("Action submitted.", ephemeral: true);
		}

		[SlashCommand("list", "Lists all the actions you have submitted.")]
		[RequireProfile]
		public async Task MyActions()
		{
			await DeferAsync(ephemeral: true);

			using var db = new IcarusContext();
			var activeActions = await db.StaffActions.Where(sa => sa.SubmitterId == Context.User.Id.ToString()).OrderBy(sa => sa.StaffActionId).ToListAsync();
			var length = activeActions.Count;
			activeActions = activeActions.TakeLast(20).ToList();

			var embedBuilder = new EmbedBuilder
			{
				Color = Color.Purple,
				Title = "Submitted Actions"

			};

			var compoBuilder = new ComponentBuilder()
				.WithButton("Older", "back-button", disabled: length <= 20)
				.WithButton("Newer", "next-button", disabled: true);

			if (!activeActions.Any())
			{
				await FollowupAsync("No actions found.", ephemeral: true);
				return;
			}

			foreach (var sa in activeActions)
			{
				if (sa.AssignedToId == null)
				{
					var embedFieldBuilder = new EmbedFieldBuilder
					{
						Value = sa.ActionResponse == null ? "No response yet." : $"Response: {sa.ActionResponse}",
						Name = sa.StaffActionId + " - " + sa.ActionTitle + " - " + sa.Status.ToString() + " - " + "Nobody",
						IsInline = false
					};

					embedBuilder.AddField(embedFieldBuilder);
				}
				else
				{
					var staffAssignedTo = _client.GetUser(ulong.Parse(sa.AssignedToId));

					var embedFieldBuilder = new EmbedFieldBuilder
					{
						Value = sa.ActionResponse == null ? "No response yet." : $"Response: {sa.ActionResponse}",
						Name = sa.StaffActionId + " - " + sa.ActionTitle + " - " + sa.Status.ToString() + " - " + staffAssignedTo.Username,
						IsInline = false
					};

					embedBuilder.AddField(embedFieldBuilder);
				}
			}

			embedBuilder.WithFooter($"Page 1 of {(int) Math.Ceiling(length / 20f)}");

			await FollowupAsync(embed: embedBuilder.Build(), ephemeral: true, components: compoBuilder.Build());
		}

		[SlashCommand("assigned", "Lists all the actions you are assigned to.")]
		public async Task AssignedActions()
		{
			await DeferAsync(ephemeral: true);

			using var db = new IcarusContext();
			var assignedActions = await db.StaffActions.Where(sa => sa.AssignedToId == Context.User.Id.ToString()).OrderBy(sa => sa.StaffActionId).ToListAsync();
			var length = assignedActions.Count;
			assignedActions = assignedActions.TakeLast(20).ToList();

			var embedBuilder = new EmbedBuilder
			{
				Color = Color.Purple,
				Title = "Assigned Actions"
			};

			var compoBuilder = new ComponentBuilder()
				.WithButton("Older", "back-button", disabled: length <= 20)
				.WithButton("Newer", "next-button", disabled: true);

			if (!assignedActions.Any())
			{
				await FollowupAsync("No actions found.", ephemeral: true);
				return;
			}

			foreach (var sa in assignedActions)
			{
				if (sa.AssignedToId == null)
				{
					var embedFieldBuilder = new EmbedFieldBuilder
					{
						Value = sa.ActionResponse == null ? "No response yet." : $"Response: {sa.ActionResponse}",
						Name = sa.StaffActionId + " - " + sa.ActionTitle + " - " + sa.Status.ToString() + " - " + "Nobody",
						IsInline = false
					};

					embedBuilder.AddField(embedFieldBuilder);
				}
				else
				{
					var staffAssignedTo = _client.GetUser(ulong.Parse(sa.AssignedToId));

					var embedFieldBuilder = new EmbedFieldBuilder
					{
						Value = sa.ActionResponse == null ? "No response yet." : $"Response: {sa.ActionResponse}",
						Name = sa.StaffActionId +  " - " + sa.ActionTitle + " - " + sa.Status.ToString() + " - " + staffAssignedTo.Username,
						IsInline = false
					};

					embedBuilder.AddField(embedFieldBuilder);
				}
			}

			embedBuilder.WithFooter($"Page 1 of {(int)Math.Ceiling(length / 20f)}");

			await FollowupAsync(embed: embedBuilder.Build(), ephemeral: true, components: compoBuilder.Build());
		}
	}
}
