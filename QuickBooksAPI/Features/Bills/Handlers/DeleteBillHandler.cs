using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Shared;

namespace QuickBooksAPI.Features.Bills.Handlers;

public sealed class DeleteBillHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IBillQboCommandService _commands;

    public DeleteBillHandler(IRequestContext requestContext, IBillQboCommandService commands)
    {
        _requestContext = requestContext;
        _commands = commands;
    }

    public async Task<ApiResponse<string>> HandleAsync(DeleteBillRequest request)
    {
        if (!FeatureRequestContextGuard.TryGetUserRealm(_requestContext, out var userId, out var realmId, out var err))
            return ApiResponse<string>.Fail(err!);
        return await _commands.DeleteAsync(userId, realmId, request);
    }
}
