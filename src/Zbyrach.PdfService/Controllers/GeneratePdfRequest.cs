namespace Zbyrach.Pdf
{
    public class GeneratePdfRequest
    {
        public string ArticleUrl { get; set; }
        public DeviceType DeviceType { get; set; }
        public bool Inline { get; set; }
    }
}