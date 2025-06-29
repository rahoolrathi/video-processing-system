﻿using Nest;
using utube.Enums;
using utube.Models;

namespace utube.Services
{
    public class ElasticSearchService
    {
        private readonly IElasticClient _elasticClient;

        public ElasticSearchService(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        public async Task IndexDocumentAsync(VideoDocument doc)
        {
            var response = await _elasticClient.IndexDocumentAsync(doc);
            if (!response.IsValid)
            {
                throw new Exception("Failed to index document: " + response.OriginalException.Message);
            }
        }

        public async Task<List<VideoDocument>> SearchAsync(string query, FormatType? format = null)
        {
            var cleanQuery = Path.GetFileNameWithoutExtension(query.Trim());

            var response = await _elasticClient.SearchAsync<VideoDocument>(s => s
                .Query(q =>
                {
                    var mustQueries = new List<QueryContainer>();

                    // Always include name query
                    mustQueries.Add(q.Wildcard(w => w
                        .Field(f => f.name.Suffix("keyword"))
                        .Value($"*{cleanQuery}*")
                    ));

                    // Optionally add format filter
                    if (format.HasValue)
                    {
                        mustQueries.Add(q.Term(t => t
                            .Field(f => f.Formats)
                            .Value(format.Value)
                        ));
                    }

                    return q.Bool(b => b.Must(mustQueries.ToArray()));
                })
            );

            Console.WriteLine($"[Elasticsearch] Search query: {cleanQuery}, Format: {format}, Found: {response.Documents.Count} documents.");
            return response.Documents.ToList();
        }






        public async Task UpdateSelectedThumbnailAsync(Guid videoId, string remotePath, List<FormatType> formats)
        {
            const string IndexName = "videos";

            var response = await _elasticClient.UpdateAsync<VideoDocument, object>(
                videoId,
                u => u
                    .Index(IndexName)
                    .Doc(new
                    {
                        videopath = remotePath,
                        Formats = formats
                    })
            );

            if (!response.IsValid)
            {
                throw new Exception($"[Elasticsearch] Update failed: {response.ServerError?.Error?.Reason}");
            }
        }


        public async Task UpdateSelectedImageNameAsync(Guid videoId, string selectedImageName)
        {
            const string IndexName = "videos";

            var response = await _elasticClient.UpdateAsync<VideoDocument, object>(
                videoId,
                u => u
                    .Index(IndexName)
                    .Doc(new
                    {
                        SelectedImageName = selectedImageName
                    })
            );

            if (!response.IsValid)
            {
                throw new Exception($"[Elasticsearch] Failed to update SelectedImageName: {response.ServerError?.Error?.Reason}");
            }
        }

        public async Task UpdateDefaultPathAsync(Guid videoId, string defaultPath)
{
    const string IndexName = "videos";

    var response = await _elasticClient.UpdateAsync<VideoDocument, object>(
        videoId,
        u => u
            .Index(IndexName)
            .Doc(new
            {
                defaultpath = defaultPath
            })
    );

    if (!response.IsValid)
    {
        throw new Exception($"[Elasticsearch] Failed to update defaultpath: {response.ServerError?.Error?.Reason}");
    }
}




    }

}
