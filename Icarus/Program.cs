using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Icarus
{
    internal class Program
	{
        private readonly DiscordSocketClient _client = new DiscordSocketClient();
		// private readonly InteractionService _commands = new InteractionService(_client.Rest);

		private IServiceProvider _services;

        public static IConfigurationRoot Configuration { get; set; }

		static Task Main(string[] args) => new Program().Start();

        public async Task Start()
        {
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            Configuration = configBuilder.Build();

            _client.Log += Log;

            _services = BuildServiceProvider();

			_services.ToString();

			var icarusConfig = new IcarusConfig();
			Configuration.GetSection("IcarusConfig").Bind(icarusConfig);

			await _client.LoginAsync(TokenType.Bot, icarusConfig.Token);
			await _client.StartAsync();

			_client.Ready += OnReady;

			// new CommandHandler(_services, _commands, _client);

			await _client.SetGameAsync(icarusConfig.Version);

			await Task.Delay(-1);
        }

		private async Task OnReady()
		{
			var icarusConfig = new IcarusConfig();
			Configuration.GetSection("IcarusConfig").Bind(icarusConfig);

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
		}

		private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public IServiceProvider BuildServiceProvider()
        {
            var services = new ServiceCollection();

            services.AddOptions();

			// services.Configure<IcarusConfig>(Configuration.GetSection("IcarusConfig"));
			services.AddSingleton<IConfiguration>(Configuration);

			services.AddSingleton(_client)
				.AddSingleton<TickService>()
				.AddSingleton<GoogleSheetsService>()
				.AddDbContext<IcarusContext>(ServiceLifetime.Transient);

            return services.BuildServiceProvider();
        }
    }
}
