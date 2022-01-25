namespace SuggestionAppLibrary.DataAccess;

using Microsoft.Extensions.Caching.Memory;

public class MongoSuggestionData : ISuggestionData
{
    private const string CacheName = "SuggestionData";

    private readonly IDbConnection _db;
    private readonly IUserData _userData;
    private readonly IMemoryCache _cache;
    private readonly IMongoCollection<SuggestionModel> _suggestions;

    public MongoSuggestionData(IDbConnection db, IUserData userData, IMemoryCache cache)
    {
        _db = db;
        _userData = userData;
        _cache = cache;
        _suggestions = db.SuggestionCollection;
    }

    public async Task<List<SuggestionModel>> GetSuggestionsAsync()
    {
        var output = _cache.Get<List<SuggestionModel>>(CacheName);

        if (output is not null)
            return output;

        var results = await _suggestions.FindAsync(s => s.Archived == false);
        output = results.ToList();

        _cache.Set(CacheName, output, TimeSpan.FromMinutes(1));

        return output;
    }

    public async Task<List<SuggestionModel>> GetApprovedSuggestionsAsync()
    {
        var output = await GetSuggestionsAsync();
        return output.Where(x => x.ApprovedForRelease).ToList();
    }

    public async Task<List<SuggestionModel>> GetSuggestionsWaitingForApprovalAsync()
    {
        var output = await GetSuggestionsAsync();
        return output.Where(x => x.ApprovedForRelease == false && x.Rejected == false).ToList();
    }

    public async Task<SuggestionModel> GetSuggestionsAsync(string id)
    {
        var results = await _suggestions.FindAsync(s => s.Id == id);
        return results.FirstOrDefault();
    }

    public async Task UpdateSuggestionAsync(SuggestionModel suggestion)
    {
        await _suggestions.ReplaceOneAsync(s => s.Id == suggestion.Id, suggestion);
        _cache.Remove(CacheName);
    }

    public async Task UpvoteSuggestionAsync(string suggestionId, string userId)
    {
        var client = _db.Client;

        using var session = await client.StartSessionAsync();
        session.StartTransaction();

        try
        {
            var db = client.GetDatabase(_db.DbName);
            var suggestionsInTransaction = db.GetCollection<SuggestionModel>(_db.SuggestionCollectionName);
            var suggestion = (await suggestionsInTransaction.FindAsync(s => s.Id == suggestionId)).First();

            var isUpvote = suggestion.UserVotes.Add(userId);
            if (!isUpvote)
                suggestion.UserVotes.Remove(userId);

            await suggestionsInTransaction.ReplaceOneAsync(s => s.Id == suggestionId, suggestion);

            var usersInTransaction = db.GetCollection<UserModel>(_db.UserCollectionName);
            var user = await _userData.GetUserByIdAsync(suggestion.Author.Id);

            if (isUpvote)
                user.VotedOnSuggestions.Add(new BasicSuggestionModel(suggestion));
            else
            {
                var suggestionToRemove = user.VotedOnSuggestions.First(s => s.Id == suggestionId);
                user.VotedOnSuggestions.Remove(suggestionToRemove);
            }

            await usersInTransaction.ReplaceOneAsync(u => u.Id == userId, user);

            await session.CommitTransactionAsync();

            _cache.Remove(CacheName);
        }
        catch (Exception)
        {
            await session.AbortTransactionAsync();
            throw;
        }
    }

    public async Task CreateSuggestionAsync(SuggestionModel suggestion)
    {
        var client = _db.Client;

        using var session = await client.StartSessionAsync();
        session.StartTransaction();

        try
        {
            var db = client.GetDatabase(_db.DbName);
            var suggestionsInTransaction = db.GetCollection<SuggestionModel>(_db.SuggestionCollectionName);
            await suggestionsInTransaction.InsertOneAsync(suggestion);

            var usersInTransaction = db.GetCollection<UserModel>(_db.UserCollectionName);
            var user = await _userData.GetUserByIdAsync(suggestion.Author.Id);
            user.AuthoredSuggestions.Add(new BasicSuggestionModel(suggestion));
            await usersInTransaction.ReplaceOneAsync(u => u.Id == user.Id, user);

            await session.CommitTransactionAsync();
        }
        catch (Exception)
        {
            await session.AbortTransactionAsync();
            throw;
        }
    }
}
