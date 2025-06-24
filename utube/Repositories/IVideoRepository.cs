using Microsoft.EntityFrameworkCore;
using utube.Data; // Assuming your DbContext is here
using utube.Enums;
using utube.Models;

namespace utube.Repositories
{
    public interface IVideoRepository
    {
        Task<Video> CreateAsync(Video video);
        Task<Video> UpdateStatusAsync(Guid videoId, VideoStatus newStatus);
        Task<Video?> GetByIdAsync(Guid videoId);
    }
    public class VideoRepository : IVideoRepository
    {
        private readonly AppDbContext _context;

        public VideoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Video> CreateAsync(Video video)
        {
            if (video.Id == Guid.Empty)
            {
                video.Id = Guid.NewGuid();
            }
            _context.Videos.Add(video);
            await _context.SaveChangesAsync();
            return video;
        }

        public async Task<Video> UpdateStatusAsync(Guid videoId, VideoStatus newStatus)
        {
            var video = await _context.Videos.FindAsync(videoId);
            if (video == null)
            {
                throw new InvalidOperationException("Video not found.");
            }

            video.Status = newStatus;
            video.UpdatedAt = DateTime.UtcNow;

            _context.Videos.Update(video);
            await _context.SaveChangesAsync();

            return video;
        }

        public async Task<Video?> GetByIdAsync(Guid videoId)
        {
            return await _context.Videos
                .Include(v => v.Chunks)
                .FirstOrDefaultAsync(v => v.Id == videoId);
        }
    }
}
