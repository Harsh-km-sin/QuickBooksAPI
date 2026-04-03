using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.DataAccessLayer.Models;

namespace QuickBooksAPI.Application.Interfaces;

public interface IQboConnectionService
{
    Task<string> GenerateOAuthUrlAsync(int userId);
    Task<ApiResponse<QuickBooksToken>> HandleCallbackAsync(string code, string state, string realmId);
    Task<ApiResponse<string>> DisconnectQboAsync(int userId, string realmId);
}
