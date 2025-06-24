using System.Security.Cryptography;
using utube.Enums;
using utube.Models;
using utube.Repositories;
namespace utube.Services
{
    public interface IVideoUploadService
    {
        Task<Guid> PrepareVideoMetaDataForUploadAsync(Video video);
        Task UploadChunkAsync(Guid videoId, int chunkIndex, IFormFile chunk, bool isLastChunk);
        Task<List<int>> GetUploadedChunkIndexesAsync(Guid videoId);


    }

    public class VideoUploadService : IVideoUploadService
    {
        private readonly IVideoRepository _videoRepository;
        private readonly IVideoChunkRepository _chunkRepository;
        private readonly IRabbitMqPublisherService _rabbitMqPublisher;
        private readonly ElasticSearchService _elasticSearchService;
        public VideoUploadService(IVideoRepository videoRepo, IVideoChunkRepository chunkRepo, IRabbitMqPublisherService rabbitMqPublisher, ElasticSearchService elasticSearchService)
        {
            _videoRepository = videoRepo;
            _chunkRepository = chunkRepo;
            _rabbitMqPublisher = rabbitMqPublisher;
            _elasticSearchService = elasticSearchService;
        }


        public async Task<Guid> PrepareVideoMetaDataForUploadAsync(Video video)
        {

            using var rng = RandomNumberGenerator.Create();
            byte[] keyBytes = new byte[16];
            rng.GetBytes(keyBytes);
            string hexKey = BitConverter.ToString(keyBytes).Replace("-", "").ToLowerInvariant();

            // 🔐 Generate UUID for Key ID
            string keyId = Guid.NewGuid().ToString();

            // 📝 Assign to video
            video.EncryptionKey = hexKey;
            video.KeyId = keyId;

            var created = await _videoRepository.CreateAsync(video);
            //video inserted into elastic search db
            var document = new VideoDocument
            {
                Id = created.Id,
                name = created.OriginalFilename,
                UploadedAt = DateTime.UtcNow,
                videopath = null,             
                SelectedImageName = null,
                Formats = null                 
            };

            await _elasticSearchService.IndexDocumentAsync(document);
            return created.Id;
        }


        public async Task UploadChunkAsync(Guid videoId, int chunkIndex, IFormFile chunk, bool isLastChunk)
        {
            var existingChunk = await _chunkRepository.GetChunkByIndexAsync(videoId, chunkIndex);

            if (existingChunk == null)
            {
                existingChunk = new VideoChunk
                {
                    Id = Guid.NewGuid(),
                    VideoId = videoId,
                    ChunkIndex = chunkIndex,
                    Status = ChunkStatus.Pending,
                    IsLastChunk = isLastChunk,
                    CreatedAt = DateTime.UtcNow
                };

                await _chunkRepository.CreateAsync(existingChunk);
            }

            var uploadFolder = Path.Combine("Uploads", videoId.ToString());
            Directory.CreateDirectory(uploadFolder);
            var chunkPath = Path.Combine(uploadFolder, $"{chunkIndex}.part");

            using (var stream = new FileStream(chunkPath, FileMode.Create))
            {
                await chunk.CopyToAsync(stream);
            }

            await _chunkRepository.UpdateStatusAsync(existingChunk.Id, ChunkStatus.Uploaded);

            if (isLastChunk)
            {
                var video = await _videoRepository.GetByIdAsync(videoId);
                if (video != null)
                {
                    var mergedFolder = Path.Combine("Merged", videoId.ToString());
                    Directory.CreateDirectory(mergedFolder);

                    var mergedFilePath = Path.Combine(mergedFolder, video.OriginalFilename);

                    using (var outputStream = new FileStream(mergedFilePath, FileMode.Create))
                    {
                        for (int i = 0; i < video.TotalChunks; i++)
                        {
                            var chunkFilePath = Path.Combine(uploadFolder, $"{i}.part");
                            if (!File.Exists(chunkFilePath))
                                throw new FileNotFoundException($"Chunk {i} not found");

                            var bytes = await File.ReadAllBytesAsync(chunkFilePath);
                            await outputStream.WriteAsync(bytes);
                        }
                    }

                    await _videoRepository.UpdateStatusAsync(video.Id, VideoStatus.Uploaded);
                    Console.WriteLine(mergedFilePath);

                }
            }
        }

        public async Task<List<int>> GetUploadedChunkIndexesAsync(Guid videoId)
        {
            var chunks = await _chunkRepository.GetChunksByStatusAsync(videoId, ChunkStatus.Uploaded);

            return chunks.Select(c => c.ChunkIndex).ToList();
        }

    }
}
