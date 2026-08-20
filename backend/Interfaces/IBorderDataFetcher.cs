using SinirKapisiYogunluk.Models;

namespace SinirKapisiYogunluk.Interfaces;

// Bu arayüz, veri kaynağını soyutlar (Adapter Pattern).
// Aşama 1'de MockBorderDataFetcher bu arayüzü uygular.
// Aşama 2'de (kurumsal erişim sağlandığında) EdiXmlBorderDataFetcher
// diye yeni bir sınıf yazılıp aynı arayüzü uygulaması yeterli olacak —
// üstteki hiçbir kod değişmeyecek.
public interface IBorderDataFetcher
{
    Task<BorderGateStatusDto> GetGateStatusAsync(string gateId);
    Task<List<BorderGateStatusDto>> GetAllGateStatusesAsync();
}
