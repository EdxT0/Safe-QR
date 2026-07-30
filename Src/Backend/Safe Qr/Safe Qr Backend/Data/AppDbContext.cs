using Microsoft.EntityFrameworkCore;
using Safe_Qr_Backend.Entities;

namespace Safe_Qr_Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<UrlReport> UrlReport => Set<UrlReport>();
        public DbSet<User> User => Set<User>();
        public DbSet<ScanHistory> ScanHistory => Set<ScanHistory>();
        public DbSet<ThreatFeedback> ThreatFeedback => Set<ThreatFeedback>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
