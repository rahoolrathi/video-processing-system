using Microsoft.AspNetCore.Mvc;
using utube.DTOs;
using utube.Services;

namespace utube.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ThumbnailController : ControllerBase
    {
        private readonly ThumbnailService _service;
        private readonly ElasticSearchService _elasticSearch;

        public ThumbnailController(ThumbnailService service, ElasticSearchService elasticSearch)
        {
            _service = service;
            _elasticSearch = elasticSearch;
        }



        [HttpPost("start")]
        public async Task<IActionResult> StartJob([FromBody] ThumbnailJobCreateDto dto)
        {
            var jobId = await _service.StartJobAsync(dto.VideoId);
            return Ok(new { JobId = jobId });
        }

        [HttpGet("status/{jobId}")]
        public async Task<IActionResult> GetStatus(Guid jobId)
        {
            var status = await _service.GetStatusAsync(jobId);
            return status == null ? NotFound() : Ok(status);
        }

        [HttpGet("count/{jobId}")]
        public async Task<IActionResult> GetThumbnailCount(Guid jobId)
        {
            var job = await _service.GetJobAsync(jobId);
            if (job == null)
                return NotFound();

            return Ok(new
            {
                Count = job.NumberOfImages
            });
        }

        [HttpPost("select")]
        public async Task<IActionResult> SelectThumbnail([FromBody] ThumbnailSelectionDto dto)
        {
            var job = await _service.GetJobAsync(dto.JobId);
            if (job == null) return NotFound();

            job.SelectedImageName = dto.SelectedImageName;
            job.UpdatedAt = DateTime.UtcNow;
            Console.WriteLine($"[ThumbnailController] Selecting thumbnail: {dto.SelectedImageName} for job {dto.JobId}");
            await _service.UpdateJobAsync(job);

            // Update in Elasticsearch
            try
            {
                await _elasticSearch.UpdateSelectedImageNameAsync(job.VideoId, dto.SelectedImageName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Elasticsearch] Failed to update thumbnail: {ex.Message}");
            }
            return Ok(new { message = "Thumbnail selected successfully" });
        }

    }
}
