namespace utube.Interfaces
{
    public interface ISignedUrlGenerator
    {
        string GetSignedVideoUrl(string blobName);
        string GenerateUploadSasUrl(string blobName);
    }

}
