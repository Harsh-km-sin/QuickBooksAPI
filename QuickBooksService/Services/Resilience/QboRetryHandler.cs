using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace QuickBooksService.Services.Resilience
{
    /// <summary>
    /// Generic <see cref="DelegatingHandler"/> that retries requests QBO throttled with
    /// 429 Too Many Requests, honoring the <c>Retry-After</c> header when QBO sends one and
    /// falling back to exponential backoff with jitter otherwise.
    ///
    /// Not tied to any specific QBO entity or endpoint — attach it to any named
    /// <see cref="IHttpClientFactory"/> client via <c>AddHttpMessageHandler&lt;QboRetryHandler&gt;()</c>
    /// to opt that client into retry behavior. Currently only wired into the Reports client, since
    /// QBO's Reports endpoint has a tighter rate limit (200 req/min) than the rest of the API, but the
    /// handler itself makes no assumption about that.
    /// </summary>
    public class QboRetryHandler : DelegatingHandler
    {
        private readonly ILogger<QboRetryHandler> _logger;
        private readonly int _maxRetries;
        private readonly TimeSpan _baseDelay;

        public QboRetryHandler(ILogger<QboRetryHandler> logger, int maxRetries = 3, TimeSpan? baseDelay = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxRetries = maxRetries;
            _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            for (var attempt = 0; ; attempt++)
            {
                var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt >= _maxRetries)
                {
                    return response;
                }

                var delay = GetRetryDelay(response, attempt);
                _logger.LogWarning(
                    "QBO request throttled (429 ThrottleExceeded). Retrying in {DelaySeconds:F1}s (attempt {Attempt}/{MaxRetries}). Url={Url}",
                    delay.TotalSeconds, attempt + 1, _maxRetries, request.RequestUri);

                response.Dispose();
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        private TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
        {
            var retryAfter = response.Headers.RetryAfter;
            if (retryAfter is not null)
            {
                if (retryAfter.Delta.HasValue)
                {
                    return retryAfter.Delta.Value;
                }

                if (retryAfter.Date.HasValue)
                {
                    var delta = retryAfter.Date.Value - DateTimeOffset.UtcNow;
                    if (delta > TimeSpan.Zero)
                    {
                        return delta;
                    }
                }
            }

            // QBO doesn't always send Retry-After — fall back to exponential backoff with jitter.
            var exponential = TimeSpan.FromMilliseconds(_baseDelay.TotalMilliseconds * Math.Pow(2, attempt));
            var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250));
            return exponential + jitter;
        }
    }
}
