namespace SuggestionAppLibrary.DataAccess;

using Microsoft.Extensions.Caching.Memory;

public class MongoStatusData : IStatusData
{
    private const string CacheName = "StatusData";

    private readonly IMemoryCache _cache;
    private readonly IMongoCollection<StatusModel> _statuses;

    public MongoStatusData(IDbConnection db, IMemoryCache cache)
    {
        _cache = cache;
        _statuses = db.StatusCollection;
    }

    public async Task<List<StatusModel>> GetStatusesAsync()
    {
        var output = _cache.Get<List<StatusModel>>(CacheName);

        if (output is not null)
            return output;

        var results = await _statuses.FindAsync(_ => true);
        output = results.ToList();

        _cache.Set(CacheName, output, TimeSpan.FromDays(1));

        return output;
    }

    public Task CreateStatus(StatusModel status)
    {
        return _statuses.InsertOneAsync(status);
    }
}
