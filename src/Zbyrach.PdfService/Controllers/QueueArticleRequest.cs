using System.ComponentModel.DataAnnotations;

namespace Zbyrach.Pdf
{
    public class QueueArticleRequest
    {
        [Required]
        [Url]
        public string ArticleUrl { get; set; }
    }
}