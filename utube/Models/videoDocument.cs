using utube.Enums;

namespace utube.Models
{
    public class VideoDocument
    {
        public Guid Id { get; set; }
        public string name { get; set; }
        public string? videopath { get; set; }
        public DateTime UploadedAt { get; set; }
        public string? SelectedImageName { get; set; } // <-- Add this
        public List<FormatType> Formats { get; set; } = new();

    }

}
