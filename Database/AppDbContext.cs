using CustomQotd.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustomQotd.Database
{
    public class AppDbContext : DbContext
    {
        public DbSet<Config> Configs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=app.db");
        }
    }
}
