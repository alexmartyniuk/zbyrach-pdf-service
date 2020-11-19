using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Zbyrach.Pdf
{
    public class PdfCacheCleaner : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<PdfCacheCleaner> _logger;
        public PdfCacheCleaner(IServiceScopeFactory serviceScopeFactory,
            ILogger<PdfCacheCleaner> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;            
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RemoveOldArticles(stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unexpected error during removing old articles.");
                }
                await Task.Delay(TimeSpan.FromSeconds(60 * 60), stoppingToken);
            }
        }

        private async Task RemoveOldArticles(CancellationToken stoppingToken)
        {
            using var serviceScope = _serviceScopeFactory.CreateScope();
            var articleService = serviceScope.ServiceProvider.GetRequiredService<ArticleService>();

            const int DAYS_LIMIT = 30;

            _logger.LogInformation($"We are goint to remove articles older than {DAYS_LIMIT} days.");
            var affectedRows = await articleService.RemoveOlderThan(DAYS_LIMIT);
            
            _logger.LogInformation($"Articles that are older that {DAYS_LIMIT} days were deleted from database: {affectedRows}.");
        }
    }
}