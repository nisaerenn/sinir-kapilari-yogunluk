using Microsoft.EntityFrameworkCore;
using SinirKapisiYogunluk.Models.Entities;

namespace SinirKapisiYogunluk.Data;

// EF Core'un veritabanıyla konuşmasını sağlayan ana sınıf.
// Program.cs'de PostgreSQL bağlantısıyla birlikte DI'a kaydedilecek.
public class BorderCrossingDbContext : DbContext
{
    public BorderCrossingDbContext(DbContextOptions<BorderCrossingDbContext> options)
        : base(options)
    {
    }

    public DbSet<BorderGateObservation> Observations => Set<BorderGateObservation>();
}
