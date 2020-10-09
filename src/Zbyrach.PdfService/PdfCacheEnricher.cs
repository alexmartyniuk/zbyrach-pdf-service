using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Zbyrach.Pdf
{
    public class PdfCacheEnricher : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<PdfCacheEnricher> _logger;
        private readonly PdfService _pdfService;

        public PdfCacheEnricher(IServiceScopeFactory serviceScopeFactory,
            PdfService pdfService,
            ILogger<PdfCacheEnricher> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _pdfService = pdfService;
            _logger = logger;            
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GenerateAndSave(stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unexpected error during saving PDF for articles.");
                }
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task GenerateAndSave(CancellationToken stoppingToken)
        {
            using var serviceScope = _serviceScopeFactory.CreateScope();
            var articleService = serviceScope.ServiceProvider.GetRequiredService<ArticleService>();

            var articles = await articleService.DequeueForGenerating();
            if (articles.Count == 0)
            {
                _logger.LogInformation($"No articles to proceed.");
            }

            _logger.LogInformation($"Found {articles.Count} articles for generating PDFs.");
            foreach (var group in articles.GroupBy(a => a.Url))
            {
                var articleUrl = group.Key;
                var deviceTypes = group
                    .Select(a => a.DeviceType)
                    .Distinct()
                    .ToArray();
                var inlines = group
                    .Select(a => a.Inlined)
                    .Distinct()
                    .ToArray();
                try
                {
                    _logger.LogInformation($"Started processing for: '{articleUrl}'");
                    await _pdfService.ConvertUrlToPdf(articleUrl, deviceTypes, inlines, async (device, inln, stream) =>
                    {
                        await articleService.CreateOrUpdate(articleUrl, device, inln, stream);
                    });
                    _logger.LogInformation($"Article was successfully processed: '{articleUrl}'");
                }
                catch (Exception e)
                {
                    await articleService.MarkAsFailed(articleUrl, e.Message);
                    _logger.LogError($"Cannot process '{articleUrl}' due: {e}");
                }
            }            
        }
    }
}