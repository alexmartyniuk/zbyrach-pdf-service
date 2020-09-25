using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Zbyrach.Pdf
{
    public class PdfCachePropagator : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<PdfCachePropagator> _logger;
        private readonly PdfService _pdfService;

        public PdfCachePropagator(IServiceScopeFactory serviceScopeFactory,
            PdfService pdfService,
            ILogger<PdfCachePropagator> logger)
        {
            _pdfService = pdfService;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SavePdf(stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unexpected error during saving PDF for articles.");
                }
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task SavePdf(CancellationToken stoppingToken)
        {
            using var serviceScope = _serviceScopeFactory.CreateScope();
            var articleService = serviceScope.ServiceProvider.GetRequiredService<ArticleService>();

            var articles = await articleService.DequeueForGenerating();
            foreach (var group in articles.GroupBy(a => a.Url))
            {
                var articleUrl = group.Key;
                var deviceTypes = group.Select(a => a.DeviceType).Distinct().ToArray();
                var inlines = group.Select(a => a.Inlined).Distinct().ToArray();

                try
                {
                    await _pdfService.ConvertUrlToPdf(articleUrl, deviceTypes, inlines, async (device, inln, stream) =>
                    {
                        await articleService.CreateOrUpdate(articleUrl, device, inln, stream);
                    });

                }
                catch (Exception e)
                {
                    await articleService.MarkAsFailed(articleUrl, e.Message);
                    _logger.LogError($"Cannot generate PDF for {articleUrl}: {e}");
                }
            }
        }
    }
}