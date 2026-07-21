using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using QuickBooksAPI.Application.Interfaces;
using QuickBooksShared.Options;

namespace QuickBooksAPI.Services.CfoAssistant;

/// <summary>
/// Answers CFO questions using warehouse-backed metrics; optionally uses Azure OpenAI to summarize.
/// Intent-specific logic lives in <see cref="ICfoAssistantIntentHandler"/> implementations (one file per handler).
/// </summary>
public sealed class CfoAssistantService : ICfoAssistantService
{
    /// <summary>Name for <see cref="IHttpClientFactory"/> registration (Azure OpenAI chat completions).</summary>
    public const string AzureOpenAiHttpClientName = "AzureOpenAI";

    private readonly IEnumerable<ICfoAssistantIntentHandler> _handlers;
    private readonly AzureOpenAiOptions _azureOpenAiOptions;
    private readonly IHttpClientFactory _httpClientFactory;

    public CfoAssistantService(
        IEnumerable<ICfoAssistantIntentHandler> handlers,
        IOptions<AzureOpenAiOptions> azureOpenAiOptions,
        IHttpClientFactory httpClientFactory)
    {
        _handlers = handlers?.ToList() ?? throw new ArgumentNullException(nameof(handlers));
        _azureOpenAiOptions = azureOpenAiOptions?.Value ?? throw new ArgumentNullException(nameof(azureOpenAiOptions));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task<CfoAssistantResponse> AskAsync(int userId, string realmId, string question, CancellationToken cancellationToken = default)
    {
        var q = question.ToLowerInvariant().Trim();
        var ctx = new CfoAssistantContext();

        foreach (var handler in _handlers)
        {
            if (handler.Matches(q))
                await handler.AppendAsync(userId, realmId, ctx, cancellationToken);
        }

        if (ctx.Narrative.Length == 0)
        {
            return new CfoAssistantResponse
            {
                Answer = "I can answer questions about cash runway, revenue vs expenses, top vendors by spend, and customer profitability. Try asking e.g. 'How many months of runway do we have?' or 'Top unprofitable customers?'",
                Citations = new List<CitationDto>()
            };
        }

        var endpoint = _azureOpenAiOptions.Endpoint;
        var apiKey = _azureOpenAiOptions.ApiKey;
        var deployment = string.IsNullOrWhiteSpace(_azureOpenAiOptions.DeploymentName)
            ? "gpt-35-turbo"
            : _azureOpenAiOptions.DeploymentName;

        if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var answer = await CallAzureOpenAIAsync(endpoint, apiKey, deployment, ctx.Narrative.ToString(), question, cancellationToken).ConfigureAwait(false);
                return new CfoAssistantResponse { Answer = answer, Citations = ctx.Citations };
            }
            catch (Exception)
            {
                return new CfoAssistantResponse
                {
                    Answer = "Summary from your data:\n\n" + ctx.Narrative.ToString().Replace("\n", "\n• ").TrimStart('•', ' '),
                    Citations = ctx.Citations
                };
            }
        }

        return new CfoAssistantResponse
        {
            Answer = "Based on your data:\n\n" + ctx.Narrative.ToString().Replace("\n", "\n• ").TrimStart('•', ' '),
            Citations = ctx.Citations
        };
    }

    private async Task<string> CallAzureOpenAIAsync(string endpoint, string apiKey, string deployment, string context, string question, CancellationToken cancellationToken)
    {
        var url = $"{endpoint.TrimEnd('/')}/openai/deployments/{deployment}/chat/completions?api-version=2024-02-15-preview";
        var client = _httpClientFactory.CreateClient(AzureOpenAiHttpClientName);

        var body = new
        {
            messages = new[]
            {
                new { role = "system", content = "You are a CFO assistant. Answer ONLY using the following data. Do not invent any numbers. Be concise (2-4 sentences)." },
                new { role = "user", content = $"Data:\n{context}\n\nQuestion: {question}" }
            },
            max_tokens = 300,
            temperature = 0.2
        };
        var json = JsonSerializer.Serialize(body);
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("api-key", apiKey);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(responseJson);
        var choices = doc.RootElement.GetProperty("choices");
        if (choices.GetArrayLength() == 0)
            return "I couldn't generate a response.";
        var message = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "No content.";
        return message.Trim();
    }
}
