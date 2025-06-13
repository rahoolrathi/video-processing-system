using Microsoft.EntityFrameworkCore;
using utube.Models;
using utube.Enums; // If your enum is in a separate folder

namespace utube.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Video> Videos { get; set; } = null!;
        public DbSet<VideoChunk> VideoChunks { get; set; } = null!;
        public DbSet<EncodingProfile> EncodingProfiles { get; set; }
        public DbSet<Format> Formats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Store FormatType enum as string in database
            modelBuilder.Entity<Format>()
                        .Property(f => f.FormatType)
                        .HasConversion<string>();
            modelBuilder.Entity<Video>()
                       .Property(v => v.Status)
                       .HasConversion<string>();

            modelBuilder.Entity<VideoChunk>()
                     .Property(v => v.Status)
                     .HasConversion<string>();
        }
    }
}
