
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using utube.Enums;
namespace utube.Models
{
   

    public class Format
    {
        public Guid Id { get; set; }

        public Guid ProfileId { get; set; }
        public EncodingProfile EncodingProfile { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(24)")]
        public FormatType FormatType { get; set; }
    }

}
