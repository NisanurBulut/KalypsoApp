using System.ComponentModel.DataAnnotations;

namespace KalypsoApp.Models
{
    public class UrlCustomDto
    {
        [Required]
        public string Url { get; set; } = "";
        [Required]
        [MaxLength(6)]
        public string UrlCustomPart { get; set; } = "";
    }
}