using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Utils;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace Icarus.Services
{
	/// <summary>
	/// A service that manages Discord votes.
	/// </summary>
	public class VoteService
	{

		private readonly TickService _tickService;
		private readonly DiscordSocketClient _client;
		private bool alreadyRunning = false;

		// Provides the number emotes. Usage: numberEmotes[numberYouWant]
		private readonly Emoji[] numberEmotes = new Emoji[] { new Emoji("0️⃣"), new Emoji("1️⃣"), new Emoji("2️⃣"), new Emoji("3️⃣"), new Emoji("4️⃣"), new Emoji("5️⃣"), new Emoji("6️⃣"), new Emoji("7️⃣"), new Emoji("8️⃣"), new Emoji("9️⃣") };

		// Types with multiple options
		private readonly VoteType[] multipleOptions = new VoteType[] {
			VoteType.FPTP,
			VoteType.TWOROUND,
			VoteType.TWOROUNDFINAL
		};

		bool IsMultipleOption(VoteType type)
		{
			return Array.Exists(multipleOptions, (x) => x == type);
		}

		public VoteService(DiscordSocketClient client, TickService tickService)
		{
			_client = client;
			_tickService = tickService;

			tickService.TickCheckEvent += DoVoteUpdate;
		}

		public void DoVoteUpdate() {
			_ = VoteUpdate();
		}

		private async Task VoteUpdate()
		{
			if(alreadyRunning) return; // A quick check to ensure the function does not run more than once simultaneously if the resolution is low enough
			alreadyRunning = true;
			// Check and update votes
			try
			{
				using var db = new IcarusContext();
				foreach (VoteMessage vms in db.VoteMessages)
				{
					try
					{
						// This is disgusting.
						RestUserMessage msg = (RestUserMessage) await ((ITextChannel)_client.GetChannel(vms.ChannelId)).GetMessageAsync(vms.MessageId);
						if (msg == null)
						{
							db.VoteMessages.Remove(vms);
							continue;
						}
						if (vms.EndTime < DateTime.Now.ToFileTime())
						{
							// await mid.AddReactionAsync(new Emoji("✅"));
							// await mid.AddReactionAsync(new Emoji("❌"));
							// await mid.AddReactionAsync(new Emoji("🇴"));
							if (!IsMultipleOption((VoteType)vms.Type))
							{
								int yesVotes = 0;
								int noVotes = 0;
								int abstainVotes = 0;
								try
								{
									yesVotes = msg.Reactions[new Emoji("✅")].ReactionCount - 1;
									noVotes = msg.Reactions[new Emoji("❌")].ReactionCount - 1;
									abstainVotes = msg.Reactions[new Emoji("🇴")].ReactionCount - 1;
								}
								catch (Exception) { }

								bool pass = false;
								switch ((VoteType)vms.Type)
								{
									case VoteType.MAJORITY:
										pass = yesVotes > noVotes;
										break;
									case VoteType.TWOTHIRD:
										pass = (yesVotes / ((float)yesVotes + noVotes)) > 0.66f;
										break;
									case VoteType.THREEFOURTHS:
										pass = (yesVotes / ((float)yesVotes + noVotes)) > 0.75f;
										break;
									default:
										pass = false;
										Console.WriteLine("Undefined VoteType: " + vms.Type);
										break;
								}
								if (pass)
								{
									Embed og = null;
									foreach (Embed e in msg.Embeds)
									{
										og = e;
										break; // I fucking hate IReadOnlyCollection.
									}
									EmbedBuilder emb = new EmbedBuilder()
											.WithTitle(og.Title)
											.WithDescription(og.Description)
											.AddField("Result:", "Passed with " + yesVotes + " for, " + noVotes + " against and " + abstainVotes + " abstaining.")
											.WithColor(Color.DarkGreen);
									await msg.ModifyAsync((e) =>
									{
										e.Embed = emb.Build();
									});
									try
									{
										await _client.GetUser(vms.CreatorId).SendMessageAsync(embed: new EmbedBuilder()
												.WithTitle("Vote " + og.Title + " has passed.")
												.WithDescription($"[Jump]({msg.GetJumpUrl()})")
												.WithColor(Color.DarkGreen)
												.Build());
									}
									catch (Exception) { }
								}
								else
								{
									Embed og = null;
									foreach (Embed e in msg.Embeds)
									{
										og = e;
										break;
									}
									EmbedBuilder emb = new EmbedBuilder()
											.WithTitle(og.Title)
											.WithDescription(og.Description)
											.AddField("Result:", "Failed with " + yesVotes + " for, " + noVotes + " against and " + abstainVotes + " abstaining.")
											.WithColor(Color.DarkRed);
									await msg.ModifyAsync((e) =>
									{
										e.Embed = emb.Build();
									});
									try
									{
										await _client.GetUser(vms.CreatorId).SendMessageAsync(embed: new EmbedBuilder()
												.WithTitle("Vote " + og.Title + " has failed.")
												.WithDescription($"[Jump]({msg.GetJumpUrl()})")
												.WithColor(Color.DarkRed)
												.Build());
									}
									catch (Exception) { }
								}
							}
							else
							{
								List<int> votes = new List<int>();
								Embed og = msg.Embeds.First();
								for (int x = 1; x < numberEmotes.Length; x++)
								{
									try
									{
										int tmp = msg.Reactions[numberEmotes[x]].ReactionCount - 1;
										votes.Add(tmp);
									}
									catch (Exception)
									{
										break; // If we get an exception, it must mean we have gone through all the existing options, so we should just end the loop.
									}
								}
								string results = "";
								for (int x = 0; x < votes.Count; x++)
								{
									results += (og.Fields[x + 2].Value.Length > 2048 / (og.Fields.Length - 1) ? og.Fields[x + 2].Value[..(2048 / (og.Fields.Length - 1))] : og.Fields[x + 2].Value) + ": " + votes[x] + " votes\n";
								}
								int winner = 0;
								switch ((VoteType)vms.Type)
								{
									case VoteType.FPTP:
										winner = votes.IndexOf(votes.Max());
										break;
									case VoteType.TWOROUND:
										int a = votes.IndexOf(votes.Max());
										if (votes[a] / (float)votes.Sum() > 0.5f)
										{ // We have a majority
											winner = a;
											break;
										}
										votes.RemoveAt(a);
										int b = votes.IndexOf(votes.Max());
										if (b >= a) b++; // Jank, but makes sense.
														 // start run-off with a and b
										TimeSpan timeSpan = new(vms.TimeSpan);
										string timeStr = $"<t:${(DateTime.UtcNow + timeSpan).ToFileTimeUtc()}:R>";
										EmbedBuilder embo = new EmbedBuilder()
											.WithTitle(og.Title)
											.AddField("End:", timeStr)
											.AddField("Type: ", "Two Round Runoff")
											.WithColor(Color.LightGrey);
										embo.AddField("Option #1", og.Fields[a + 2].Value);
										embo.AddField("Option #2", og.Fields[b + 2].Value);
										RestUserMessage mid = (RestUserMessage)await ((ITextChannel)_client.GetChannel(vms.ChannelId)).SendMessageAsync("", false, embo.Build());
										embo.WithFooter("Message ID: " + mid.Id);
										await mid.ModifyAsync((x) =>
										{
											x.Embed = embo.Build();
										});
										Embed neu = mid.Embeds.First();
										await mid.AddReactionsAsync(numberEmotes.AsSpan(1, neu.Fields.Length - 2).ToArray());
										VoteMessage message = new()
										{
											MessageId = mid.Id,
											CreatorId = vms.CreatorId,
											ChannelId = vms.ChannelId,
											Type = (int)VoteType.TWOROUNDFINAL,
											EndTime = DateTime.Now.AddHours(timeSpan.Hours).AddMinutes(timeSpan.Minutes).ToFileTime()
										};
										db.VoteMessages.Add(message);

										embo = new EmbedBuilder()
											.WithTitle(og.Title)
											.WithDescription(results)
											.AddField("Result:", og.Fields[a + 2].Value + " and " + og.Fields[b + 2].Value + " continue onto the runoff vote.")
											.WithColor(Color.DarkTeal);
										await msg.ModifyAsync((e) =>
										{
											e.Embed = embo.Build();
										});
										try
										{
											await _client.GetUser(vms.CreatorId).SendMessageAsync(embed: new EmbedBuilder()
													.WithTitle(og.Fields[a + 2].Value + " and " + og.Fields[b + 2].Value + " have continued onto the runoff vote.")
													.WithDescription($"[Jump]({msg.GetJumpUrl()})")
													.WithColor(Color.DarkTeal)
													.Build());
										}
										catch (Exception) { }
										db.Remove(vms);
										continue;
									case VoteType.TWOROUNDFINAL:
										winner = votes.IndexOf(votes.Max());
										break;
									default:
										winner = 0;
										break;
								}
								int num = votes.Count(x => x == votes.Max());
								if (num > 1)
								{
									string res = "The vote has ended with ";
									for (int x = 0; x < votes.Count; x++)
									{
										if (votes[x] == votes.Max()) res += og.Fields[x + 2].Value + ", ";
									}
									res = res[..^2] + " obtaining an equal amount of votes.";
									EmbedBuilder emb = new EmbedBuilder()
											.WithTitle(og.Title)
											.WithDescription(results)
											.AddField("Result:", res)
											.WithColor(Color.DarkTeal);
									await msg.ModifyAsync((e) =>
									{
										e.Embed = emb.Build();
									});
									try
									{
										await _client.GetUser(vms.CreatorId).SendMessageAsync(embed: new EmbedBuilder()
												.WithTitle("The vote " + og.Title + " has ended in a tie.")
												.WithDescription($"[Jump]({msg.GetJumpUrl()})")
												.WithColor(Color.DarkTeal)
												.Build());
									}
									catch (Exception) { }
								}
								else
								{
									EmbedBuilder emb = new EmbedBuilder()
											.WithTitle(og.Title)
											.WithDescription(results)
											.AddField("Result:", og.Fields[winner + 2].Name + " has won with " + votes[winner] + " votes.")
											.WithColor(Color.DarkTeal);
									await msg.ModifyAsync((e) =>
									{
										e.Embed = emb.Build();
									});
									try
									{
										await _client.GetUser(vms.CreatorId).SendMessageAsync(embed: new EmbedBuilder()
												.WithTitle(og.Fields[winner + 2].Value + " has won the vote " + og.Title + ".")
												.WithDescription($"[Jump]({msg.GetJumpUrl()})")
												.WithColor(Color.DarkTeal)
												.Build());
									}
									catch (Exception) { }
								}
							}
							db.VoteMessages.Remove(vms);
						}
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					}
				}
				await db.SaveChangesAsync();
			}
			catch (Exception e)
			{
				Console.Write("UpdateHandler Exception: " + e.StackTrace);
			}

			alreadyRunning = false;

			return;
		}
	}
}
