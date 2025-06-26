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
        Task<Video?> MarkUploadCompleteAsync(Guid videoId);
        Task<Video?> UpdatePublicUrlAsync(Guid videoId, string publicUrl);
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
         .FirstOrDefaultAsync(v => v.Id == videoId);
        }

        public async Task<Video?> MarkUploadCompleteAsync(Guid videoId)
        {
            var video = await _context.Videos.FindAsync(videoId);
            if (video == null) return null;

            video.Status = VideoStatus.Uploaded;
            //video.FilePath = $"videos/{video.Id}/{video.OriginalFilename}";
            video.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return video;
        }
        public async Task<Video?> UpdatePublicUrlAsync(Guid videoId, string publicUrl)
        {
            var video = await _context.Videos.FindAsync(videoId);

            if (video == null) return null;

            Console.WriteLine($"Updating public URL for video ID: {video.Id} to {publicUrl}");

            video.PublicUrl = publicUrl;
           

          
            await _context.SaveChangesAsync();

            return video;
        }




    }
}
