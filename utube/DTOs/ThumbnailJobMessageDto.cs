namespace utube.DTOs
{
    public class ThumbnailJobMessageDto
    {
        public Guid JobId { get; set; }
        public Guid VideoId { get; set; }

        public string? VideoPath { get; set; }
    }

}
