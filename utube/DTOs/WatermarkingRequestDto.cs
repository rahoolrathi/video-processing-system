namespace utube.DTOs
{
    public class WatermarkingRequestDto
    {
        public required Guid VideoId { get; set; }
        public required string text { get; set; }
    }
}
