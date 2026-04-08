using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Configuration;
using Avachat.Infra.Interfaces.AppServices;

namespace Avachat.Infra.AppServices;

public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly string _indexName;
    private bool _indexCreated;

    public ElasticsearchService(IConfiguration configuration)
    {
        var url = configuration["Elasticsearch:Url"] ?? "http://localhost:9200";
        _indexName = configuration["Elasticsearch:IndexName"] ?? "knowledge_chunks";
        var settings = new ElasticsearchClientSettings(new Uri(url));
        _client = new ElasticsearchClient(settings);
    }

    public async Task CreateIndexAsync()
    {
        if (_indexCreated) return;

        for (int attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                var exists = await _client.Indices.ExistsAsync(_indexName);
                if (exists.Exists)
                {
                    _indexCreated = true;
                    return;
                }

                var properties = new Properties
                {
                    ["chunk_id"] = new KeywordProperty(),
                    ["agent_id"] = new KeywordProperty(),
                    ["knowledge_file_id"] = new KeywordProperty(),
                    ["content"] = new TextProperty { Analyzer = "standard" },
                    ["embedding"] = new DenseVectorProperty
                    {
                        Dims = 1536,
                        Index = true,
                        Similarity = DenseVectorSimilarity.Cosine
                    },
                    ["chunk_index"] = new IntegerNumberProperty()
                };

                var createResponse = await _client.Indices.CreateAsync(_indexName, c => c
                    .Mappings(m => m.Properties(properties))
                );

                if (createResponse.IsValidResponse)
                {
                    _indexCreated = true;
                    Console.WriteLine($"[Elasticsearch] Index '{_indexName}' created successfully.");
                    return;
                }

                Console.WriteLine($"[Elasticsearch] Attempt {attempt}/10 - Failed to create index: {createResponse.DebugInformation}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Elasticsearch] Attempt {attempt}/10 - Connection error: {ex.Message}");
            }

            if (attempt < 10)
                await Task.Delay(3000);
        }

        Console.WriteLine($"[Elasticsearch] WARNING: Could not create index '{_indexName}' after 10 attempts.");
    }

    private async Task EnsureIndexAsync()
    {
        if (!_indexCreated)
            await CreateIndexAsync();
    }

    public async Task IndexChunksAsync(long agentId, long knowledgeFileId, List<ChunkData> chunks)
    {
        await EnsureIndexAsync();

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var doc = new Dictionary<string, object>
            {
                ["chunk_id"] = $"{knowledgeFileId}_{chunk.ChunkIndex}",
                ["agent_id"] = agentId.ToString(),
                ["knowledge_file_id"] = knowledgeFileId.ToString(),
                ["content"] = chunk.Content,
                ["embedding"] = chunk.Embedding,
                ["chunk_index"] = chunk.ChunkIndex
            };

            await _client.IndexAsync(doc, idx => idx
                .Index(_indexName)
                .Id($"{knowledgeFileId}_{chunk.ChunkIndex}")
            );
        }

        await _client.Indices.RefreshAsync(_indexName);
    }

    public async Task DeleteChunksByFileIdAsync(long knowledgeFileId)
    {
        await EnsureIndexAsync();

        await _client.DeleteByQueryAsync<Dictionary<string, object>>(_indexName, d => d
            .Query(q => q
                .Term(new TermQuery("knowledge_file_id")
                {
                    Value = knowledgeFileId.ToString()
                })
            )
        );
    }

    public async Task DeleteChunksByAgentIdAsync(long agentId)
    {
        await EnsureIndexAsync();

        await _client.DeleteByQueryAsync<Dictionary<string, object>>(_indexName, d => d
            .Query(q => q
                .Term(new TermQuery("agent_id")
                {
                    Value = agentId.ToString()
                })
            )
        );
    }

    public async Task<List<string>> HybridSearchAsync(long agentId, float[] queryVector, string queryText, int topK = 5)
    {
        await EnsureIndexAsync();

        var response = await _client.SearchAsync<Dictionary<string, object>>(s => s
            .Index(_indexName)
            .Size(topK)
            .Query(q => q
                .Bool(b => b
                    .Filter(new Query[]
                    {
                        new TermQuery("agent_id") { Value = agentId.ToString() }
                    })
                    .Should(new Query[]
                    {
                        new MatchQuery("content") { Query = queryText }
                    })
                )
            )
            .Knn(k => k
                .Field("embedding")
                .QueryVector(queryVector)
                .k(topK)
                .NumCandidates(topK * 10)
                .Filter(new Query[]
                {
                    new TermQuery("agent_id") { Value = agentId.ToString() }
                })
            )
        );

        var results = new List<string>();
        if (response.IsValidResponse && response.Documents != null)
        {
            foreach (var doc in response.Documents)
            {
                if (doc.TryGetValue("content", out var content))
                {
                    results.Add(content?.ToString() ?? string.Empty);
                }
            }
        }

        return results;
    }
}
