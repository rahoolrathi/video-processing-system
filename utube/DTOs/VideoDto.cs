using utube.Enums;
namespace utube.DTOs
{


    public class VideoDto
    {
        public Guid Id { get; set; }

        public string OriginalFilename { get; set; }

        public VideoStatus Status { get; set; }

        public long FileSize { get; set; }

        public int TotalChunks { get; set; }

        public int ChunkSize { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        
    }
}
