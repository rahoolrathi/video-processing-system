namespace utube.DTOs
{
    public class EncodingProfileWithFormatsDto
    {
        public Guid Id { get; set; }
        public string ProfileName { get; set; }
        public string Resolutions { get; set; }        // e.g., "1920x1080"
        public string BitratesKbps { get; set; }       // e.g., "4500"

        public List<FormatDto> Formats { get; set; } = new();
    }
}
