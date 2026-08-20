namespace SinirKapisiYogunluk.Models;

// SRS Bolum 7'deki veri modeline birebir karsilik gelir
public enum ConfidenceLevel
{
    NearRealTime,   // "Yakin Zamanli"  Bulgaristan/Yunanistan kapilari
    Estimated       // "Tahmini"  Suriye/Irak kapilari
}

public class BorderGateStatusDto
{
    public string GateId { get; set; } = string.Empty;
    public string GateName { get; set; } = string.Empty;
    public int WaitingVehicleCount { get; set; }
    public DateTime LastUpdated { get; set; }
    public ConfidenceLevel ConfidenceLevel { get; set; }
    public int EstimatedWaitMinutes { get; set; }

    // Verinin GERCEKTEN nereden geldigini belirtir , arayuzde seffaflik icin.
    //   "TomTom"    -> Bekleme suresi gercek, arac sayisi GERIYE HESAPLANMIS (tahmini)
    //   "Manual"    -> Hem bekleme suresi hem arac sayisi GERCEK (elle girildi)
    //   "Synthetic" -> Hem bekleme suresi hem arac sayisi TAHMINI (sentetik desen)
    public string Source { get; set; } = string.Empty;

    // Yalnizca Source == "TomTom" oldugunda dolu olur  "Kapiya Ulasim Trafigi"
    // kartinda ham, uydurulmamis TomTom verisini gostermek icin.
    public int? LiveTravelTimeSeconds { get; set; }
    public int? NoTrafficTravelTimeSeconds { get; set; }
    public int? TrafficDelaySeconds { get; set; }
}
