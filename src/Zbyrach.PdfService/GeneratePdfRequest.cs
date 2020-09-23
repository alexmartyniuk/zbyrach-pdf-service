namespace Zbyrach.Pdf
{
    public class GeneratePdfRequest
    {
        public string ArticleUrl { get; set; }
        public DeviceType DeviceType { get; set; }
        public bool Inline { get; set; }
    }

    public enum DeviceType
    {
        Unknown = 0,
        Mobile = 1,
        Tablet = 2,
        Desktop = 3
    }
}