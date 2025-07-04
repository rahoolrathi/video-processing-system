﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using utube.Enums;
namespace utube.Models
{
   

    public class Video
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public required string OriginalFilename { get; set; }

        [Column(TypeName = "nvarchar(24)")]
        public VideoStatus Status { get; set; } = VideoStatus.Processing;

        public long FileSize { get; set; }

        public int TotalChunks { get; set; }

        public int ChunkSize { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

      

        // 🔐 Encryption fields
        [MaxLength(64)]
        public string? EncryptionKey { get; set; }

        [MaxLength(64)]
        public string? KeyId { get; set; }

        // 🌐 Public (Signed) URL
        [MaxLength(2048)]
        public string? PublicUrl { get; set; }
    }
}
