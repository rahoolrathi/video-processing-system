namespace utube.Interfaces
{
    public interface ICloudStorageUploader
    {
        Task UploadFolderAsync(string localFolderPath, string remoteFolderName);
    }

}
