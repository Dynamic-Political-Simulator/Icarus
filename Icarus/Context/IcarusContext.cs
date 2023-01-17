using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Icarus.Context.Models;

namespace Icarus.Context
{
    public class IcarusContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var config = new IcarusConfig();
            Program.Configuration.GetSection("StewardConfig").Bind(config);

            optionsBuilder.UseSqlServer($"Server={config.DatabaseIp};"
                + $"Database={config.DatabaseName};"
                + $"User Id={config.SqlUsername};"
                + $"Password={config.SqlPassword};"
                + "MultipleActiveResultSets=true");
        }

        public DbSet<Value> Values { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ValueModifier>()
                .HasOne(vm => vm.Value)
                .WithMany(v => v.Modifiers)
                .HasForeignKey(vm => vm.ValueId);
        }
    }
}
