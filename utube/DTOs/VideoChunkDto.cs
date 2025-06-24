using System.ComponentModel.DataAnnotations;
namespace utube.DTOs
{


    public class VideoChunkDto
    {
        [Required]
        public Guid VideoId { get; set; }

        [Required]
        public int ChunkIndex { get; set; }

        [Required]
        public bool IsLastChunk { get; set; }

        [Required]
        public IFormFile Chunk { get; set; }
    }
}

