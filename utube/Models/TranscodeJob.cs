using System.ComponentModel.DataAnnotations.Schema;
using utube.Enums;

namespace utube.Models
{
    public class TranscodeJob
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid VideoId { get; set; }
        public Video Video { get; set; } = null!;

        public Guid ProfileId { get; set; }
        public EncodingProfile Profile { get; set; } = null!;

        public string? WorkerId { get; set; }

        [Column(TypeName = "nvarchar(24)")]
        public JobStatus Status { get; set; } = JobStatus.Queued;

        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? TranscodedPath { get; set; }
    }

}
