using utube.Data;
using utube.Models;

namespace utube.Repositories
{
    public interface IThumbnailJobRepository
    {
        Task<ThumbnailJob> CreateAsync(ThumbnailJob job);
        Task<ThumbnailJob?> GetByIdAsync(Guid jobId);
        Task UpdateAsync(ThumbnailJob job);
    }

    public class ThumbnailJobRepository : IThumbnailJobRepository
    {
        private readonly AppDbContext _context;
        public ThumbnailJobRepository(AppDbContext context) => _context = context;

        public async Task<ThumbnailJob> CreateAsync(ThumbnailJob job)
        {
            _context.ThumbnailJobs.Add(job);
            await _context.SaveChangesAsync();
            return job;
        }

        public async Task<ThumbnailJob?> GetByIdAsync(Guid jobId)
            => await _context.ThumbnailJobs.FindAsync(jobId);

        public async Task UpdateAsync(ThumbnailJob job)
        {
            _context.ThumbnailJobs.Update(job);
            await _context.SaveChangesAsync();
        }


    }


}
