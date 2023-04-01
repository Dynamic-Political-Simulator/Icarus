using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Icarus.Context.Models;

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
                + "Trusted_Connection=false;"
                + "MultipleActiveResultSets=true");
        }

        public DbSet<Gamestate> Gamestates { get; set; }
        public DbSet<Value> Values { get; set; }
        public DbSet<ValueModifier> ValueModifiers { get; set; }
        public DbSet<ValueRelationship> Relationships { get; set; }
        public DbSet<Nation> Nations { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<Modifier> Modifiers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ValueModifier>()
                .HasOne(m => m.ModifierWrapper)
                .WithMany(m => m.Modifiers)
                .HasForeignKey(m => m.ModifierWrapperId);
            modelBuilder.Entity<ValueRelationship>()
                .HasOne(vr => vr.Origin);
            modelBuilder.Entity<ValueRelationship>()
                .HasOne(vr => vr.Target);
            modelBuilder.Entity<Value>()
                .HasOne(v => v.Province)
                .WithMany(p => p.Values)
                .HasForeignKey(v => v.ProvinceId);
            modelBuilder.Entity<Province>()
                .HasOne(p => p.Nation)
                .WithMany(n => n.Provinces)
                .HasForeignKey(p => p.NationId);
            modelBuilder.Entity<Gamestate>()
                .HasOne(g => g.Nation);
        }
    }
}
