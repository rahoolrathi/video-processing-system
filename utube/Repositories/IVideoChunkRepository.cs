// VideoChunkRepository.cs
using Microsoft.EntityFrameworkCore;
using utube.Data; // Assuming your DbContext is here
using utube.Enums;
using utube.Models;

namespace utube.Repositories
{
    public interface IVideoChunkRepository
    {
        Task<VideoChunk> CreateAsync(VideoChunk chunk);
        Task<IEnumerable<VideoChunk>> GetChunksByVideoIdAsync(Guid videoId);
        Task<VideoChunk?> GetChunkByIndexAsync(Guid videoId, int chunkIndex);
        Task<VideoChunk> UpdateStatusAsync(Guid chunkId, ChunkStatus newStatus);
        Task<List<VideoChunk>> GetChunksByStatusAsync(Guid videoId, ChunkStatus status);

    }
    public class VideoChunkRepository : IVideoChunkRepository
    {
        private readonly AppDbContext _context;

        public VideoChunkRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<VideoChunk> CreateAsync(VideoChunk chunk)
        {

            _context.VideoChunks.Add(chunk);
            await _context.SaveChangesAsync();
            return chunk;
        }

        public async Task<IEnumerable<VideoChunk>> GetChunksByVideoIdAsync(Guid videoId)
        {
            return await _context.VideoChunks
                .Where(c => c.VideoId == videoId)
                .OrderBy(c => c.ChunkIndex)
                .ToListAsync();
        }

        public async Task<VideoChunk?> GetChunkByIndexAsync(Guid videoId, int chunkIndex)
        {
            return await _context.VideoChunks
                .FirstOrDefaultAsync(c => c.VideoId == videoId && c.ChunkIndex == chunkIndex);
        }

        public async Task<VideoChunk> UpdateStatusAsync(Guid chunkId, ChunkStatus newStatus)
        {
            var chunk = await _context.VideoChunks.FindAsync(chunkId);
            if (chunk == null)
                throw new InvalidOperationException("Chunk not found.");

            chunk.Status = newStatus;
            _context.VideoChunks.Update(chunk);
            await _context.SaveChangesAsync();
            return chunk;
        }

        public async Task<List<VideoChunk>> GetChunksByStatusAsync(Guid videoId, ChunkStatus status)
        {
            var chunks = await _context.VideoChunks
    .Where(c => c.VideoId == videoId && c.Status == status)
    .OrderBy(c => c.ChunkIndex)
    .ToListAsync();



            // Log each chunk to the console or debugger
            foreach (var c in chunks)
            {
                Console.WriteLine($"ChunkIndex: {c.ChunkIndex}, Status: {c.Status}, VideoId: {c.VideoId}");
            }

            return chunks;

        }
    }
}
