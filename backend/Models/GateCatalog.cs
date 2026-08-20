namespace SinirKapisiYogunluk.Models;

// Bir sınır kapısının sabit bilgilerini, Kuyruk Teorisi parametrelerini VE
// TomTom Traffic API için gereken koordinatları bir arada taşır.
//
// ApproachLat/Lon + GateLat/Lon yalnızca TomTom entegrasyonu olan kapılarda
// doludur (5 Trakya kapısı). Diğer kapılarda bu alanlar null kalır —
// TrafficApiBorderDataFetcher bu durumda otomatik olarak manuel/tahmini
// veri kaynağına (fallback + SyntheticTrafficPatternService) yönlenir.
public record GateInfo(
    string Name,
    ConfidenceLevel Confidence,
    int ActiveBooths,
    double ProcessingRatePerHourPerBooth,
    double SystemDelayFactor = 1.15,
    double? ApproachLat = null,
    double? ApproachLon = null,
    double? GateLat = null,
    double? GateLon = null
);

public static class GateCatalog
{
    public static readonly Dictionary<string, GateInfo> Gates = new()
    {
        // --- Bulgaristan/Yunanistan (TomTom canlı trafik entegrasyonu var) ---
        ["KAP34"] = new GateInfo(
            "Kapıkule", ConfidenceLevel.NearRealTime,
            ActiveBooths: 8, ProcessingRatePerHourPerBooth: 10,
            ApproachLat: 41.700741003292485, ApproachLon: 26.419897824506126,
            GateLat: 41.71492422801163, GateLon: 26.362666968100108),

        ["IPS22"] = new GateInfo(
            "İpsala", ConfidenceLevel.NearRealTime,
            ActiveBooths: 6, ProcessingRatePerHourPerBooth: 9,
            ApproachLat: 40.905405, ApproachLon: 26.376308,
            GateLat: 40.933067, GateLon: 26.329245),

        ["HAM11"] = new GateInfo(
            "Hamzabeyli", ConfidenceLevel.NearRealTime,
            ActiveBooths: 4, ProcessingRatePerHourPerBooth: 8,
            ApproachLat: 41.936771, ApproachLon: 26.659862,
            GateLat: 41.958248, GateLon: 26.610754),

        ["DRK09"] = new GateInfo(
            "Dereköy (Aziziye)", ConfidenceLevel.NearRealTime,
            ActiveBooths: 3, ProcessingRatePerHourPerBooth: 7,
            ApproachLat: 41.973979, ApproachLon: 27.500414,
            GateLat: 41.967135, GateLon: 27.458010),

        ["PZK15"] = new GateInfo(
            "Pazarkule", ConfidenceLevel.NearRealTime,
            ActiveBooths: 3, ProcessingRatePerHourPerBooth: 7,
            ApproachLat: 41.678411, ApproachLon: 26.536414,
            GateLat: 41.654585, GateLon: 26.490973),

        // --- Yunanistan (koordinat/TomTom yok, tahmini kalıyor) ---
        ["UZK08"] = new GateInfo("Uzunköprü", ConfidenceLevel.Estimated, ActiveBooths: 2, ProcessingRatePerHourPerBooth: 6),

        // --- Suriye/Irak (koordinat yok, tahmini) ---
        ["CLV77"] = new GateInfo("Cilvegözü", ConfidenceLevel.Estimated, ActiveBooths: 5, ProcessingRatePerHourPerBooth: 9),
        ["ONC10"] = new GateInfo("Öncüpınar", ConfidenceLevel.Estimated, ActiveBooths: 3, ProcessingRatePerHourPerBooth: 7),
        ["COB11"] = new GateInfo("Çobanbey", ConfidenceLevel.Estimated, ActiveBooths: 2, ProcessingRatePerHourPerBooth: 6),
        ["KRK12"] = new GateInfo("Karkamış", ConfidenceLevel.Estimated, ActiveBooths: 2, ProcessingRatePerHourPerBooth: 6),
        ["HAB05"] = new GateInfo("Habur", ConfidenceLevel.Estimated, ActiveBooths: 6, ProcessingRatePerHourPerBooth: 8.5),
        ["UZM13"] = new GateInfo("Üzümlü (Çukurca)", ConfidenceLevel.Estimated, ActiveBooths: 2, ProcessingRatePerHourPerBooth: 5),

        // --- Gürcistan (koordinat yok, tahmini) ---
        ["SRP01"] = new GateInfo("Sarp", ConfidenceLevel.Estimated, ActiveBooths: 4, ProcessingRatePerHourPerBooth: 8),
        ["TGZ02"] = new GateInfo("Türkgözü", ConfidenceLevel.Estimated, ActiveBooths: 2, ProcessingRatePerHourPerBooth: 6),
        ["CLA14"] = new GateInfo("Çıldır-Aktaş", ConfidenceLevel.Estimated, ActiveBooths: 2, ProcessingRatePerHourPerBooth: 5),

        // --- İran (koordinat yok, tahmini) ---
        ["GRB03"] = new GateInfo("Gürbulak", ConfidenceLevel.Estimated, ActiveBooths: 4, ProcessingRatePerHourPerBooth: 7),
        ["ESN04"] = new GateInfo("Esendere", ConfidenceLevel.Estimated, ActiveBooths: 2, ProcessingRatePerHourPerBooth: 5),
        ["KPK06"] = new GateInfo("Kapıköy", ConfidenceLevel.Estimated, ActiveBooths: 2, ProcessingRatePerHourPerBooth: 5),

        // --- Azerbaycan (koordinat yok, tahmini) ---
        ["DLC07"] = new GateInfo("Dilucu", ConfidenceLevel.Estimated, ActiveBooths: 2, ProcessingRatePerHourPerBooth: 6),
    };
}
