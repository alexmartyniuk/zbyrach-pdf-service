using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Zbyrach.Pdf
{
    public class ArticleService
    {
        private readonly ApplicationContext _context;

        public ArticleService(ApplicationContext context)
        {
            _context = context;
        }

        public async Task<ArticleModel> FindArticle(string url, DeviceType deviceType, bool inlined)
        {
            return await _context.Articles
                .SingleOrDefaultAsync(a =>
                    a.Url == url &&
                    a.DeviceType == deviceType &&
                    a.Inlined == inlined);
        }

        public async Task SaveArticle(string url, DeviceType deviceType, bool inlined, Stream pdfStream)
        {
            var article = new ArticleModel
            {
                DeviceType = deviceType,
                Inlined = inlined,
                Url = url,
                PdfDataSize = pdfStream.Length,
                PdfData = StreamToBytes(pdfStream),
                StoredAt = DateTime.UtcNow
            };

            _context.Articles
              .Add(article);
            await _context.SaveChangesAsync();
        }

        private static byte[] StreamToBytes(Stream input)
        {
            input.Position = 0;
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}