using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public async Task<ArticleModel> Find(string url, DeviceType deviceType, bool inlined)
        {
            return await _context.Articles
                .SingleOrDefaultAsync(a =>
                    a.Url == url &&
                    a.DeviceType == deviceType &&
                    a.Inlined == inlined);
        }

        public async Task CreateOrUpdate(string url, DeviceType deviceType, bool inlined, Stream pdfStream)
        {
            var article = await Find(url, deviceType, inlined);
            if (article == null)
            {
                article = new ArticleModel
                {
                    DeviceType = deviceType,
                    Inlined = inlined,
                    Url = url,
                    PdfDataSize = pdfStream.Length,
                    PdfData = StreamToBytes(pdfStream),
                    StoredAt = DateTime.UtcNow
                };
                _context.Articles.Add(article);
            }
            else 
            {
                article.PdfData = StreamToBytes(pdfStream);
                article.PdfDataSize = pdfStream.Length;
                article.StoredAt = DateTime.UtcNow;
                _context.Articles.Update(article);
            }

            
            await _context.SaveChangesAsync();
        }

        public Task<List<ArticleModel>> DequeueForGenerating()
        {
            return _context.Articles
                .Where(a => a.PdfDataSize == 0)
                .ToListAsync();
        }

        public async Task<bool> IsExist(string articleUrl)
        {
            var count = await _context.Articles
                .CountAsync(a => a.Url == articleUrl);

            return count > 0;
        }

        public async Task QueueForGenerating(string articleUrl)
        {
            var pairs = new[] {
                (DeviceType.Desktop, true),
                (DeviceType.Desktop, false),
                (DeviceType.Mobile, true),
                (DeviceType.Mobile, false),
                (DeviceType.Tablet, true),
                (DeviceType.Tablet, false)
            };

            foreach (var pair in pairs)
            {
                _context.Articles.Add(new ArticleModel
                {
                    Url = articleUrl,
                    DeviceType = pair.Item1,
                    Inlined = pair.Item2,
                    PdfDataSize = 0,
                    PdfData = null,
                    StoredAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
        }

        private static byte[] StreamToBytes(Stream input)
        {
            input.Position = 0;
            using (var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}