namespace SuggestionAppLibrary.DataAccess;

using Microsoft.Extensions.Caching.Memory;

public class MongoCategoryData : ICategoryData
{
    private const string CacheName = "CategoryData";

    private readonly IMongoCollection<CategoryModel> _categories;
    private readonly IMemoryCache _cache;

    public MongoCategoryData(IDbConnection db, IMemoryCache cache)
    {
        _cache = cache;
        _categories = db.CategoryCollection;
    }

    public async Task<List<CategoryModel>> GetCategoriesAsync()
    {
        var output = _cache.Get<List<CategoryModel>>(CacheName);

        if (output is not null)
            return output;

        var results = await _categories.FindAsync(_ => true);
        output = results.ToList();

        _cache.Set(CacheName, output, TimeSpan.FromDays(1));

        return output;
    }

    public Task CreateCategory(CategoryModel category)
    {
        return _categories.InsertOneAsync(category);
    }
}
