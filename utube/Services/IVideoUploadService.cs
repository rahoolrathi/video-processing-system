using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using utube.DTOs.utube.Dtos;
using utube.Enums;
using utube.Models;
using utube.Repositories;
namespace utube.Services
{
    public interface IVideoUploadService
    {
        Task<Guid> InitializeVideoAsync(Video video);
        Task UploadChunkAsync(Guid videoId, int chunkIndex, IFormFile chunk, bool isLastChunk);
        Task<List<int>> GetUploadedChunkIndexesAsync(Guid videoId);

    }

    public class VideoUploadService : IVideoUploadService
    {
        private readonly IVideoRepository _videoRepository;
        private readonly IVideoChunkRepository _chunkRepository;
        private readonly IRabbitMqPublisherService _rabbitMqPublisher;

        public VideoUploadService(IVideoRepository videoRepo, IVideoChunkRepository chunkRepo, IRabbitMqPublisherService rabbitMqPublisher)
        {
            _videoRepository = videoRepo;
            _chunkRepository = chunkRepo;
            _rabbitMqPublisher = rabbitMqPublisher;

        }

        public async Task<Guid> InitializeVideoAsync(Video video)
        {
            var created = await _videoRepository.CreateAsync(video);
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
                    var message = new TranscodingJobMessageDto
                    {
                        VideoId = video.Id,
                        VideoPath = mergedFilePath,
                        EncodingProfileId = Guid.Parse("f81d4fae-7dec-11d0-a765-00a0c91e6bf6")
                    };

                    // ✅ Publish to RabbitMQ
                    await _rabbitMqPublisher.Publish("transcoding-queue", message);
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
