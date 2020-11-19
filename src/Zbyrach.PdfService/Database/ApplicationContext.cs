using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Zbyrach.Pdf
{
    public class ApplicationContext : DbContext
    {
        public DbSet<ArticleModel> Articles { get; set; }

        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ArticleModel>()
               .Property(a => a.Id)
               .IsRequired();
            modelBuilder.Entity<ArticleModel>()
               .Property(a => a.Url)
               .IsRequired();
            modelBuilder.Entity<ArticleModel>()
               .HasIndex(a => new { a.Url, a.DeviceType, a.Inlined });
            modelBuilder.Entity<ArticleModel>()
               .HasIndex(a => a.StoredAt);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false, true)
                    .AddJsonFile("appsettings.Development.json", true)
                    .AddEnvironmentVariables()
                    .Build();

                optionsBuilder.UseNpgsql(config.GetConnectionString());
            }
        }

    }
}