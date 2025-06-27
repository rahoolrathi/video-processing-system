using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using System;

namespace utube.Services
{
    public class SignedUrlGeneratorService
    {
        private readonly string _storageAccountName;
        private readonly string _storageAccountKey;
        private readonly string _containerName;
        private readonly string _blobHostname;
        private readonly string _cdnHostname;

        public SignedUrlGeneratorService(IConfiguration configuration)
        {
            _containerName = configuration["AzureStorage:ContainerName"];
            _blobHostname = configuration["BlobStorage:Hostname"];
            _cdnHostname = configuration["CDN:Hostname"];
            _storageAccountName = configuration["AzureStorage2:AccName"];
            _storageAccountKey = configuration["AzureStorage2:AccountKey"];
        }

        public string GetSignedVideoUrl(string blobName)
        {
            // ✅ Sanitize if full URL passed in
            if (Uri.IsWellFormedUriString(blobName, UriKind.Absolute))
            {
                var uri = new Uri(blobName);
                var segments = uri.AbsolutePath.TrimStart('/').Split('/');
                if (segments.Length > 1 && segments[0] == _containerName)
                {
                    blobName = string.Join('/', segments.Skip(1));
                }
            }

            Console.WriteLine($"[SignedUrlGenerator] Generating signed URL for blob: {blobName}");

            var credential = new StorageSharedKeyCredential(_storageAccountName, _storageAccountKey);
            var blobUri = new Uri($"{_blobHostname}/{_containerName}/{blobName}");
            var blobClient = new BlobClient(blobUri, credential);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            sasBuilder.SetPermissions   (BlobSasPermissions.Read);
            string sasToken = sasBuilder.ToSasQueryParameters(credential).ToString();
            string signedUrl = $"{blobClient.Uri}?{sasToken}";

            return signedUrl.Replace(_blobHostname, _cdnHostname);
        }

        public string GenerateUploadSasUrl(string blobName)
        {
            Console.WriteLine($"[SignedUrlGenerator] Generating upload SAS URL for blob: {blobName}");
            var credential = new StorageSharedKeyCredential(_storageAccountName, _storageAccountKey);
            var blobUri = new Uri($"{_blobHostname}/{_containerName}/{blobName}");
            var blobClient = new BlobClient(blobUri, credential);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1) // longer for large uploads
            };

            // ✅ Permissions needed to upload chunks and commit them
            sasBuilder.SetPermissions(
     BlobSasPermissions.Read | BlobSasPermissions.Write | BlobSasPermissions.Create | BlobSasPermissions.Add
 );


            var sasToken = sasBuilder.ToSasQueryParameters(credential).ToString();
            string signedUrl = $"{blobClient.Uri}?{sasToken}";
            return signedUrl.Replace(_blobHostname, _cdnHostname);
        }

    }
}
