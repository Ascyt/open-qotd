using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenQotd.Bot.Database.Entities;

namespace OpenQotd.Bot.Database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() { }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Config> Configs { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<PresetSent> PresetSents { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string? password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
                if (string.IsNullOrEmpty(password))
                {
                    throw new InvalidOperationException("POSTGRES_PASSWORD environment variable is not set.");
                }

                string connection = $"User ID=root;Password={password};Host=localhost;Port=5432;Database=openqotd;Pooling=true;MaxPoolSize=100;Connection Lifetime=0;";

                optionsBuilder.UseNpgsql(connection);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder
                .Properties<ulong>()
                .HaveConversion<DiscordSnowflakeToLongConverter>();
            configurationBuilder
                .Properties<ulong?>()
                .HaveConversion<NullableDiscordSnowflakeToLongConverter>();
        }

        public class DiscordSnowflakeToLongConverter : ValueConverter<ulong, long>
        {
            public DiscordSnowflakeToLongConverter() : base(
                v => (long)v,
                v => (ulong)v)
            { }
        }
        public class NullableDiscordSnowflakeToLongConverter : ValueConverter<ulong?, long?>
        {
            public NullableDiscordSnowflakeToLongConverter() : base(
                v => !v.HasValue ? null : (long)v.Value,
                v => !v.HasValue ? null : (ulong)v.Value)
            { }
        }
    }
}