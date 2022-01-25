namespace SuggestionAppLibrary.DataAccess.Contracts;

public interface ISuggestionData
{
    Task<List<SuggestionModel>> GetSuggestionsAsync();
    Task<SuggestionModel> GetSuggestionsAsync(string id);
    Task<List<SuggestionModel>> GetApprovedSuggestionsAsync();
    Task<List<SuggestionModel>> GetSuggestionsWaitingForApprovalAsync();
    Task UpdateSuggestionAsync(SuggestionModel suggestion);
    Task UpvoteSuggestionAsync(string suggestionId, string userId);
    Task CreateSuggestionAsync(SuggestionModel suggestion);
}