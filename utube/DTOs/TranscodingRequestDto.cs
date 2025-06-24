namespace utube.Dtos
{
    public class TranscodingRequestDto
    {
        public Guid VideoId { get; set; }
        public Guid EncodingProfileId { get; set; }
    }
}
