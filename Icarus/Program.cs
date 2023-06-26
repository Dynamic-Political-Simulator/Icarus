using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Castle.Core.Configuration;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Services;
using Icarus.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Icarus
{
	internal class Program
	{
		private readonly DiscordSocketClient _client = new DiscordSocketClient();
		// private readonly InteractionService _commands = new InteractionService(_client.Rest);

		private IServiceProvider _services;

		static Task Main(string[] args) => new Program().Start();

		public async Task Start()
		{
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

			InteractionService interactionService = new InteractionService(_client.Rest);
			// Register slash commands defined in modules
			await interactionService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), _services);
			await interactionService.RegisterCommandsToGuildAsync(icarusConfig.GuildId); // TODO: Register commands globally when this reaches production (otherwise they will not be usable in DMs)

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

			await Task.Delay(-1);
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
				.AddDbContext<IcarusContext>(ServiceLifetime.Transient)
			.BuildServiceProvider();

			return services.BuildServiceProvider();
		}
	}
}
