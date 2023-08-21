using System;

namespace KalypsoApp
{
    public static class KalypsoHelper
    {
        public static string GetRandomString()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789@az";
            var randomStrItem = new string(Enumerable.Repeat(chars, 6)
                .Select(x => x[random.Next(x.Length)]).ToArray());
            return randomStrItem;
        }
        public static string GetRequestUrl(HttpContext ctx, string randomStrItem = "")
        {
            return $"{ctx.Request.Scheme}://{ctx.Request.Host}/{randomStrItem}";
        }
    }
}
