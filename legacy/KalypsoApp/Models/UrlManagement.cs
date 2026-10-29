using System;

namespace KalypsoApp.Models
{

    public class UrlManagement
    {
        public UrlManagement()
        {
            
        }
        public UrlManagement(string urlParam = "", string shortUrlParam = "", string urlPartParam = "")
        {
            this.Url = urlParam;
            this.ShortUrl = shortUrlParam;
            this.ShortUrlPart = urlPartParam;
        }
        public int Id { get; set; }
        public string Url { get; set; } = "";
        public string ShortUrlPart { get; set; } = "";

        public string ShortUrl { get; set; } = "";
        public DateTime CreatedAt = DateTime.Now;

    }
}