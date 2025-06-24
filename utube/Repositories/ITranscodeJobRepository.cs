using Microsoft.EntityFrameworkCore;
using utube.Data;
using utube.Models;

namespace utube.Repositories
{
    public interface ITranscodeJobRepository
    {
        Task AddAsync(TranscodeJob job);
        Task<TranscodeJob> GetByVideoAndProfileAsync(Guid videoId, Guid profileId);
        Task UpdateAsync(TranscodeJob job);
    }


    public class TranscodeJobRepository : ITranscodeJobRepository
    {
        private readonly AppDbContext _context;

        public TranscodeJobRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(TranscodeJob job)
        {
            await _context.TranscodeJob.AddAsync(job);
            await _context.SaveChangesAsync();
        }

        public async Task<TranscodeJob> GetByVideoAndProfileAsync(Guid videoId, Guid profileId)
        {
            return await _context.TranscodeJob
                .FirstOrDefaultAsync(j => j.VideoId == videoId && j.ProfileId == profileId);
        }

        public async Task UpdateAsync(TranscodeJob job)
        {
            _context.TranscodeJob.Update(job);
            await _context.SaveChangesAsync();
        }
    }
}


