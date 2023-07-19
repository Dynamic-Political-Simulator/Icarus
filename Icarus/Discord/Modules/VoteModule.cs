using Discord.WebSocket;
using System.Threading.Tasks;
using Icarus.Context;
using Discord.Interactions;
using Icarus.Services;
using System.Linq;
using System;
using Discord;
using System.Collections.Generic;
using Icarus.Context.Models;
using System.Xml;
using Icarus.Discord.CustomPreconditions;
using System.Globalization;
using System.Text.RegularExpressions;
using Discord.Rest;
using Icarus.Utils;
using System.Runtime.InteropServices;

namespace Icarus.Discord.Modules
{

	public partial class VoteModule : InteractionModuleBase<SocketInteractionContext>
	{
		private readonly DiscordSocketClient _client;
		private readonly VoteService _vote;

		public VoteModule(DiscordSocketClient client, VoteService vote)
		{
			_client = client;
			_vote = vote;
		}

		// Provides the number emotes. Usage: numberEmotes[numberYouWant]
		private readonly Emoji[] numberEmotes = new Emoji[] { new Emoji("0️⃣"), new Emoji("1️⃣"), new Emoji("2️⃣"), new Emoji("3️⃣"), new Emoji("4️⃣"), new Emoji("5️⃣"), new Emoji("6️⃣"), new Emoji("7️⃣"), new Emoji("8️⃣"), new Emoji("9️⃣") };

		// Types with multiple options
		private readonly VoteType[] multipleOptions = new VoteType[] {
			VoteType.FPTP,
			VoteType.TWOROUND
		};

		string ConvertVoteTypeToString(VoteType type)
		{
			return type switch
			{
				VoteType.MAJORITY => "Majority",
				VoteType.TWOTHIRD => "Two Thirds",
				VoteType.FPTP => "First Past the Post",
				VoteType.TWOROUND => "Two Round",
				VoteType.TWOROUNDFINAL => "Two Round Run-off",
				_ => "Undefined",
			};
		}

		string[] formats = new[] { "h'h'", "h'h'm'm'", "m'm'", "%m" }; // should ensure that XhMmin format works
		TimeSpan GetTimeSpan(string time)
		{
			if (TimeSpan.TryParseExact(time, formats, CultureInfo.InvariantCulture, out TimeSpan timeSpan))
			{
				return timeSpan;
			}
			else
			{
				return TimeSpan.Zero;
			}
		}

		bool IsMultipleOption(VoteType type)
		{
			return Array.Exists(multipleOptions, (x) => x == type);
		}

		// Parses the type using regex that allows for a degree of variation on the words
		VoteType? ParseType(string type)
		{
			type = type.ToLower(); // Gets rid of the capitalisation, this is not a case sensitive server :rage:
			return type switch
			{
				// This is art.
				string when MajorityRegex().IsMatch(type) => (VoteType?)VoteType.MAJORITY,
				string when TwoThirdsRegex().IsMatch(type) => (VoteType?)VoteType.TWOTHIRD,
				string when FPTPRegex().IsMatch(type) => (VoteType?)VoteType.FPTP,
				string when TwoRoundsRegex().IsMatch(type) => (VoteType?)VoteType.TWOROUND,
				string when type == "three-fourths" => VoteType.THREEFOURTHS,
				_ => null,
			};
		}

		// TryParse variation of the function so you don't have to write a check for null every time you use it
		bool ParseType(string type, out VoteType res)
		{
			VoteType? tmp = ParseType(type); // Call the original function to get the actual type
			if (tmp == null) // Test if it found something
			{
				res = 0; // Feed it some bogus 
				return false;
			}
			else
			{
				res = (VoteType)tmp;
				return true;
			}
		}

		bool? ParseBool(string text)
		{
			text = text.ToLower();
			return text switch
			{
				string when YeaRegex().IsMatch(text) => true,
				string when NayRegex().IsMatch(text) => false,
				_ => null,
			};
		}

		bool ParseBool(string text, out bool res)
		{
			bool? tmp = ParseBool(text);
			if (tmp == null)
			{
				res = false;
				return false;
			}
			else
			{
				res = (bool)tmp;
				return true;
			}
		}

