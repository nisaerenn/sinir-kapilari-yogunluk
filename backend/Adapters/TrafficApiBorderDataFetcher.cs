using System.Globalization;
using System.Text.Json;
using SinirKapisiYogunluk.Interfaces;
using SinirKapisiYogunluk.Models;

namespace SinirKapisiYogunluk.Adapters;

// V3 kararı: bu sistemin veri kaynağı YALNIZCA TomTom Traffic API'dir
// (+ arayüzde ayrıca gösterilen, işlenmemiş kamera görüntüleri).
// Rastgele/sentetik ("uydurma") veri ARTIK KULLANILMIYOR — TomTom başarısız
// olursa, sahte bir sayı üretmek yerine dürüstçe "Unavailable" durumu
// döndürülür; arayüz bunu "veri şu an alınamıyor" olarak gösterir.
public class TrafficApiBorderDataFetcher : IBorderDataFetcher
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public TrafficApiBorderDataFetcher(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<BorderGateStatusDto> GetGateStatusAsync(string gateId)
    {
        if (!GateCatalog.Gates.TryGetValue(gateId, out var gate))
            throw new ArgumentException($"Bilinmeyen sınır kapısı kodu: {gateId}");

        if (gate.ApproachLat is null || gate.GateLat is null)
        {
            return Unavailable(gateId, gate.Name, "Bu kapı için TomTom koordinatı tanımlı değil.");
        }

        var apiKey = _config["TomTom:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Unavailable(gateId, gate.Name, "TomTom API anahtarı appsettings'te bulunamadı.");
        }

        var ci = CultureInfo.InvariantCulture;
        var url =
            $"https://api.tomtom.com/routing/1/calculateRoute/" +
            $"{gate.ApproachLat!.Value.ToString(ci)},{gate.ApproachLon!.Value.ToString(ci)}:" +
            $"{gate.GateLat!.Value.ToString(ci)},{gate.GateLon!.Value.ToString(ci)}/json" +
            $"?traffic=true&computeTravelTimeFor=all&key={apiKey}";

        const int maxAttempts = 3;
        HttpResponseMessage? response = null;

        try
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode) break;

                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[TANI] {gateId}: Deneme {attempt}/{maxAttempts} başarısız. HTTP {(int)response.StatusCode} - {errorBody}");

                if (attempt < maxAttempts) await Task.Delay(400 * attempt);
            }

            if (response is null || !response.IsSuccessStatusCode)
            {
                return Unavailable(gateId, gate.Name, $"{maxAttempts} deneme sonunda TomTom'dan yanıt alınamadı.");
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            var summary = doc.RootElement.GetProperty("routes")[0].GetProperty("summary");

            int liveTravelTime = summary.GetProperty("travelTimeInSeconds").GetInt32();
            int noTrafficTravelTime = summary.GetProperty("noTrafficTravelTimeInSeconds").GetInt32();
            int trafficDelaySeconds = summary.GetProperty("trafficDelayInSeconds").GetInt32();
            int estimatedWaitMinutes = (int)Math.Ceiling(trafficDelaySeconds / 60.0);

            double totalCapacityPerHour = gate.ActiveBooths * gate.ProcessingRatePerHourPerBooth;
            int approximateVehicleCount = (int)Math.Round(
                (estimatedWaitMinutes / gate.SystemDelayFactor / 60.0) * totalCapacityPerHour);

            return new BorderGateStatusDto
            {
                GateId = gateId,
                GateName = gate.Name,
                WaitingVehicleCount = Math.Max(0, approximateVehicleCount),
                LastUpdated = DateTime.Now,
                ConfidenceLevel = ConfidenceLevel.NearRealTime,
                EstimatedWaitMinutes = estimatedWaitMinutes,
                Source = "TomTom",
                LiveTravelTimeSeconds = liveTravelTime,
                NoTrafficTravelTimeSeconds = noTrafficTravelTime,
                TrafficDelaySeconds = trafficDelaySeconds
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TANI] {gateId}: Beklenmeyen hata - {ex.GetType().Name}: {ex.Message}");
            return Unavailable(gateId, gate.Name, "Beklenmeyen bir hata oluştu.");
        }
    }

    private static BorderGateStatusDto Unavailable(string gateId, string gateName, string reason)
    {
        Console.WriteLine($"[TANI] {gateId}: Veri alınamadı - {reason}");
        return new BorderGateStatusDto
        {
            GateId = gateId,
            GateName = gateName,
            WaitingVehicleCount = 0,
            LastUpdated = DateTime.Now,
            ConfidenceLevel = ConfidenceLevel.Estimated,
            EstimatedWaitMinutes = 0,
            Source = "Unavailable"
        };
    }

    public async Task<List<BorderGateStatusDto>> GetAllGateStatusesAsync()
    {
        var results = new List<BorderGateStatusDto>();
        var gateIds = GateCatalog.Gates.Keys.ToList();
        for (int i = 0; i < gateIds.Count; i++)
        {
            results.Add(await GetGateStatusAsync(gateIds[i]));
            if (i < gateIds.Count - 1) await Task.Delay(150);
        }
        return results;
    }
}
