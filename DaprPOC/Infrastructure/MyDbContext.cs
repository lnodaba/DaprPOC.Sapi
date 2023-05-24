using DaprPOC.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DaprPOC.Infrastructure
{
    public class MyDbContext : DbContext
    {
        protected readonly IConfiguration Configuration;

        public MyDbContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // connect to postgres with connection string from app settings
            options.UseNpgsql(Configuration.GetConnectionString("WebApiDatabase"));
        }

        public DbSet<Cage> Cages { get; set; }
    }
}
