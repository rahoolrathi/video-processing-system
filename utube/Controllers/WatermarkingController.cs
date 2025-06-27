using Microsoft.AspNetCore.Mvc;
using utube.DTOs;
using utube.Enums;
using utube.Repositories;
using utube.Services;

namespace utube.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WatermarkingController : Controller
    {


        private readonly IWatermarkingRepository _watermarking;
        private readonly IRabbitMqPublisherService _rabbitMqPublisher;
        private readonly IVideoRepository _videoRepository;
        private readonly SignedUrlGeneratorService _signedUrlService;
        public WatermarkingController(IWatermarkingRepository watermarking,IRabbitMqPublisherService rabbitMqPublisherService, IVideoRepository videoRepository, SignedUrlGeneratorService signedUrlService)
        {
            _watermarking = watermarking;
            _rabbitMqPublisher = rabbitMqPublisherService;
            _videoRepository = videoRepository;
            _signedUrlService = signedUrlService;
        }


        ///user will send the video id and text for atermarking and i will publish job in the queue
        ///
        [HttpPost("start")]
        public async Task<IActionResult> StartWatermarking([FromBody] WatermarkingRequestDto request)
        {
            if (request == null || request.VideoId == Guid.Empty || string.IsNullOrWhiteSpace(request.text))
                return BadRequest("Invalid watermarking request.");

            // 1. Save watermark job to DB
            var job = await _watermarking.AddAsync(request);
            var video = await _videoRepository.GetByIdAsync(request.VideoId);
            if (video == null)
            {
                return NotFound($"Video with ID {request.VideoId} not found.");
            }

            var mergedFilePath = video.PublicUrl;//Path.Combine("Merged", request.VideoId.ToString(), video.OriginalFilename);

            // 2. Create job DTO for RabbitMQ
            var message = new WatermarkingJobDto
            {
                JobId = job.Id,
                VideoId = job.VideoId,
                Text = job.text,
                VideoPath = mergedFilePath,
                filename = video.OriginalFilename

            };

            try
            {
                Console.WriteLine($"[WatermarkingController] Publishing job to queue: {message.JobId} for video {message.VideoId}");

                await _rabbitMqPublisher.Publish("Watermarking-queue", message);
                


            }
            catch (Exception ex)
            {
                return StatusCode(500, $"❌ Failed to publish job to queue: {ex.Message}");
            }

            return Ok(new { message = "Watermarking job started", jobId = job.Id });
        }
        [HttpGet("status/{jobId}")]
        public async Task<IActionResult> GetStatus(Guid jobId)
        {
            try
            {
                var status = await _watermarking.GetStatus(jobId);

                return Ok(new
                {
                    JobId = jobId,
                    status = status.ToString()
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var jobs = await _watermarking.GetAllCompletedJobsAsync();

                if (jobs == null || !jobs.Any())
                {
                    return NotFound(new { message = "No completed watermarking jobs found." });
                }

                var result = jobs.Select(job => new
                {
                    job.Id,
                    job.VideoId,
                    job.Status,
                    job.text,
                    SignedWatermarkUrl = string.IsNullOrEmpty(job.WatermarkPath)
                        ? null
                        : _signedUrlService.GetSignedVideoUrl(job.WatermarkPath)
                }).ToList();
                Console.WriteLine(result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Something went wrong.", error = ex.Message });
            }
        }



    }



}

