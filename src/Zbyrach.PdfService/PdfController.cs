using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Net;

namespace Zbyrach.Pdf
{
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
        public async Task<IActionResult> GetPdf([FromBody] GeneratePdfRequest request)
        {
            var article = await _articleService.Find(request.ArticleUrl, request.DeviceType, request.Inline);
            Stream stream = null;
            if (article?.PdfDataSize != 0)
            {
                stream = new MemoryStream(article.PdfData); 
            }
            else
            {
                stream = await _pdfService.ConvertUrlToPdf(request.ArticleUrl, request.DeviceType, request.Inline);                
                await _articleService.CreateOrUpdate(request.ArticleUrl, request.DeviceType, request.Inline, stream);
            }
            
            stream.Position = 0;
            var fileName = GetPdfFileName(request.ArticleUrl);            
            
            Response.Headers[HeaderNames.ContentDisposition] = new ContentDisposition
            {
                FileName = fileName,
                DispositionType = request.Inline ? DispositionTypeNames.Inline : DispositionTypeNames.Attachment
            }.ToString();           

            return File(stream, "application/pdf");
        }

        [HttpPost]
        [Route("/queue")]
        public async Task<IActionResult> QueueArticle([FromBody] QueueArticleRequest request)
        {         
            if (await _articleService.IsExist(request.ArticleUrl))
            {
                return Ok();
            }

            await _articleService.QueueForGenerating(request.ArticleUrl);
                                               
            return Ok();
        }

        private string GetPdfFileName(string url)
        {
            var uri = new Uri(url.ToLower());
            var fileName = Path.GetFileName(uri.LocalPath) + ".pdf";
            return WebUtility.UrlEncode(fileName);
        }
    }
}