namespace SinirKapisiYogunluk.Models.Entities;

// Bu, DTO'dan farklı bir şey: bu sınıf doğrudan veritabanı tablosuna karşılık gelir.
// Her satır, "şu kapıda şu anda şu kadar araç bekliyor" şeklinde tek bir
// manuel gözlem kaydını temsil eder. Zamanla aynı kapı için birden fazla
// kayıt birikecek — biz her zaman en sonuncusunu okuyacağız.
public class BorderGateObservation
{
    public int Id { get; set; }
    public string GateId { get; set; } = string.Empty;
    public int WaitingVehicleCount { get; set; }
    public DateTime RecordedAt { get; set; }
}
