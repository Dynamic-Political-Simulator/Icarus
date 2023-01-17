using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Icarus
{
    internal class Program
    {
        private readonly CommandService _commands = new CommandService();
        private readonly DiscordSocketClient _client = new DiscordSocketClient();

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

			var icarusConfig = new IcarusConfig();
			Configuration.GetSection("IcarusConfig").Bind(icarusConfig);

			await _client.LoginAsync(TokenType.Bot, icarusConfig.Token);
			await _client.StartAsync();

			await _client.SetGameAsync(icarusConfig.Version);

			await Task.Delay(-1);
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

            services.Configure<IcarusConfig>(Configuration.GetSection("IcarusConfig"));

            services.AddSingleton(_client)
                .AddSingleton(_commands)
            //.AddSingleton<RollService>()
            //.AddSingleton<ActivityService>()
            //.AddSingleton<DeathService>()
            //.AddSingleton<CharacterService>()
            //.AddSingleton<HouseRoleManager>()
            //.AddSingleton<GameActivityService>()
            //.AddSingleton<StaffActionService>()
            //.AddSingleton<InventoryService>()
            //.AddSingleton<MarriageService>()
            //.AddDbContext<StewardContext>(ServiceLifetime.Transient)
            .BuildServiceProvider();

            return services.BuildServiceProvider();
        }
    }
}
