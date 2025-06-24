
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using utube.Enums;

   
    namespace utube.Models
    {
        public class Watermarking
        {
            [Key]
            public Guid Id { get; set; } = Guid.NewGuid();

            [ForeignKey("Video")]
            public Guid VideoId { get; set; }
            public Video Video { get; set; } = null!;
            public String text { get; set; } = string.Empty;

            public string WatermarkPath { get; set; } = string.Empty;

            public JobStatus Status { get; set; } = JobStatus.Queued;

            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            public DateTime? UpdatedAt { get; set; }
        }
    }


