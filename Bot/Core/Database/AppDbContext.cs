using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenQotd.Core.Configs.Entities;
using OpenQotd.Core.Pools.Entities;
using OpenQotd.Core.Presets.Entities;
using OpenQotd.Core.Profiles;
using OpenQotd.Core.Questions.Entities;
using System.Text;

namespace OpenQotd.Core.Database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() { }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Config> Configs { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<PresetSent> PresetSents { get; set; }
        public DbSet<ProfileSelection> ProfileSelections { get; set; }
        public DbSet<Pool> Pools { get; set; }
        public DbSet<PoolEntry> PoolEntries { get; set; }

        private static string? _postgresConnectionString = null;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_postgresConnectionString == null)
            {
                string? connectionFromEnv = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING_OVERRIDE");
                if (!string.IsNullOrEmpty(connectionFromEnv))
                {
                    Console.WriteLine("Connection string provided from `POSTGRES_CONNECTION_STRING_OVERRIDE` environment variable.");
                    _postgresConnectionString = connectionFromEnv;
                }
                else
                {
                    string? password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
                    if (string.IsNullOrEmpty(password))
                    {
                        throw new InvalidOperationException("POSTGRES_PASSWORD environment variable is not set.");
                    }

                    StringBuilder connection = new(Program.AppSettings.PostgresConnectionStringNoPassword.Trim());
                    if (!connection.ToString().EndsWith(';'))
                    {
                        connection.Append(';');
                    }
                    connection.Append($"Password={password}");

                    _postgresConnectionString = connection.ToString();
                }
            }

            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(_postgresConnectionString);
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