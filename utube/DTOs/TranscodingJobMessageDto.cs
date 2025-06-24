namespace utube.DTOs
{
    namespace utube.Dtos
    {
        public class TranscodingJobMessageDto
        {
            public Guid VideoId { get; set; }
            public string VideoPath { get; set; } = null!;
            public Guid EncodingProfileId { get; set; }
            public string EncryptionKey { get; set; } = null!;
            public string KeyId { get; set; } = null!;


        }
    }

}
