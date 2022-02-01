using Microsoft.Extensions.Caching.Memory;

namespace SuggestionAppLibrary.DataAccess;

public class MongoCategoryData : ICategoryData
{
    private readonly IMongoCollection<CategoryModel> _categories;
    private readonly IMemoryCache _cache;
    private const string cacheName = "CategoryData";

    public MongoCategoryData(IDbConnection db, IMemoryCache cache)
    {
        _cache = cache;
        _categories = db.CategoryCollection;
    }

    // Get all categories
    public async Task<List<CategoryModel>> GetAllCategories()
    {
        // obtain data from cache
        var output = _cache.Get<List<CategoryModel>>(cacheName);
        if (output is null)
        {
            var results = await _categories.FindAsync(_ => true);
            output = results.ToList();

            // putting value in cache (with a timespan of 1 day, data will be cached for a single day)
            _cache.Set(cacheName, output, TimeSpan.FromDays(1));
        }
        return output;
    }

    // Create a category
    public Task CreateCategory(CategoryModel category)
    {
        return _categories.InsertOneAsync(category);
    }
}
