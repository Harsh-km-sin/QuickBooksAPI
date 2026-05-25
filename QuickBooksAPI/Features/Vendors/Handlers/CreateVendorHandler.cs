using QuickBooksAPI.API.DTOs.Request;
using QuickBooksAPI.API.DTOs.Response;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Features.Shared;

namespace QuickBooksAPI.Features.Vendors.Handlers;

public sealed class CreateVendorHandler
{
    private readonly IRequestContext _requestContext;
    private readonly IVendorQboCommandService _commands;

    public CreateVendorHandler(IRequestContext requestContext, IVendorQboCommandService commands)
    {
        _requestContext = requestContext;
        _commands = commands;
    }

    public async Task<ApiResponse<string>> HandleAsync(CreateVendorRequest request)
    {
        if (!FeatureRequestContextGuard.TryGetUserRealm(_requestContext, out var userId, out var realmId, out var err))
            return ApiResponse<string>.Fail(err!);
        return await _commands.CreateAsync(userId, realmId, request);
    }
}
