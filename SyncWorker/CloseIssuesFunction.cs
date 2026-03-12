using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuickBooksAPI.DataAccessLayer.Models;
using QuickBooksAPI.DataAccessLayer.Repos;

namespace SyncWorker
{
    public class CloseIssuesFunction
    {
        private const int OverdueDaysThreshold = 30;

        private readonly ILogger<CloseIssuesFunction> _logger;
        private readonly IServiceProvider _serviceProvider;

        public CloseIssuesFunction(ILogger<CloseIssuesFunction> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [Function(nameof(CloseIssuesFunction))]
        public async Task Run(
            [TimerTrigger("0 0 3 * * *", RunOnStartup = false)] TimerInfo timerInfo)
        {
            _logger.LogInformation("Close issues detection triggered at {Time}", DateTime.UtcNow);

            using var scope = _serviceProvider.CreateScope();
            var companyRepo = scope.ServiceProvider.GetRequiredService<ICompanyRepository>();
            var invoiceRepo = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
            var closeIssueRepo = scope.ServiceProvider.GetRequiredService<ICloseIssueRepository>();

            var companies = await companyRepo.GetDistinctConnectedUserRealmAsync();
            var asOfDate = DateTime.UtcNow.Date.AddDays(-OverdueDaysThreshold);

            foreach (var (userId, realmId) in companies)
            {
                try
                {
                    var overdueCount = await invoiceRepo.GetOverdueReceivablesCountAsync(realmId, asOfDate);
                    if (overdueCount > 0)
                    {
                        await closeIssueRepo.InsertAsync(new CloseIssue
                        {
                            UserId = userId,
                            RealmId = realmId,
                            IssueType = "InvoiceWithoutPayment",
                            Severity = overdueCount > 10 ? "High" : "Medium",
                            Details = $"{overdueCount} invoice(s) with balance are over {OverdueDaysThreshold} days past due.",
                            DetectedAt = DateTime.UtcNow
                        }, default);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Close issues check failed for UserId={UserId}, RealmId={RealmId}", userId, realmId);
                }
            }
        }
    }
}
