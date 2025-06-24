using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using utube.Enums;
namespace utube.Models
{
    

    public class VideoChunk
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(Video))]
        public Guid VideoId { get; set; }

        public Video Video { get; set; }

        [Required]
        public int ChunkIndex { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(24)")]
        public ChunkStatus Status { get; set; }
        [Required]



        public bool IsLastChunk { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
