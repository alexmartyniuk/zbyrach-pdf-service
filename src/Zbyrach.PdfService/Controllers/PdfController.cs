using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace Zbyrach.Pdf
{
    [Authorize]
    public class PdfController : Controller
    {
        private readonly PdfService _pdfService;
        private readonly ArticleService _articleService;

        public PdfController(PdfService pdfService, ArticleService articleService)
        {
            _pdfService = pdfService;
            _articleService = articleService;
        }

        [HttpPost]
        [Route("/pdf")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetPdf([FromBody] GeneratePdfRequest request)
        {
            var article = await _articleService.FindOne(request.ArticleUrl, request.DeviceType, request.Inline);

            Stream stream;
            if (article != null && article.PdfDataSize != 0)
            {
                stream = new MemoryStream(article.PdfData);
            }
            else
            {
                stream = await _pdfService.ConvertUrlToPdf(request.ArticleUrl, request.DeviceType, request.Inline);
                await _articleService.CreateOrUpdate(request.ArticleUrl, request.DeviceType, request.Inline, stream);
            }

            var contentDisposition = new ContentDisposition
            {
                FileName = GetPdfFileName(request.ArticleUrl),
                DispositionType = request.Inline ? DispositionTypeNames.Inline : DispositionTypeNames.Attachment
            };

            Response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();
            stream.Position = 0;

            return File(stream, "application/pdf");
        }

        [HttpPost]
        [Route("/queue")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> QueueArticle([FromBody] QueueArticleRequest request)
        {
            if (!await _articleService.IsExistByUrl(request.ArticleUrl))
            {
                await _articleService.QueueForGenerating(request.ArticleUrl);
            }

            return Ok();
        }

        [HttpGet]
        [Route("/statistic")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<StatisticResponse> Statistic()
        {
            var totalSizeInBytes = await _articleService.GetTotalSizeInBytes();
            var totalRowsCount = await _articleService.GetTotalRowsCount();

            return new StatisticResponse
            {
                TotalRowsCount = totalRowsCount,
                TotalSizeInBytes = totalSizeInBytes
            };
        }

        [HttpDelete]
        [Route("/cleanup/{days}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Cleanup([FromRoute]int days)
        {
            await _articleService.RemoveOlderThan(days);

            return NoContent();            
        }

        private string GetPdfFileName(string url)
        {
            var uri = new Uri(url.ToLower());
            var fileName = Path.GetFileName(uri.LocalPath) + ".pdf";
            return WebUtility.UrlEncode(fileName);
        }
    }
}