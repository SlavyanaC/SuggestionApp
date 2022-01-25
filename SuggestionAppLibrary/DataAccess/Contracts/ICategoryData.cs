namespace SuggestionAppLibrary.DataAccess.Contracts;

public interface ICategoryData
{
    Task<List<CategoryModel>> GetCategoriesAsync();
    Task CreateCategory(CategoryModel category);
}