using utube.Data;
using utube.Models;
using Microsoft.EntityFrameworkCore;

namespace utube.Repositories
{
    public interface IEncodingProfileRepository
    {
        Task<EncodingProfile> GetByIdAsync(Guid id);
        Task AddAsync(EncodingProfile profile);
        Task UpdateAsync(EncodingProfile profile);
        Task DeleteAsync(EncodingProfile profile);
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

        public async Task UpdateAsync(EncodingProfile profile)
        {
            // Load existing profile with formats from DB
            var existing = await _context.EncodingProfiles
                .Include(p => p.Formats)
                .FirstOrDefaultAsync(p => p.Id == profile.Id);

            if (existing == null)
                throw new Exception("Profile not found");

            // Update scalar properties
            existing.ProfileName = profile.ProfileName;
            existing.Resolutions = profile.Resolutions;
            existing.BitratesKbps = profile.BitratesKbps;

            // Remove old formats
            foreach (var format in profile.Formats.ToList())
            {
                _context.Formats.Remove(format); // ✅ safe now
            }


            // Add new formats
            foreach (var format in profile.Formats)
            {
                existing.Formats.Add(new Format
                {
                    Id = Guid.NewGuid(),
                    FormatType = format.FormatType,
                    EncodingProfile = profile
                });
            }

            await _context.SaveChangesAsync();
        }


        public async Task DeleteAsync(EncodingProfile profile)
        {
            _context.EncodingProfiles.Remove(profile);
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
