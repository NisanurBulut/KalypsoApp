using KalypsoApp.Models;
using Microsoft.EntityFrameworkCore;

namespace KalypsoApp.Data
{
    public class KalypsoDbContext : DbContext
    {
        public virtual DbSet<UrlManagement> Urls { get; set; }
        public KalypsoDbContext(DbContextOptions<KalypsoDbContext> options) : base(options)
        {

        }
    }
}