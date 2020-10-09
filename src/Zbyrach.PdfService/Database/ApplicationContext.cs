using Microsoft.EntityFrameworkCore;

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
        }
    }
}