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
    }


}
