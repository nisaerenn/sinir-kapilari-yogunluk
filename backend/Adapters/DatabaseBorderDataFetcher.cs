using Microsoft.EntityFrameworkCore;
using SinirKapisiYogunluk.Data;
using SinirKapisiYogunluk.Interfaces;
using SinirKapisiYogunluk.Models;
using SinirKapisiYogunluk.Services;

namespace SinirKapisiYogunluk.Adapters;

// Ana veri kaynağı adaptörü.
// Mantık: her kapı için veritabanında en son manuel girilen kaydı ara.
//   - Kayıt varsa: onu kullan (Source = "Manual", GERÇEK araç sayısı).
//   - Kayıt yoksa VEYA kapı zaten "Tahmini" tipteyse:
//     SyntheticTrafficPatternService ile güne göre değişen, gerçekçi bir
//     yaklaşık değer üret (Source = "Synthetic", TAHMİNİ araç sayısı).
public class DatabaseBorderDataFetcher : IBorderDataFetcher
{
    private readonly BorderCrossingDbContext _db;
    private readonly WaitTimeEstimationService _estimator;
    private readonly SyntheticTrafficPatternService _patternGenerator;

    public DatabaseBorderDataFetcher(
        BorderCrossingDbContext db,
        WaitTimeEstimationService estimator,
        SyntheticTrafficPatternService patternGenerator)
    {
        _db = db;
        _estimator = estimator;
        _patternGenerator = patternGenerator;
    }

    public async Task<BorderGateStatusDto> GetGateStatusAsync(string gateId)
    {
        if (!GateCatalog.Gates.TryGetValue(gateId, out var gate))
            throw new ArgumentException($"Bilinmeyen sınır kapısı kodu: {gateId}");

        var latestObservation = await _db.Observations
            .Where(o => o.GateId == gateId)
            .OrderByDescending(o => o.RecordedAt)
            .FirstOrDefaultAsync();

        int vehicleCount;
        DateTime lastUpdated;
        ConfidenceLevel confidence;
        string source;

        if (latestObservation is not null && gate.Confidence == ConfidenceLevel.NearRealTime)
        {
            vehicleCount = latestObservation.WaitingVehicleCount;
            lastUpdated = latestObservation.RecordedAt;
            confidence = ConfidenceLevel.NearRealTime;
            source = "Manual";
        }
        else
        {
            vehicleCount = _patternGenerator.GenerateVehicleCount(gate);
            lastUpdated = DateTime.Now;
            confidence = ConfidenceLevel.Estimated;
            source = "Synthetic";
        }

        return new BorderGateStatusDto
        {
            GateId = gateId,
            GateName = gate.Name,
            WaitingVehicleCount = vehicleCount,
            LastUpdated = lastUpdated,
            ConfidenceLevel = confidence,
            EstimatedWaitMinutes = _estimator.EstimateWaitMinutes(vehicleCount, gate),
            Source = source
        };
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
