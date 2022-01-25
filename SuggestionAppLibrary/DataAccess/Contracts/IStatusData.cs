namespace SuggestionAppLibrary.DataAccess.Contracts;

public interface IStatusData
{
    Task<List<StatusModel>> GetStatusesAsync();
    Task CreateStatus(StatusModel status);
}