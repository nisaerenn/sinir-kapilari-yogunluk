using SinirKapisiYogunluk.Interfaces;
using SinirKapisiYogunluk.Models;
using SinirKapisiYogunluk.Services;

namespace SinirKapisiYogunluk.Adapters;

// Not: Bu sınıf Program.cs'de kayıtlı DEĞİL — yerini DatabaseBorderDataFetcher aldı.
// Referans olarak / gerekirse hızlıca geri dönmek için burada tutuluyor.
public class MockBorderDataFetcher : IBorderDataFetcher
{
    private readonly Random _random = new();
    private readonly WaitTimeEstimationService _estimator;

    public MockBorderDataFetcher(WaitTimeEstimationService estimator)
    {
        _estimator = estimator;
    }

    public Task<BorderGateStatusDto> GetGateStatusAsync(string gateId)
    {
        if (!GateCatalog.Gates.TryGetValue(gateId, out var gate))
            throw new ArgumentException($"Bilinmeyen sınır kapısı kodu: {gateId}");

        var vehicleCount = _random.Next(5, 180);

        var dto = new BorderGateStatusDto
        {
            GateId = gateId,
            GateName = gate.Name,
            WaitingVehicleCount = vehicleCount,
            LastUpdated = DateTime.Now,
            ConfidenceLevel = gate.Confidence,
            EstimatedWaitMinutes = _estimator.EstimateWaitMinutes(vehicleCount, gate)
        };

        return Task.FromResult(dto);
    }

    public async Task<List<BorderGateStatusDto>> GetAllGateStatusesAsync()
    {
        var results = new List<BorderGateStatusDto>();
        foreach (var gateId in GateCatalog.Gates.Keys)
        {
            results.Add(await GetGateStatusAsync(gateId));
        }
        return results;
    }
}
