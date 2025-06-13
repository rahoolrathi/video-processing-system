using System.Text.Json.Serialization;
using utube.Enums;

namespace utube.DTOs
{
    public class FormatDto
    {
        public Guid Id { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FormatType FormatType { get; set; }     // Enum: HLS, MPEG_DASH, etc.
             
    }
}
