using System.ComponentModel.DataAnnotations;

namespace Zbyrach.Pdf
{
    public class GeneratePdfRequest
    {
        [Required]
        [Url]
        public string ArticleUrl { get; set; }
        public DeviceType DeviceType { get; set; }
        public bool Inline { get; set; }
    }
}