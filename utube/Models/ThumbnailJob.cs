using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using utube.Enums;

namespace utube.Models
{
    public class ThumbnailJob
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey("Video")]
        public Guid VideoId { get; set; }

        public Video Video { get; set; } = null!;

        public string FilePath { get; set; } = null!;

        public int NumberOfImages { get; set; }

        public string? SelectedImageName { get; set; }
        [Column(TypeName = "nvarchar(24)")]
        public JobStatus Status { get; set; } = JobStatus.Queued;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
