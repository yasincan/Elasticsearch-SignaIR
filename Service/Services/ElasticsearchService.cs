using Elasticsearch.Net;
using Nest;

namespace Service.Services;

public class ElasticsearchService
{
    private readonly IElasticClient _client;
    private string _index;

    public ElasticsearchService(IElasticClient client, string index)
    {
        _client = client;
        _index = index;
    }

    public ElasticsearchService Index(string index)
    {
        _index = index;
        return this;
    }

    public async Task CreateIndexIfNotExistsAsync<T>(string indexName) where T : class
    {
        var existResponse = await _client.Indices.ExistsAsync(indexName);
        if (!existResponse.Exists)
        {
            var createIndexResponse = await _client.Indices.CreateAsync(indexName, c => c
                .Map<T>(m => m.AutoMap())
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(1)
                )
            );
        }
        Index(indexName);
    }

    public async Task<bool> AddOrUpdateAsync<T>(T document) where T : class
    {
        var indexResponse = await _client.IndexAsync(document, idx => idx.Index(_index).OpType(OpType.Index));
        return indexResponse.IsValid;
    }

    public async Task<T?> GetLastAddedAsync<T>() where T : class
    {
        var searchResponse = await _client.SearchAsync<T>(s => s
            .Index(_index)
            .Sort(sort => sort.Descending("id"))
            .Take(1));

        if (searchResponse.IsValid && searchResponse.Documents.Any())
        {
            return searchResponse.Documents.First();
        }
        return null;
    }

    public async Task<bool> AddOrUpdateBulkAsync<T>(IEnumerable<T> documents) where T : class
    {
        var indexResponse = await _client.BulkAsync(b => b
               .Index(_index)
               .UpdateMany(documents, (ud, d) => ud.Doc(d).DocAsUpsert(true))
           );
        return indexResponse.IsValid;
    }

    public async Task<T> GetAsync<T>(string key) where T : class
    {
        var response = await _client.GetAsync<T>(key, g => g.Index(_index));
        return response.Source;
    }

    public async Task<List<T>?> GetAllAsync<T>() where T : class
    {
        var searchResponse = await _client.SearchAsync<T>(s => s.Index(_index).Query(q => q.MatchAll()));
        return searchResponse.IsValid ? searchResponse.Documents.ToList() : default;
    }

    public async Task<List<T>?> QueryAsync<T>(QueryContainer predicate) where T : class
    {
        var searchResponse = await _client.SearchAsync<T>(s => s.Index(_index).Query(q => predicate));
        return searchResponse.IsValid ? searchResponse.Documents.ToList() : default;
    }

    public async Task<bool> RemoveAsync<T>(string key) where T : class
    {
        var response = await _client.DeleteAsync<T>(key, d => d.Index(_index));
        return response.IsValid;
    }

    public async Task<long> RemoveAllAsync<T>() where T : class
    {
        var response = await _client.DeleteByQueryAsync<T>(d => d.Index(_index).Query(q => q.MatchAll()));
        return response.Deleted;
    }
}