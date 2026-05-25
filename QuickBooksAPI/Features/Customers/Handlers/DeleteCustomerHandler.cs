using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Shared;

namespace QuickBooksAPI.Features.Customers.Handlers;

public sealed class DeleteCustomerHandler
{
    private readonly IRequestContext _requestContext;
    private readonly ICustomerQboCommandService _commands;

    public DeleteCustomerHandler(IRequestContext requestContext, ICustomerQboCommandService commands)
    {
        _requestContext = requestContext;
        _commands = commands;
    }

    public async Task<ApiResponse<string>> HandleAsync(DeleteCustomerRequest request)
    {
        if (!FeatureRequestContextGuard.TryGetUserRealm(_requestContext, out var userId, out var realmId, out var err))
            return ApiResponse<string>.Fail(err!);
        return await _commands.DeleteAsync(userId, realmId, request);
    }
}
