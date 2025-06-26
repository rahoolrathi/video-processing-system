using System.Security.Cryptography;
using utube.Enums;
using utube.Models;
using utube.Repositories;
namespace utube.Services
{
    public interface IVideoUploadService
    {
        Task<Guid> PrepareVideoMetaDataForUploadAsync(Video video);
        //Task UploadChunkAsync(Guid videoId, int chunkIndex, IFormFile chunk, bool isLastChunk);
        //Task<List<int>> GetUploadedChunkIndexesAsync(Guid videoId);
        Task<Video?> CompleteUploadAsync(Guid videoId);
        Task UpdatePublicUrlAsync(Guid videoId, string signedUploadUrl);


    }

    public class VideoUploadService : IVideoUploadService
    {
        private readonly IVideoRepository _videoRepository;
     
        private readonly IRabbitMqPublisherService _rabbitMqPublisher;
        private readonly ElasticSearchService _elasticSearchService;
        public VideoUploadService(IVideoRepository videoRepo, IRabbitMqPublisherService rabbitMqPublisher, ElasticSearchService elasticSearchService)
        {
            _videoRepository = videoRepo;
        
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





        public async Task<Video?> CompleteUploadAsync(Guid videoId)
        {
            var video = await _videoRepository.MarkUploadCompleteAsync(videoId);
            if (video == null) return null;

            var defaultPath = $"videos/{video.Id}/{video.OriginalFilename}";
            await _elasticSearchService.UpdateDefaultPathAsync(video.Id, defaultPath);
            Console.WriteLine($"Upload completed for video: {video.PublicUrl} with ID: {video.Id}");
            return video;
        }

        public async Task UpdatePublicUrlAsync(Guid videoId, string signedUploadUrl)
        {
            try
            {
                await _videoRepository.UpdatePublicUrlAsync(videoId, signedUploadUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Exception in service while updating public URL: {ex.Message}");
                // Optionally rethrow or handle
                throw;
            }
        }



    }
}
