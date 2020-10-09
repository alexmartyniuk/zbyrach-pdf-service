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
            var article = await _articleService.FindOne(request.ArticleUrl, request.DeviceType, request.Inline);
            
            Stream stream = null;
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
        public async Task<IActionResult> QueueArticle([FromBody] QueueArticleRequest request)
        {         
            if (!await _articleService.IsExistByUrl(request.ArticleUrl))
            {
                await _articleService.QueueForGenerating(request.ArticleUrl);
            }           
                                               
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