namespace utube.DTOs
{
    public class WatermarkingJobDto
    {
        public Guid JobId { get; set; }
        public Guid VideoId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string VideoPath { get; set; } = string.Empty;
    }

}
