namespace KalypsoApp.Models
{
    public class UrlShortResponseDto
    {
        public UrlShortResponseDto(string Urlparam = "")
        {
            this.Url = Urlparam;
        }
        public string Url { get; set; } = "";
    }
}