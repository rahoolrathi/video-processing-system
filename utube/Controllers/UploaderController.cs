using Microsoft.AspNetCore.Mvc;
using utube.DTOs;
using utube.Models;
using utube.Services;

namespace utube.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploaderController : ControllerBase
    {
        private readonly IVideoUploadService _uploadService;

        public UploaderController(IVideoUploadService uploadService)
        {
            _uploadService = uploadService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateVideo([FromBody] Video video)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var videoId = await _uploadService.PrepareVideoMetaDataForUploadAsync(video);
            return Ok(new { id = videoId });
        }

        [HttpPost("chunk")]
        public async Task<IActionResult> UploadChunk([FromForm] VideoChunkDto dto)
        {
            if (dto.Chunk == null || dto.Chunk.Length == 0)
                return BadRequest("Chunk file missing.");

            await _uploadService.UploadChunkAsync(dto.VideoId, dto.ChunkIndex, dto.Chunk, dto.IsLastChunk);

            return Ok(new { message = "Chunk uploaded and tracked." });
        }

        [HttpGet("status/{videoId}")]
        public async Task<IActionResult> GetUploadedChunks(Guid videoId)
        {
            var uploadedChunkIndexes = await _uploadService.GetUploadedChunkIndexesAsync(videoId);

            return Ok(new { completedChunks = uploadedChunkIndexes });
        }
    }
}
