using Microsoft.EntityFrameworkCore;
using utube.Data;
using utube.Models;

namespace utube.Repositories
{
    public interface IEncodingProfileRepository
    {
        Task<EncodingProfile> GetByIdAsync(Guid id);
        Task AddAsync(EncodingProfile profile);

        Task<List<EncodingProfile>> GetAllAsync();

    }

    public class EncodingProfileRepository : IEncodingProfileRepository
    {
        private readonly AppDbContext _context;

        public EncodingProfileRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<EncodingProfile> GetByIdAsync(Guid id)
        {
            return await _context.EncodingProfiles.Include(p => p.Formats)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AddAsync(EncodingProfile profile)
        {
            _context.EncodingProfiles.Add(profile);
            await _context.SaveChangesAsync();
        }


        public async Task<List<EncodingProfile>> GetAllAsync()
        {
            return await _context.EncodingProfiles
                .Include(p => p.Formats)
                .ToListAsync();
        }

    }

}