		[SlashCommand("makevote", "Creates a vote")]
		public async Task MakeVote(
			[Summary("Type", "The type of vote"), 
			Choice("Majority", "majority"), 
			Choice("Two-Thirds", "two-thirds"),
			Choice("Three-Fourths", "three-fourths"),
			Choice("First-Past-the-Post", "fptp"), 
			Choice("Two Rounds", "two-rounds")] string type,
			// [Summary("Time", "How long the time should last (XhYmin)")] string time, 
			[Summary("Text", "Vote text, either Title | Decription or Title | Candidate 1 | Candidate 2 | etc.")] string text,
			[Summary("Hours", "How many hours the vote should last")] int? hours = null,
			[Summary("Minutes", "How many minutes the vote should last")] int? minutes = null)
		{
			string[] textArgs = text.Split("|");
			if (ParseType(type, out VoteType voteType))
			{
				// Vote type is certified epic :)
				if (hours == null && minutes == null)
				{
					await RespondAsync("You have specified an invalid time.", ephemeral: true);
				}
				else
				{
					TimeSpan timeSpan = TimeSpan.FromMinutes((hours ?? 0) * 60 + (minutes ?? 0));
					await DeferAsync(ephemeral: true);
					if (!IsMultipleOption(voteType))
					{
						// textArg format: [Title] | [Link/Desc]
						if (textArgs.Length != 2)
						{
							await RespondAsync("You have specified an invalid amount of text arguments", ephemeral: true);
						}
						else
						{
							string timeStr = $"<t:{(DateTimeOffset.UtcNow + timeSpan).ToUnixTimeSeconds()}:R>";
							EmbedBuilder emb = new EmbedBuilder()
								.WithTitle(textArgs[0])
								.WithDescription(textArgs[1])
								.AddField("End:", timeStr)
								.AddField("Type: ", ConvertVoteTypeToString(voteType))
								.WithColor(Color.LightGrey);
							RestUserMessage mid = await Context.Channel.SendMessageAsync("", false, emb.Build());
							emb.WithFooter("Message ID: " + mid.Id);

							await mid.ModifyAsync((x) =>
							{
								x.Embed = emb.Build();
							});

							await mid.AddReactionsAsync(new IEmote[] {
									new Emoji("✅"), // yay
									new Emoji("❌"), // nay
									new Emoji("🇴") // abstain
								});

							VoteMessage message = new()
							{
								MessageId = mid.Id,
								CreatorId = Context.User.Id,
								ChannelId = Context.Channel.Id,
								Type = (int)voteType,
								EndTime = (DateTime.UtcNow + timeSpan).ToFileTime(),
								TimeSpan = timeSpan.Ticks
							};
							
							using var db = new IcarusContext();

							db.VoteMessages.Add(message);
							await db.SaveChangesAsync();
						}
					}
					else
					{
						// textArg format: [Title] | [Candidate 1] | [Candidate 2] | ... | [Candidate N], where N <= 9
						// If at some point we end up needing more than 9 candidates then I guess it'll automatically go to anon mode
						if (textArgs.Length < 2 || textArgs.Length > 10)
						{
							await ReplyAsync("You have specified an invalid amount of text arguments");
						}
						else
						{
							string timeStr = $"<t:{(DateTimeOffset.UtcNow + timeSpan).ToUnixTimeSeconds()}:R>";
							EmbedBuilder emb = new EmbedBuilder()
								.WithTitle(textArgs[0])
								.AddField("Ends:", timeStr)
								.AddField("Type: ", ConvertVoteTypeToString(voteType))
								.WithColor(Color.LightGrey);
							for (int x = 1; x < textArgs.Length; x++)
							{
								emb.AddField("Option #" + x + ":", textArgs[x], true);
							}
							RestUserMessage mid = await Context.Channel.SendMessageAsync("", false, emb.Build());

							emb.WithFooter("Message ID: " + mid.Id);

							await mid.ModifyAsync((x) =>
							{
								x.Embed = emb.Build();
							});
							await mid.AddReactionsAsync(numberEmotes.AsSpan(1, textArgs.Length - 1).ToArray());

							VoteMessage message = new()
							{
								MessageId = mid.Id,
								CreatorId = Context.User.Id,
								ChannelId = Context.Channel.Id,
								Type = (int)voteType,
								EndTime = DateTime.Now.AddHours(timeSpan.Hours).AddMinutes(timeSpan.Minutes).ToFileTime(),
								TimeSpan = timeSpan.Ticks,
							};

							using var db = new IcarusContext();

							db.VoteMessages.Add(message);
							await db.SaveChangesAsync();
						}
					}
					await ModifyOriginalResponseAsync((msg) => {
						msg.Content = "Vote created!";
					});
				}
			}
			else
			{
				await ReplyAsync("You specified an invalid vote type.");
			}
		}

		[GeneratedRegex("^(majority)\\b")]
		private static partial Regex MajorityRegex();
		[GeneratedRegex("^(two( |-)?thirds?)\\b")]
		private static partial Regex TwoThirdsRegex();
		[GeneratedRegex("^(f(irst)?(-| )?p(ast)?(-| )?t(he)?(-| )?p(ost)?)\\b")]
		private static partial Regex FPTPRegex();
		[GeneratedRegex("^(two( |-)?rounds?)\\b")]
		private static partial Regex TwoRoundsRegex();
		[GeneratedRegex("^((y(es|ay|ea?)?)|(a(ye|ffirmative)?)|(t(rue)?))\\b")]
		private static partial Regex YeaRegex();
		[GeneratedRegex("^((n(o|ay|egative|ah)?)|(f(alse)?))\\b")]
		private static partial Regex NayRegex();
	}
}
