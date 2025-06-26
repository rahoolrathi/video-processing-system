using Microsoft.AspNetCore.Mvc;
using utube.Enums;
using utube.Models;
using utube.Services;

namespace utube.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ElasticSearchService _searchService;
        private readonly SignedUrlGeneratorService _signedUrlService;

        public SearchController(ElasticSearchService searchService, SignedUrlGeneratorService signedUrlService)
        {
            _searchService = searchService;
            _signedUrlService = signedUrlService;
        }

        [HttpPost("index")]
        public async Task<IActionResult> Index(VideoDocument doc)
        {
            await _searchService.IndexDocumentAsync(doc);
            return Ok("Indexed");
        }


        //[HttpGet("search")]
        //public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] FormatType format)
        //{
        //    var results = await _searchService.SearchAsync(q, format);

        //    foreach (var doc in results)
        //    {
        //        Console.WriteLine(doc.videopath);

        //        // Generate signed video URL
        //        var basePath = doc.videopath.TrimEnd('/');
        //        string formatPath = format switch
        //        {
        //            FormatType.HLS => "hls/master.m3u8",
        //            FormatType.DASH => "dash/manifest.mpd",
        //            _ => throw new ArgumentOutOfRangeException(nameof(format), "Unsupported format")
        //        };
        //        var blobPath = $"{basePath}/{formatPath}";
        //        doc.videopath = _signedUrlService.GetSignedVideoUrl(blobPath);
        //        Console.WriteLine($"doc.videopath: {doc.videopath}");
        //        // ✅ Generate signed thumbnail URL if selected image exists
        //        if (!string.IsNullOrWhiteSpace(doc.SelectedImageName))
        //        {
        //            var fileName = Path.GetFileName(doc.SelectedImageName); // Strip full URL if any
        //            string thumbBlobPath = $"thumbnails/{doc.Id}/{fileName}";
        //            doc.SelectedImageName = _signedUrlService.GetSignedVideoUrl(thumbBlobPath);
        //        }
        //        Console.WriteLine($" doc.SelectedImageName: {doc.SelectedImageName}");

        //    }

        //    return Ok(results);
        //}



        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] FormatType? format = null)
        {
            var results = await _searchService.SearchAsync(q, format);

            foreach (var doc in results)
            {
                // If video is unprocessed (no processed videopath), return raw defaultPath
                if (string.IsNullOrWhiteSpace(doc.videopath) && !string.IsNullOrWhiteSpace(doc.defaultpath))
                {
                    doc.defaultpath = _signedUrlService.GetSignedVideoUrl(doc.defaultpath); ;
                }
                else if (!string.IsNullOrWhiteSpace(doc.videopath) && format != null)
                {
                    var basePath = doc.videopath.TrimEnd('/');
                    string formatPath = format switch
                    {
                        FormatType.HLS => "hls/master.m3u8",
                        FormatType.DASH => "dash/manifest.mpd",
                        _ => throw new ArgumentOutOfRangeException(nameof(format), "Unsupported format")
                    };
                    var blobPath = $"{basePath}/{formatPath}";
                    doc.videopath = _signedUrlService.GetSignedVideoUrl(blobPath);
                    doc.defaultpath = null;
                }

                // ✅ Signed thumbnail
                if (!string.IsNullOrWhiteSpace(doc.SelectedImageName))
                {
                    var fileName = Path.GetFileName(doc.SelectedImageName);
                    string thumbBlobPath = $"thumbnails/{doc.Id}/{fileName}";
                    doc.SelectedImageName = _signedUrlService.GetSignedVideoUrl(thumbBlobPath);
                }
            }

            return Ok(results);
        }


    }

}
