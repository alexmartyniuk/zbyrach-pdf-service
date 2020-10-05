using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Zbyrach.Pdf
{
    public class PdfService
    {

        private const int _timeout = 60000;
        private readonly string _chromiumExecutablePath;
        private readonly string _removeFolowLinkScript;
        private readonly string _removePageBreaksScript;
        private readonly string _leftOnlyArticleNodeScript;
        private readonly string _scrollPageToBottomScript;
        private readonly string _removeBannerTopScript;
        private readonly string _removeBannerFreeStoriesScript; 
        private string _lastLogMessage;

        public PdfService(IConfiguration configuration)
        {
            _chromiumExecutablePath = configuration["PUPPETEER_EXECUTABLE_PATH"];

            _removeFolowLinkScript = @"()=> {
                    const links = document.querySelectorAll('a, button');
                    for (let link of links) {
                        if (link.textContent.includes('Follow')) {
                            link.style.display = 'none';
                        }
                        if (link.getAttribute('target') !== '_blank') {
                            link.removeAttribute('href');              
                        } 
                    }
                    console.log('Follow links were removed.'); 
                }";

            _removePageBreaksScript = @"()=> {
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
                    console.log('Page breaks were removed.'); 
                }";

            _leftOnlyArticleNodeScript = @"()=> {
                const article = document.querySelectorAll('article')[0];
                if (article) {
                    const parent = article.parentNode;
                    parent.innerHTML = '';
                    parent.append(article);
                }
                console.log('All elements except the article were removed.'); 
            }";

            _scrollPageToBottomScript = @"()=> {                
                var currentScroll = 0;
                var scrollStep = 200;
                var scrollInterval = 100;

                function scrool() {
                    if (currentScroll > document.body.scrollHeight) {
                        console.log('Scrolling to the bottom was finished.'); 
                        return;
                    }
                    currentScroll += scrollStep;
                    window.scrollBy(0, scrollStep);   
                    setTimeout(scrool, scrollInterval);
                };
                
                scrool();            
            }";

            _removeBannerTopScript = @"()=> {
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
                console.log('The top banner was removed.'); 
            }";

            _removeBannerFreeStoriesScript = @"()=> {
                var banner = document.querySelector('article h4>span');
                if (banner && banner.textContent.includes('free stories left this month.')) {
                    var parent = banner.parentElement;
                    while (parent) {
                        if (parent.parentElement.nodeName == 'ARTICLE') {
                        break;
                        }
                        parent = parent.parentElement;
                    } 
                    parent.parentElement.removeChild(parent);    
                }
                console.log('The free stories banner was removed.');
            }";
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
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            inlines = inlines.OrderByDescending(i => i).ToArray();

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
                Timeout = _timeout
            };

            using var browser = await Puppeteer.LaunchAsync(options);
            using var page = await browser.NewPageAsync();

            page.Console += async (sender, args) =>
            {
                switch (args.Message.Type)
                {
                    case ConsoleType.Error:
                        try
                        {
                            var errorArgs = await Task.WhenAll(args.Message.Args.Select(arg => arg.ExecutionContext.EvaluateFunctionAsync("(arg) => arg instanceof Error ? arg.message : arg", arg)));
                            System.Console.WriteLine($"{args.Message.Text} args: [{string.Join<object>(", ", errorArgs)}]");
                        }
                        catch { }
                        break;
                    case ConsoleType.Warning:
                        System.Console.WriteLine(args.Message.Text);
                        break;
                    default:
                        _lastLogMessage = args.Message.Text;
                        System.Console.WriteLine(args.Message.Text);
                        break;
                }
            };

            await page.GoToAsync(url);

            await page.EvaluateFunctionAsync(_leftOnlyArticleNodeScript);
            await WaitUntil(() => _lastLogMessage == "All elements except the article were removed.");

            await page.EvaluateFunctionAsync(_scrollPageToBottomScript);
            await WaitUntil(() => _lastLogMessage == "Scrolling to the bottom was finished.");

            await page.EvaluateFunctionAsync(_removeBannerTopScript);
            await WaitUntil(() => _lastLogMessage == "The top banner was removed.");

            await page.EvaluateFunctionAsync(_removeBannerFreeStoriesScript);
            await WaitUntil(() => _lastLogMessage == "The free stories banner was removed.");

            await page.EvaluateFunctionAsync(_removeFolowLinkScript);
            await WaitUntil(() => _lastLogMessage == "Follow links were removed.");            

            foreach (var inline in inlines)
            {
                foreach (var deviceType in deviceTypes)
                {
                    if (!inline)
                    {
                        await page.EvaluateFunctionAsync(_removePageBreaksScript);
                        await WaitUntil(() => _lastLogMessage == "Page breaks were removed.");
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



        private async Task WaitUntil(Func<bool> condition, int frequency = 100, int timeout = _timeout)
        {
            var waitTask = Task.Run(async () =>
            {
                while (!condition()) await Task.Delay(frequency);
            });

            if (waitTask != await Task.WhenAny(waitTask,
                    Task.Delay(timeout)))
                throw new TimeoutException();
        }
    }
}