using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Icarus.Context.Models;
using Icarus.Utils;

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
            var config = ConfigFactory.GetConfig();

            optionsBuilder
                .UseLazyLoadingProxies()
                .UseSqlServer($"Server={config.DatabaseIp};"
                + $"Database={config.DatabaseName};"
                + $"User Id={config.SqlUsername};"
                + $"Password={config.SqlPassword};"
                + "Trusted_Connection=false;"
                + "MultipleActiveResultSets=true;"
                + "trustServerCertificate=true;");
                // .LogTo(Console.WriteLine);
        }

        public DbSet<GameState> GameStates { get; set; }
        public DbSet<CharacterToken> Tokens { get; set; }
        public DbSet<CharacterTokenType> TokenTypes { get; set; }
        public DbSet<DiscordUser> Users { get; set; }
        public DbSet<PlayerCharacter> Characters { get; set; }
        public DbSet<DeathTimer> DeathTimer { get; set; }
        public DbSet<Value> Values { get; set; }
        public DbSet<ValueModifier> ValueModifiers { get; set; }
        public DbSet<ValueRelationship> Relationships { get; set; }
        public DbSet<Nation> Nations { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<Modifier> Modifiers { get; set; }
        public DbSet<Good> Goods { get; set; }
        public DbSet<GoodValueModifier> GoodValueModifiers { get; set; }
        public DbSet<ValueHistory> valueHistories { get; set; }
        public DbSet<GraveyardChannel> GraveyardChannels { get; set; }
        public DbSet<DebugChannel> DebugChannels { get; set; }
        public DbSet<GroupOfInterest> GroupOfInterests { get; set; }
		public DbSet<VoteMessage> VoteMessages { get; set; }
		public DbSet<StaffAction> StaffActions { get; set; }
		public DbSet<StaffActionChannel> StaffActionChannels { get; set; }


        // Default Data
        protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<GameState>().HasData(
				new GameState
				{
					GameStateId = 1,
					TickInterval = 3600000,
					LastTickEpoch = 0,
                    Year = 0
				}
			);

            modelBuilder.Entity<GoodValueModifier>()
                .HasOne(m => m.GoodWrapper)
                .WithMany(g => g.ValueModifiers)
                .HasForeignKey(m => m.GoodWrapperId);

            modelBuilder.Entity<ValueModifier>()
                .HasOne(m => m.ModifierWrapper)
                .WithMany(m => m.Modifiers)
                .HasForeignKey(m => m.ModifierWrapperId);

            modelBuilder.Entity<Value>()
                .HasOne(v => v.Province)
                .WithMany(p => p.Values)
                .HasForeignKey(v => v.ProvinceId);
            modelBuilder.Entity<ValueHistory>()
                .HasOne(h => h.Value)
                .WithMany(v => v.PastValues)
                .HasForeignKey(h => h.ValueId);
                
            modelBuilder.Entity<Province>()
                .HasOne(p => p.Nation)
                .WithMany(n => n.Provinces)
                .HasForeignKey(p => p.NationId);
                
            modelBuilder.Entity<GameState>()
                .HasOne(g => g.Nation);

            modelBuilder.Entity<GameState>()
                .Property(g => g.GameStateId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<CharacterToken>()
                .HasOne(ct => ct.TokenType)
                .WithMany()
                .HasForeignKey(ct => ct.TokenTypeId);

            modelBuilder.Entity<CharacterToken>()
                .HasOne(ct => ct.Character)
                .WithMany(c => c.Tokens)
                .HasForeignKey(ct => ct.PlayerCharacterId);

            modelBuilder.Entity<PlayerCharacter>()
                .HasOne(pc => pc.DiscordUser)
                .WithMany(du => du.Characters)
                .HasForeignKey(pc => pc.DiscordUserId);

            modelBuilder.Entity<PlayerCharacter>()
                .HasOne(pc => pc.GroupOfInterest)
                .WithMany(goi => goi.Characters)
                .HasForeignKey(pc => pc.GoIid);

            modelBuilder.Entity<CharacterToken>().HasKey(ct => new { ct.PlayerCharacterId, ct.TokenTypeId });

			modelBuilder.Entity<StaffAction>()
				.HasOne(sa => sa.Submitter)
				.WithMany(du => du.CreatedStaffActions)
				.HasForeignKey(sa => sa.SubmitterId);

			modelBuilder.Entity<StaffAction>()
				.HasOne(sa => sa.AssignedTo)
				.WithMany(du => du.AssignedStaffActions)
				.HasForeignKey(sa => sa.AssignedToId);
        }
    }
}
