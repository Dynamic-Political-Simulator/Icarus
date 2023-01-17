using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Icarus.Context
{
    public class IcarusContext : DbContext
    {
		public IConfiguration Configuration { get; }

		public IcarusContext() // Called for migrations
		{
			var configBuilder = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

			Configuration = configBuilder.Build();
		}

		public IcarusContext(IConfiguration configuration)
		{
			Configuration = configuration;
		}


		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var config = new IcarusConfig();
			Configuration.GetSection("IcarusConfig").Bind(config);

            optionsBuilder.UseSqlServer($"Server={config.DatabaseIp};"
                + $"Database={config.DatabaseName};"
                + $"User Id={config.SqlUsername};"
                + $"Password={config.SqlPassword};"
                + "MultipleActiveResultSets=true");
        }

		public DbSet<GameState> GameStates { get; set; }
    }
}
