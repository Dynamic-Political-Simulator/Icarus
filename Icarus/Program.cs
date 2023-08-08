using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Configuration;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Context.Models;
using Icarus.Services;
using Icarus.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using IResult = Discord.Interactions.IResult;
using RunMode = Discord.Interactions.RunMode;

namespace Icarus
{
	internal class Program
	{
		private static readonly DiscordSocketConfig discordSocketConfig = new DiscordSocketConfig()
		{
            GatewayIntents = GatewayIntents.All
        };

		private DiscordSocketClient _client;
		// private readonly InteractionService _commands = new InteractionService(_client.Rest);

		private IServiceProvider _services;

		static Task Main(string[] args) => new Program().Start();
		
		public async Task Start()
		{
            _client = new DiscordSocketClient(discordSocketConfig);

            CultureInfo culture;

			culture = CultureInfo.CreateSpecificCulture("en-US");

            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            _client.Log += LogAsync;

			_services = BuildServiceProvider();

			var icarusConfig = ConfigFactory.GetConfig();

			_client.Ready += OnReady;

			await _client.LoginAsync(TokenType.Bot, icarusConfig.Token.ToString());
			await _client.StartAsync();

			await _client.SetGameAsync(icarusConfig.Version);

			await Task.Delay(-1);
		}

		private async Task OnReady()
		{
			var icarusConfig = ConfigFactory.GetConfig();

			var interactionServiceConfig = new InteractionServiceConfig()
			{
				DefaultRunMode = RunMode.Async,
				LogLevel = LogSeverity.Debug
			};

			InteractionService interactionService = new InteractionService(_client.Rest, interactionServiceConfig);
			// Register slash commands defined in modules
			await interactionService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), _services);
			await interactionService.RegisterCommandsToGuildAsync(icarusConfig.GuildId); // TODO: Register commands globally when this reaches production (otherwise they will not be usable in DMs)

			interactionService.SlashCommandExecuted += async (SlashCommandInfo arg1, IInteractionContext arg2, IResult arg3) => 
			{
				if (!arg3.IsSuccess)
				{
					switch (arg3.Error)
					{
						case InteractionCommandError.UnmetPrecondition:
							await arg2.Interaction.RespondAsync($"{arg3.ErrorReason}", ephemeral: true);
							break;
						case InteractionCommandError.Exception:
							await _services.GetService<DebugService>().PrintToChannels($"An exception ocurred:\n{arg3.ErrorReason}");
							await arg2.Interaction.RespondAsync("An exception ocurred when executing command. Please inform staff of what you were trying to run.", ephemeral: true);							
							Console.WriteLine(arg3.ErrorReason);
							break;
						default:
							await arg2.Interaction.RespondAsync("Command could not be executed. Try again later.", ephemeral: true);
							break;
					}
				}
			};

			// Register a callback to handle commands when they are run.
			_client.InteractionCreated += async (SocketInteraction socketInteraction) =>
			{
				try
				{
					var context = new SocketInteractionContext(_client, socketInteraction);
					await interactionService.ExecuteCommandAsync(context, _services);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.ToString());

					if (socketInteraction.Type == InteractionType.ApplicationCommand)
					{
						await socketInteraction.GetOriginalResponseAsync()
								.ContinueWith(async message => await message.Result.DeleteAsync());
					}
				}
			};

			_client.ReactionAdded += HandleReactAsync;

			ComponentReactionHandlerService selectMenuHandler = (ComponentReactionHandlerService)_services.GetService(typeof(ComponentReactionHandlerService));

			_client.SelectMenuExecuted += selectMenuHandler.HandleSelectMenu;
			_client.ButtonExecuted += selectMenuHandler.HandleButtonPress;

			_ = RunPythonScript();

			_client.Ready -= OnReady;

            await Task.Delay(-1);
		}

		private async Task RunPythonScript()
		{
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "/python/bin/python3";
            start.Arguments = "ChartGen.py";
            start.UseShellExecute = false;
            start.CreateNoWindow = true;
            start.RedirectStandardOutput = true;
			Process.Start(start);
        }

		private Task LogAsync(LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}

		public IServiceProvider BuildServiceProvider()
		{
			var services = new ServiceCollection();

			services.AddOptions();

			services.AddSingleton(_client)
				//.AddSingleton(_commands)
				.AddSingleton<ValueManagementService>()
				.AddSingleton<TickService>()
				.AddSingleton<GoogleSheetsService>()
				.AddSingleton<ActionService>()
				.AddSingleton<CharacterService>()
				.AddSingleton<DiscordInteractionHelpers>()
				.AddSingleton<EconVisualsService>()
				.AddSingleton<GoIService>()
				.AddSingleton<RoleService>()
				.AddSingleton<DeathService>()
				.AddSingleton<GraveyardService>()
				.AddSingleton<DebugService>()
				.AddSingleton<AgeService>()
				.AddSingleton<MessagingService>()
				.AddSingleton<ComponentReactionHandlerService>()
				.AddSingleton<VoteService>()
				.AddSingleton<StaffActionService>()
				.AddDbContext<IcarusContext>(ServiceLifetime.Transient)
			.BuildServiceProvider();

			return services.BuildServiceProvider();
		}

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

		private async Task HandleReactAsync(Cacheable<IUserMessage, ulong> msg, Cacheable<IMessageChannel, ulong> channel, SocketReaction react)
		{
			if (react.UserId == _client.CurrentUser.Id) return;
			using var db = new IcarusContext();
			VoteMessage vms = db.VoteMessages.Find(msg.Id);
			if (vms != null)
			{
				if (!IsMultipleOption((VoteType)vms.Type)
					&& (react.Emote.Name == (new Emoji("✅")).Name
						|| react.Emote.Name == (new Emoji("❌")).Name
						|| react.Emote.Name == (new Emoji("🇴")).Name)) // Yes, I wrote this conditional purely to allow meme reacts on votes.
				{
					IUserMessage message = await msg.GetOrDownloadAsync();
					ulong reactor = react.UserId;
					if (react.Emote.Name != (new Emoji("✅")).Name)
					{
						await message.RemoveReactionAsync(new Emoji("✅"), reactor);
					}
					if (react.Emote.Name != (new Emoji("❌")).Name)
					{
						await message.RemoveReactionAsync(new Emoji("❌"), reactor);
					}
					if (react.Emote.Name != (new Emoji("🇴")).Name)
					{
						await message.RemoveReactionAsync(new Emoji("🇴"), reactor);
					}
				}
				else
				{
					Emoji reacte = numberEmotes.FirstOrDefault(x => x.Name == react.Emote.Name);
					if (reacte != null)
					{
						IUserMessage message = await msg.GetOrDownloadAsync();
						ulong reactor = react.UserId;
						List<Emoji> temp = numberEmotes.ToList();
						temp.Remove(reacte);
						await message.RemoveReactionsAsync(_client.GetUser(reactor), temp.ToArray()); // WHY THE FUCK DOES ID NOT WORK FOR THIS EVEN THOUGH IT WORKS FOR NON-BULK REACT REMOVAL??? DISCORD.NET PLEAAAAAAAAAAAASE
					}
				}
			}
			return;
		}
	}
}
