using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using Microsoft.Extensions.Logging;

namespace Zbyrach.Pdf
{
    public class PdfService
    {
        private const int PDF_GENERATION_TIMEOUT = 60000;
        private readonly string _chromiumExecutablePath;
        private readonly ILogger<PdfService> _logger;

        public PdfService(IConfiguration configuration, ILogger<PdfService> logger)
        {
            _chromiumExecutablePath = configuration["PUPPETEER_EXECUTABLE_PATH"];
            _logger = logger;
        }

        public async Task<Stream> ConvertUrlToPdf(string url, DeviceType deviceType, bool inline = false)
        {
            Stream result = null;
            await ConvertUrlToPdf(url, new DeviceType[] { deviceType }, new bool[] { inline }, async (device, inln, stream) =>
                 {
                     result = stream;
                 });
            return result;
        }

        public async Task ConvertUrlToPdf(string url, DeviceType[] deviceTypes, bool[] inlines, Func<DeviceType, bool, Stream, Task> callback)
        {
            inlines = inlines
                .OrderByDescending(i => i)
                .ToArray();

            var options = new LaunchOptions
            {
                Headless = true,
                Args = new[]
                {
                    "--no-sandbox",
                    "--disable-plugins",
                    "--incognito",
                    "--disable-sync",
                    "--disable-gpu",
                    "--disable-speech-api",
                    "--disable-remote-fonts",
                    "--disable-shared-workers",
                    "--disable-webgl",
                    "--no-experiments",
                    "--no-first-run",
                    "--no-default-browser-check",
                    "--no-wifi",
                    "--no-pings",
                    "--no-service-autorun",
                    "--disable-databases",
                    "--disable-default-apps",
                    "--disable-demo-mode",
                    "--disable-notifications",
                    "--disable-permissions-api",
                    "--disable-background-networking",
                    "--disable-3d-apis",
                    "--disable-bundled-ppapi-flash",
                 },
                ExecutablePath = _chromiumExecutablePath,
                Timeout = PDF_GENERATION_TIMEOUT
            };

            using var browser = await Puppeteer.LaunchAsync(options);
            using var page = await browser.NewPageAsync();

            await page.GoToAsync(url);

            await RemoveRedundantContent(page);
            await ScrollPageToBottom(page);
            await RemoveTopBanner(page);
            await RemoveFreeStoriesLeftBanner(page);
            await RemoveFollowLinks(page);

            foreach (var inline in inlines)
            {
                foreach (var deviceType in deviceTypes)
                {
                    if (!inline)
                    {
                        await RemovePageBreaks(page);
                    }

                    var format = deviceType switch
                    {
                        DeviceType.Mobile => PaperFormat.A6,
                        DeviceType.Tablet => inline ? PaperFormat.A4 : PaperFormat.A5,
                        DeviceType.Desktop => PaperFormat.A4,
                        _ => throw new ArgumentOutOfRangeException(nameof(deviceType))
                    };

                    var stream = await page.PdfStreamAsync(new PdfOptions
                    {
                        Format = format,
                        MarginOptions = new PuppeteerSharp.Media.MarginOptions
                        {
                            Top = inline ? "0px" : "40px",
                            Bottom = inline ? "0px" : "40px"
                        }
                    });

                    await callback(deviceType, inline, stream);
                }
            }
        }

        private async Task RemovePageBreaks(Page page)
        {
            var markerString = Guid.NewGuid().ToString();
            await ExecuteJavascript(page, @"()=> {
                    var style = document.createElement('style');
                    style.innerHTML = `
                        h1, h2 {
                            page-break-inside: avoid;
                        }
                        h1::after, h2::after {
                            content: '';
                            display: block;
                            height: 100px;
                            margin-bottom: -100px;
                        }
                        .paragraph-image, figure {
                            page-break-inside: avoid;
                            page-break-before: auto;
                            page-break-after: auto;
                        }
                        `;
                    document.head.appendChild(style);                  
                    console.log('" + markerString + @"'); 
                }", markerString);
            _logger.LogInformation("Page breaks were removed.");
        }

        private async Task RemoveFollowLinks(Page page)
        {
            var markerString = Guid.NewGuid().ToString();
            await ExecuteJavascript(page, @"()=> {
                const links = document.querySelectorAll('a, button');
                for (let link of links) {
                    if (link.textContent.includes('Follow')) {
                        link.style.display = 'none';
                    }
                    if (link.getAttribute('target') !== '_blank') {
                        link.removeAttribute('href');              
                    } 
                }
                console.log('" + markerString + @"'); 
            }", markerString);
            _logger.LogInformation("Follow links were removed.");
        }

