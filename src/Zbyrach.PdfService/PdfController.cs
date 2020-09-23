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

        public PdfController(PdfService pdfService)
        {
            _pdfService = pdfService;
        }

        [HttpPost]
        [Route("/pdf")]
        public async Task<IActionResult> GetPdf([FromBody] GeneratePdfRequest request)
        {
            var stream = await _pdfService.ConvertUrlToPdf(request.ArticleUrl, request.DeviceType, request.Inline);
            var fileName = GetPdfFileName(request.ArticleUrl);
            
            Response.Headers[HeaderNames.ContentDisposition] = new ContentDisposition
            {
                FileName = fileName,
                DispositionType = request.Inline ? DispositionTypeNames.Inline : DispositionTypeNames.Attachment
            }.ToString();           

            return File(stream, "application/pdf");
        }

        private string GetPdfFileName(string url)
        {
            var uri = new Uri(url.ToLower());
            var fileName = Path.GetFileName(uri.LocalPath) + ".pdf";
            return WebUtility.UrlEncode(fileName);
        }
    }
}