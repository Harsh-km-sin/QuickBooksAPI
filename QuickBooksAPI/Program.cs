using QuickBooksAPI.Application.Interfaces;
using QuickBooksAPI.Infrastructure;
using QuickBooksAPI.Infrastructure.Identity;
using QuickBooksAPI.Services;
using QuickBooksShared;
using QuickBooksShared.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<RequestContext>();
builder.Services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<RequestContext>());

// Composition: application + infrastructure registrations are extension-based (see Infrastructure/).
builder.Services.AddQuickBooksAuthAndEntityApplicationServices();
builder.Services.AddQuickBooksAnalyticsApplicationServices();

builder.Services.AddQuickBooksTypedOptions(builder.Configuration, validateOnStart: true);

builder.Services.AddQuickBooksServiceBusAndSync(builder.Configuration);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddGlReview(builder.Configuration);

builder.AddQuickBooksApiHostServices();

var app = builder.Build();

app.UseQuickBooksApiPipeline();

app.Run();
