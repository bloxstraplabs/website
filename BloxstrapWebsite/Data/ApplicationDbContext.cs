using Microsoft.EntityFrameworkCore;

using BloxstrapWebsite.Data.Entities;

namespace BloxstrapWebsite.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IConfiguration _configuration;

        public ApplicationDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(_configuration.GetConnectionString("Postgres"));
            optionsBuilder.UseSnakeCaseNamingConvention();
        }

        public DbSet<ExceptionReport> ExceptionReports { get; set; }
    }
}
