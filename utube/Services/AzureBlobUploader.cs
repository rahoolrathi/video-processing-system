using Azure.Storage.Blobs;

namespace utube.Services
{
    public class AzureBlobUploader
    {
        private readonly BlobContainerClient _containerClient;

        public AzureBlobUploader(IConfiguration config)
        {
            var connString = config["AzureStorage:ConnectionString"];
            var containerName = config["AzureStorage:ContainerName"];

            if (string.IsNullOrEmpty(connString))
                throw new ArgumentNullException(nameof(connString), "Azure storage connection string is missing.");

            if (string.IsNullOrEmpty(containerName))
                throw new ArgumentNullException(nameof(containerName), "Azure blob container name is missing.");

            _containerClient = new BlobContainerClient(connString, containerName);
            _containerClient.CreateIfNotExists();
        }

        public async Task UploadFolderAsync(string localFolderPath, string remoteFolderName)
        {
            if (!Directory.Exists(localFolderPath))
                throw new DirectoryNotFoundException($"Directory not found: {localFolderPath}");

            var files = Directory.GetFiles(localFolderPath, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(localFolderPath, file);
                var blobPath = Path.Combine(remoteFolderName, relativePath).Replace("\\", "/");

                BlobClient blob = _containerClient.GetBlobClient(blobPath);

                await using var fileStream = File.OpenRead(file);
                await blob.UploadAsync(fileStream, overwrite: true);

                Console.WriteLine($"[Azure] Uploaded: {blobPath}");
            }
        }
    }
}
