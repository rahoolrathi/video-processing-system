using Microsoft.SqlServer.Server;
using System.ComponentModel.DataAnnotations;

namespace utube.Models
{
    public class EncodingProfile
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string ProfileName { get; set; }

        [Required]
        public string Resolutions { get; set; }
        [Required]
        public string BitratesKbps { get; set; }
        public ICollection<Format> Formats { get; set; }
    }
}
