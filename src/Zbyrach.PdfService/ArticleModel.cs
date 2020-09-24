using System;

namespace Zbyrach.Pdf
{
    public class ArticleModel
    {
        public long Id { get; set; }
        public string Url {get; set;}
        public DeviceType DeviceType { get; set; }
        public bool Inlined { get; set; }
        public byte[] PdfData { get; set; }
        public long PdfDataSize { get; set; }
        public DateTime StoredAt { get; set; }
    }
}