        private async Task RemoveFreeStoriesLeftBanner(Page page)
        {
            var markerString = Guid.NewGuid().ToString();
            await ExecuteJavascript(page, @"()=> {
                var banner = document.querySelector('article h4>span');
                if (banner && banner.textContent.includes('stories left this month.')) {
                    var parent = banner.parentElement;
                    while (parent) {
                        if (parent.parentElement.nodeName == 'ARTICLE') {
                        break;
                        }
                        parent = parent.parentElement;
                    } 
                    parent.parentElement.removeChild(parent);    
                }
                console.log('" + markerString + @"'); 
            }", markerString);
            _logger.LogInformation("The free stories banner was removed.");
        }

        private async Task RemoveTopBanner(Page page)
        {
            var markerString = Guid.NewGuid().ToString();
            await ExecuteJavascript(page, @"()=> {
                var banner = document.querySelector('.branch-journeys-top');
                if (banner) {
                    var parent = banner.parentElement;
                    while (parent) {
                        if (parent.parentElement == document.body) {
                        break;
                        }
                        parent = parent.parentElement;
                    } 
                    document.body.removeChild(parent);
                } 
                console.log('" + markerString + @"'); 
            }", markerString);
            _logger.LogInformation("The top banner was removed.");
        }

        private async Task ScrollPageToBottom(Page page)
        {
            var markerString = Guid.NewGuid().ToString();
            await ExecuteJavascript(page, @"()=> {                
                var currentScroll = 0;
                var scrollStep = 200;
                var scrollInterval = 100;

                function scrool() {
                    if (currentScroll > document.body.scrollHeight) {
                        console.log('" + markerString + @"'); 
                        return;
                    }
                    currentScroll += scrollStep;
                    window.scrollBy(0, scrollStep);   
                    setTimeout(scrool, scrollInterval);
                };
                
                scrool();            
            }", markerString);
            _logger.LogInformation("Scrolling to the bottom was finished.");
        }

        private async Task RemoveRedundantContent(Page page)
        {
            var markerString = Guid.NewGuid().ToString();
            await ExecuteJavascript(page, @"()=> {
                const article = document.querySelectorAll('article')[0];
                if (article) {
                    const parent = article.parentNode;
                    parent.innerHTML = '';
                    parent.append(article);
                }  
                console.log('" + markerString + @"');               
            }", markerString);
            _logger.LogInformation("Redundant content was removed.");
        }

        private async Task ExecuteJavascript(Page page, string javaScript, string markerString)
        {
            if (string.IsNullOrEmpty(markerString))
            {
                throw new Exception("Marker string can not be empty.");
            }

            var lastLogMessage = string.Empty;

            async void ConsoleHandler(object sender, ConsoleEventArgs args)
            {
                switch (args.Message.Type)
                {
                    case ConsoleType.Error:
                        try
                        {
                            var errorArgs = await Task.WhenAll(args.Message.Args.Select(
                                arg => arg.ExecutionContext.EvaluateFunctionAsync("(arg) => arg instanceof Error ? arg.message : arg", arg)));
                            _logger.LogError($"{args.Message.Text} args: [{string.Join<object>(", ", errorArgs)}]");
                        }
                        catch { }
                        break;
                    case ConsoleType.Warning:
                        _logger.LogWarning(args.Message.Text);
                        break;
                    default:
                        lastLogMessage = args.Message.Text;
                        break;
                }
            };

            page.Console += ConsoleHandler;
            try
            {
                await page.EvaluateFunctionAsync(javaScript);
                await WaitUntil(() => lastLogMessage == markerString);
            }
            finally
            {
                page.Console -= ConsoleHandler;
            }
        }
        private async Task WaitUntil(Func<bool> condition, int frequency = 100, int timeout = PDF_GENERATION_TIMEOUT)
        {
            var waitTask = Task.Run(async () =>
            {
                while (!condition())
                {
                    await Task.Delay(frequency);
                }
            });

            if (waitTask != await Task.WhenAny(waitTask,
                    Task.Delay(timeout)))
                throw new TimeoutException();
        }
    }
}