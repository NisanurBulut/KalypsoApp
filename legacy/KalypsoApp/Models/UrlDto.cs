using System.ComponentModel.DataAnnotations;

namespace KalypsoApp.Models
{
    public class UrlDto
    {
        public UrlDto()
        {

        }
        [Required]
        public string Url { get; set; } = "";
    }
}