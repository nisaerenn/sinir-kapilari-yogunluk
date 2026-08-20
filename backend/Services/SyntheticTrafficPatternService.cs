namespace SinirKapisiYogunluk.Services;

// Gerçek/otomatik veri kaynağı olmayan kapılar (Cilvegözü, Habur, Gürbulak vb. —
// "Estimated" kategorisi) için düz rastgele sayı üretmek yerine, güne göre
// değişen, gerçekçi bir yoğunluk deseni üretir.
//
// Mantık: gece sakin, sabah/akşam saatlerinde artan, akşamüstü zirve yapan
// basit bir "günlük yoğunluk eğrisi" (24 saatlik, parçalı-doğrusal) + üzerine
// %±15 rastgele gürültü eklenir — böylece hem gerçekçi bir örüntü olur hem de
// her istekte birebir aynı sayı tekrar etmez.
//
// ÖNEMLİ NOT: Buradaki saatlik çarpanlar VARSAYIMSALDIR (genel gözleme dayalı —
// "akşamüstü daha yoğun olur" gibi), gerçek istatistiksel veriye dayanmaz.
// İleride gerçek gözlem verisi biriktikçe (örn. Trakyasınırları'ndan elle
// toplanan kayıtlar) bu eğri kalibre edilebilir — bu, projenin "gelecek
// iyileştirmeler" listesindeki maddelerden biridir.
public class SyntheticTrafficPatternService
{
    private readonly Random _random = new();

    private double GetHourlyMultiplier(int hour) => hour switch
    {
        >= 0 and < 6 => 0.15,   // gece, çok sakin
        >= 6 and < 9 => 0.45,   // sabah artışı
        >= 9 and < 12 => 0.65,
        >= 12 and < 16 => 0.80,
        >= 16 and < 20 => 1.00, // günün zirvesi (akşamüstü)
        >= 20 and < 22 => 0.70,
        _ => 0.35                // 22-24 arası düşüş
    };

    // Kapının kapasitesine göre gerçekçi bir üst sınır belirler (2.5 saatlik
    // birikme potansiyeli varsayımıyla), sonra saatlik çarpan + rastgele
    // gürültü uygulayarak o anki tahmini araç sayısını üretir.
    public int GenerateVehicleCount(Models.GateInfo gate, DateTime? at = null)
    {
        var time = at ?? DateTime.Now;
        double maxCapacity = gate.ActiveBooths * gate.ProcessingRatePerHourPerBooth * 2.5;

        double multiplier = GetHourlyMultiplier(time.Hour);
        double noise = 0.85 + (_random.NextDouble() * 0.30); // 0.85 - 1.15 arası

        int count = (int)Math.Round(maxCapacity * multiplier * noise);
        return Math.Max(0, count);
    }
}
