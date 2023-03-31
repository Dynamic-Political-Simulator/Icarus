using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Icarus.Context;
using Icarus.Services;
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

        static void Main(string[] args) => new Program().Start().GetAwaiter().GetResult();

        public async Task Start()
        {
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            Configuration = configBuilder.Build();

            _client.Log += Log;

            _services = BuildServiceProvider();
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
                .AddSingleton<ValueManagementService>()
                .AddDbContext<IcarusContext>(ServiceLifetime.Transient)
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
