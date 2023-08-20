using Discord;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace Icarus.Services
{
	public class StaffActionService
	{
		private readonly DiscordSocketClient _client;
		private readonly DebugService _debugService;

		public StaffActionService(DiscordSocketClient client, DebugService debugService)
		{
			_client = client;
			_debugService = debugService;
		}

		private Color GetStaffActionColour(StaffActionStatus status)
		{
            switch (status)
            {
                case StaffActionStatus.TODO: return Color.Red;
                case StaffActionStatus.IN_PROGRESS: return Color.Gold;
                case StaffActionStatus.DONE: return Color.Green;
                default: return Color.Red;
            }
        }

		public async Task Cleanup()
		{
			using var db = new IcarusContext();

			var saChannel = db.StaffActionChannels.FirstOrDefault();

			var channel = (ITextChannel) await _client.GetChannelAsync(ulong.Parse(saChannel.ChannelId));

			var amountOfStaffActions = db.StaffActions.Count();

			var staffActionsId = await db.StaffActions.Select(sa => sa.MessageId).ToListAsync();

			await foreach(var messageBatch in channel.GetMessagesAsync(amountOfStaffActions + 100))
			{
				foreach (var message in messageBatch)
				{
					if (!staffActionsId.Contains(message.Id.ToString()))
					{
						await message.DeleteAsync();
					}
				}
			}
		}

		public StaffAction GetStaffActionById(int id)
		{
            using var db = new IcarusContext();

            var staffAction = db.StaffActions.Include(sa => sa.Submitter).Include(sa => sa.AssignedTo)
                .SingleOrDefault(sa => sa.StaffActionId == id);

            return staffAction;
        }

		public List<StaffAction> GetTodoStaffActions()
		{
			using var db = new IcarusContext();

			var saTodo = db.StaffActions.Include(sa => sa.Submitter).Include(sa => sa.AssignedTo)
                .Where(s => s.Status == StaffActionStatus.TODO || s.Status == StaffActionStatus.IN_PROGRESS);

			return saTodo.ToList();
		}

        public StaffAction GetMyOldestTodoStaffAction(ulong assginedId)
        {
            using var db = new IcarusContext();

            var oldest = db.StaffActions.Include(sa => sa.Submitter).Include(sa => sa.AssignedTo)
                .Where(sa => (sa.Status == StaffActionStatus.TODO || sa.Status == StaffActionStatus.IN_PROGRESS) && sa.AssignedToId == assginedId.ToString()).OrderBy(sa => sa.StaffActionId).First();

            return oldest;
        }

        public StaffAction GetOldestTodoStaffAction()
		{
			using var db = new IcarusContext();

			var oldest = db.StaffActions.Include(sa => sa.Submitter).Include(sa => sa.AssignedTo)
				.Where(sa => sa.Status == StaffActionStatus.TODO || sa.Status == StaffActionStatus.IN_PROGRESS).OrderBy(sa => sa.StaffActionId).First();

			return oldest;
		}

        public EmbedBuilder BuildStaffActionMessage(StaffAction staffAction)
		{
			var submitter = _client.GetUser(ulong.Parse(staffAction.SubmitterId));

			var embedBuilder = new EmbedBuilder
			{
				Color = GetStaffActionColour(staffAction.Status),
				Title = staffAction.ActionTitle + " - " + staffAction.StaffActionId.ToString() + " - " + submitter.Username,
			};

			EmbedFieldBuilder embedFieldBuilder;

			if (staffAction.AssignedToId == null)
			{
				embedFieldBuilder = new EmbedFieldBuilder
				{
					Value = staffAction.ActionDescription,
					Name = staffAction.Status.ToString(),
					IsInline = false
				};
			}
			else
			{
				var assignedStaff = _client.GetUser(ulong.Parse(staffAction.AssignedToId));

				embedFieldBuilder = new EmbedFieldBuilder
				{
					Value = staffAction.ActionDescription,
					Name = staffAction.Status.ToString() + " : " + assignedStaff.Username,
					IsInline = false
				};
			}

			embedBuilder.AddField(embedFieldBuilder);

			embedBuilder.AddField(new EmbedFieldBuilder
			{
				Name = "Response",
				Value = staffAction.ActionResponse ?? "None"
			});

			return embedBuilder;
		}

		public async Task NotifyUser(StaffAction staffAction)
		{
			var submitter = _client.GetUser(ulong.Parse(staffAction.SubmitterId));
			var embedBuilder = new EmbedBuilder
			{
				Color = Color.Purple,
				Title = "Your Action has been updated to the following:",
			};

			embedBuilder.AddField(new EmbedFieldBuilder
			{
				Name = staffAction.ActionTitle + " - " + staffAction.StaffActionId.ToString(),
				Value = staffAction.ActionDescription,
				IsInline = false
			});
			embedBuilder.AddField(new EmbedFieldBuilder
			{
				Name = staffAction.Status.ToString() + " - Response:",
				Value = staffAction.ActionResponse ?? "None"
			});
			embedBuilder.AddField(new EmbedFieldBuilder
			{
				Name = "Assigned to Staff: ",
				Value = staffAction.AssignedToId == null ? "No Staff Assigned" : _client.GetUser(ulong.Parse(staffAction.AssignedToId)).Username
			});

			try
			{
				await submitter.SendMessageAsync("", false, embedBuilder.Build());
			}
			catch (NullReferenceException e)
			{
				Console.WriteLine(e.StackTrace);
				await _debugService.PrintToChannels($"An exception ocurred in StaffActionsService:\n{e.StackTrace}");
				//nothing, I just don't want it to crash the command
			}

		}

		public async Task NotifyStaff(StaffAction staffAction)
		{
			var staff = _client.GetUser(ulong.Parse(staffAction.AssignedToId));
			var embedBuilder = new EmbedBuilder
			{
				Color = Color.Purple,
				Title = "You have been assigned to the following staff Action:",
			};

			embedBuilder.AddField(new EmbedFieldBuilder
			{
				Name = staffAction.ActionTitle + " - " + staffAction.StaffActionId.ToString(),
				Value = staffAction.ActionDescription,
				IsInline = false
			});
			embedBuilder.AddField(new EmbedFieldBuilder
			{
				Name = staffAction.Status.ToString() + " - Response:",
				Value = staffAction.ActionResponse ?? "None"
			});
			embedBuilder.AddField(new EmbedFieldBuilder
			{
				Name = "Assigned to Staff: ",
				Value = staffAction.AssignedToId == null ? "No Staff Assigned" : _client.GetUser(ulong.Parse(staffAction.AssignedToId)).Username
			});

			try
			{
				await staff.SendMessageAsync("", false, embedBuilder.Build());
			}
			catch (NullReferenceException e)
			{
				Console.WriteLine(e.StackTrace);
				await _debugService.PrintToChannels($"An exception ocurred in StaffActionsService:\n{e.StackTrace}");
				//nothing, I just don't want it to crash the command
			}

		}

		public async Task PreviousSAPage(SocketMessageComponent arg) {
			SocketMessage msg = arg.Message;
			if (msg.Embeds.Count < 1) {
				await _debugService.PrintToChannels($"{arg.Data.CustomId} executed on a message with no embed!");
				return;
			}
			Embed msgEmbed = msg.Embeds.First();
			string type = msgEmbed.Title.Split(' ')[0]; // Since the type of list is in the title, we're basically doing advanced sleuthing here
			string pageStr = msgEmbed.Footer.Value.Text.Split(' ')[1]; // Once again, sleuthing

			using var db = new IcarusContext();

			List<StaffAction> actions;
			if (type == "Submitted") actions = await db.StaffActions.Where(sa => sa.SubmitterId == arg.User.Id.ToString()).OrderBy(sa => sa.StaffActionId).ToListAsync();
			else if (type == "Assigned") actions = await db.StaffActions.Where(sa => sa.AssignedToId == arg.User.Id.ToString()).OrderBy(sa => sa.StaffActionId).ToListAsync();
			else {
				await _debugService.PrintToChannels($"Unknown SA list type \"{type}\"!");
				return;
			}

			int length = actions.Count;

			if (!int.TryParse(pageStr, System.Globalization.NumberStyles.Integer, null, out int page)) {
				await _debugService.PrintToChannels($"Could not parse \"{pageStr}\" as int!");
				return;
			}

			if (length < page * 20) {
				await _debugService.PrintToChannels($"Back button was not disable for SA list on last page!");
				return;
			}
			page += 1;
			actions = actions.TakeLast(Math.Max(page * 20, length)).Take(Math.Min(20, length - (page - 1) * 20)).ToList(); // First 20 of page * 20 last items will be the 20 items we wish to display

			var embedBuilder = new EmbedBuilder
			{
				Color = Color.Purple,
				Title = $"{type} Actions"

			};

			var compoBuilder = new ComponentBuilder()
				.WithButton("Older", "back-button", disabled: length < page * 20)
				.WithButton("Newer", "next-button", disabled: page <= 1);

			foreach (var sa in actions)
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

			embedBuilder.WithFooter($"Page {page} of {(int)Math.Ceiling(length / 20f)}");

			await arg.UpdateAsync(msg =>
			{
				Embed[] e = {
					embedBuilder.Build()
				};
				msg.Embeds = e;
				msg.Components = compoBuilder.Build();
			});
		}

		public async Task NextSAPage(SocketMessageComponent arg)
		{
			SocketMessage msg = arg.Message;
			if (msg.Embeds.Count < 1)
			{
				await _debugService.PrintToChannels($"{arg.Data.CustomId} executed on a message with no embed!");
				return;
			}
			Embed msgEmbed = msg.Embeds.First();
			string type = msgEmbed.Title.Split(' ')[0]; // Since the type of list is in the title, we're basically doing advanced sleuthing here
			string pageStr = msgEmbed.Footer.Value.Text.Split(' ')[1]; // Once again, sleuthing

			using var db = new IcarusContext();

			List<StaffAction> actions;
			if (type == "Submitted") actions = await db.StaffActions.Where(sa => sa.SubmitterId == arg.User.Id.ToString()).OrderBy(sa => sa.StaffActionId).ToListAsync();
			else if (type == "Assigned") actions = await db.StaffActions.Where(sa => sa.AssignedToId == arg.User.Id.ToString()).OrderBy(sa => sa.StaffActionId).ToListAsync();
			else
			{
				await _debugService.PrintToChannels($"Unknown SA list type \"{type}\"!");
				return;
			}

			int length = actions.Count;

			if (!int.TryParse(pageStr, System.Globalization.NumberStyles.Integer, null, out int page))
			{
				await _debugService.PrintToChannels($"Could not parse \"{pageStr}\" as int!");
				return;
			}

			page -= 1;
			if (page < 1)
			{
				await _debugService.PrintToChannels($"Next button was not disabled for SA list on first page!");
				return;
			}
			actions = actions.TakeLast(Math.Max(page * 20, length)).Take(Math.Min(20, length - (page - 1) * 20)).ToList(); // First 20 of page * 20 last items will be the 20 items we wish to display

			var embedBuilder = new EmbedBuilder
			{
				Color = Color.Purple,
				Title = $"{type} Actions"

			};

			var compoBuilder = new ComponentBuilder()
				.WithButton("Older", "back-button", disabled: length < page * 20)
				.WithButton("Newer", "next-button", disabled: page <= 1);

			foreach (var sa in actions)
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

			embedBuilder.WithFooter($"Page {page} of {(int)Math.Ceiling(length / 20f)}");

			await arg.UpdateAsync(msg =>
			{
				Embed[] e = {
					embedBuilder.Build()
				};
				msg.Embeds = e;
				msg.Components = compoBuilder.Build();
			});
		}
	}
}
