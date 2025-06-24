using Microsoft.EntityFrameworkCore;
using utube.Models;


namespace utube.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Video> Videos { get; set; } = null!;
        public DbSet<VideoChunk> VideoChunks { get; set; } = null!;
        public DbSet<EncodingProfile> EncodingProfiles { get; set; }
        public DbSet<TranscodeJob> TranscodeJob { get; set; }
        public DbSet<Format> Formats { get; set; }
        public DbSet<ThumbnailJob> ThumbnailJobs { get; set; }
        public DbSet<Watermarking> WatermarkingJobs { get; set; }

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
            modelBuilder.Entity<TranscodeJob>()
                    .Property(t => t.Status)
                    .HasConversion<string>();
            modelBuilder.Entity<ThumbnailJob>()
                   .Property(t => t.Status)
                   .HasConversion<string>();
            modelBuilder.Entity<Watermarking>()
                   .Property(t => t.Status);


        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Modified)
                {
                    if (entry.Entity is TranscodeJob job)
                    {
                        job.UpdatedAt = now;
                    }
                    else if (entry.Entity is ThumbnailJob job2)
                    {
                        job2.UpdatedAt = now;
                    }
                    else if (entry.Entity is Watermarking job3)
                    {
                        job3.UpdatedAt = now;



                    }


                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

    }
}
