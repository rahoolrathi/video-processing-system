using Microsoft.AspNetCore.Mvc;
using utube.DTOs;
using utube.Models;
using utube.Repositories;
using utube.Services;

namespace utube.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploaderController : ControllerBase
    {
        private readonly IVideoUploadService _uploadService;
        private readonly AzureSignedUrlGenerator _signedUrlService;
       



        public UploaderController(IVideoUploadService uploadService, AzureSignedUrlGenerator signedUrlService)
        {
            _uploadService = uploadService;
            _signedUrlService = signedUrlService;
            

        }

        [HttpPost]
        public async Task<IActionResult> CreateVideo([FromBody] Video video)
        {
            Console.WriteLine($"Received video for upload: {video.OriginalFilename} ");
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var videoId = await _uploadService.PrepareVideoMetaDataForUploadAsync(video);
            Console.WriteLine($"Video metadata prepared with ID: {videoId}");
            var blobName = $"videos/{videoId}/{video.OriginalFilename}";
            var signedUploadUrl = _signedUrlService.GenerateUploadSasUrl(blobName);
            Console.WriteLine($"Generated signed upload URL: {signedUploadUrl}");

            await _uploadService.UpdatePublicUrlAsync(videoId, signedUploadUrl);
            return Ok(new
            {
                id = videoId,
                uploadUrl = signedUploadUrl,
                blobPath = blobName 
            });
        }

       




        [HttpPost("complete/{id}")]
        public async Task<IActionResult> MarkUploadComplete(Guid id)
        {
            Console.WriteLine($"Upload completed for vidvideo. with ID: ");
            var video = await _uploadService.CompleteUploadAsync(id);
            if (video == null) return NotFound();
            Console.WriteLine($"Upload completed for video: {video.OriginalFilename} with ID: {video.Id}");
            // await _elasticService.IndexVideoAsync(video); // Optional
            return Ok(new { publicUrl = video.PublicUrl });

        }


    }
}
