using global::utube.DTOs.utube.Dtos;
using Microsoft.AspNetCore.Mvc;
using utube.Dtos;
using utube.Enums;
using utube.Models;
using utube.Repositories;
using utube.Services;

namespace utube.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TranscoderController : ControllerBase
    {
        private readonly IRabbitMqPublisherService _rabbitMqPublisher;
        private readonly IVideoRepository _videoRepository;
        private readonly ITranscodeJobRepository _transcodeJobRepository;

        public TranscoderController(
            IRabbitMqPublisherService rabbitMqPublisher,
            IVideoRepository videoRepository,
            ITranscodeJobRepository transcodeJobRepository)
        {
            _rabbitMqPublisher = rabbitMqPublisher;
            _videoRepository = videoRepository;
            _transcodeJobRepository = transcodeJobRepository;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartTranscoding([FromBody] TranscodingRequestDto request)
        {
            if (request == null || request.VideoId == Guid.Empty || request.EncodingProfileId == Guid.Empty)
            {
                return BadRequest("Invalid request payload.");
            }

            var video = await _videoRepository.GetByIdAsync(request.VideoId);
            if (video == null)
            {
                return NotFound($"Video with ID {request.VideoId} not found.");
            }
            Console.WriteLine($"Transcoding request for video: {video.PublicUrl} with profile ID: {request.EncodingProfileId}");
            var mergedFilePath = video.PublicUrl;//Path.Combine("Merged", request.VideoId.ToString(), video.OriginalFilename);

            var message = new TranscodingJobMessageDto
            {
                VideoId = request.VideoId,
                VideoPath = mergedFilePath,
                EncodingProfileId = request.EncodingProfileId,
                EncryptionKey = video.EncryptionKey,
                KeyId = video.KeyId
            };



            try
            {
                // 1. Publish to RabbitMQ
                await _rabbitMqPublisher.Publish("transcoding-queue", message);
                //job2


            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Failed to publish job to queue: {ex.Message}");
            }

            try
            {
                // 2. Save to DB only if publish succeeded
                var transcodeJob = new TranscodeJob
                {
                    Id = Guid.NewGuid(),
                    VideoId = request.VideoId,
                    ProfileId = request.EncodingProfileId,
                    Status = JobStatus.Queued,
                    WorkerId = "unassigned",
                    CreatedAt = DateTime.UtcNow
                };

                await _transcodeJobRepository.AddAsync(transcodeJob);
            }
            catch (Exception dbEx)
            {
                return StatusCode(500, $"✅ Published to queue, but failed to save transcode job in DB: {dbEx.Message}");


            }

            return Ok(new { message = "✅ Transcoding job published and saved successfully." });
        }


        [HttpGet("status")]
        public async Task<IActionResult> GetStatus(Guid videoId, Guid profileId)
        {
            var job = await _transcodeJobRepository.GetByVideoAndProfileAsync(videoId, profileId);
            if (job == null)
                return NotFound(new { message = "Transcode job not found." });

            return Ok(new
            {
                videoId = job.VideoId,
                profileId = job.ProfileId,
                status = job.Status.ToString(),
                workerId = job.WorkerId,
                error = job.ErrorMessage
            });
        }
    }
}
