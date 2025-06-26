using utube.DTOs;
using utube.Enums;
using utube.Models;
using utube.Repositories;

namespace utube.Services
{
    public class ThumbnailService
    {
        private readonly IThumbnailJobRepository _repo;
        private readonly IRabbitMqPublisherService _mq;
        private readonly IVideoRepository _vr;
        public ThumbnailService(IThumbnailJobRepository repo, IRabbitMqPublisherService mq, IVideoRepository vr)
        {
            _repo = repo;
            _mq = mq;
            _vr = vr;
        }

        public async Task<Guid> StartJobAsync(Guid videoId)
        {
            var video = await _vr.GetByIdAsync(videoId);
            if (video == null)
                throw new Exception("Video not found");
            var mergedFilePath = video.PublicUrl;//Path.Combine("Merged", videoId.ToString(), video.OriginalFilename);
            var job = new ThumbnailJob
            {
                VideoId = videoId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = JobStatus.Queued,
                NumberOfImages = 0,
                FilePath = mergedFilePath // ✅ Required non-null value
            };

            await _repo.CreateAsync(job);


            var message = new ThumbnailJobMessageDto
            {
                JobId = job.Id,
                VideoId = videoId,
                VideoPath = mergedFilePath
            };


            _mq.Publish("thumbnail-generation-queue", message);
            job.Status = JobStatus.Processing;
            await _repo.UpdateAsync(job);
            return job.Id;
        }

        public async Task<object?> GetStatusAsync(Guid jobId)
        {
            var job = await _repo.GetByIdAsync(jobId);
            if (job == null) return null;

            return new
            {
                status = job.Status.ToString()
            };
        }

        public async Task<ThumbnailJob?> GetJobAsync(Guid jobId)
        {
            return await _repo.GetByIdAsync(jobId);
        }

        public async Task UpdateJobAsync(ThumbnailJob job)
        {
            await _repo.UpdateAsync(job);
        }


    }

}
