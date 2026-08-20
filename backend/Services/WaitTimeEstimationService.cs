using SinirKapisiYogunluk.Models;

namespace SinirKapisiYogunluk.Services;

// Kuyruk Teorisi (Queueing Theory / Little's Law) prensiplerine dayanan tahmin motoru.
//   Toplam Kapasite (arac/saat) = Aktif Peron Sayisi × Peron Basi Saatlik Hiz
//   Temel Bekleme (dakika)      = (Bekleyen Arac Sayisi / Toplam Kapasite) × 60
//   Nihai Tahmin (dakika)       = Temel Bekleme × Sistem Gecikme Katsayisi
//
// Not: ActiveBooths / ProcessingRatePerHourPerBooth değerleri GateCatalog'da
// varsayimsal olarak tanimli; gercek gozlemlerle kalibre edilmeye aciktir.
public class WaitTimeEstimationService
{
    public int EstimateWaitMinutes(int waitingVehicleCount, GateInfo gate)
    {
        if (waitingVehicleCount <= 0)
            return 0;

        double totalCapacityPerHour = gate.ActiveBooths * gate.ProcessingRatePerHourPerBooth;
        double baseMinutes = (waitingVehicleCount / totalCapacityPerHour) * 60;
        double adjustedMinutes = baseMinutes * gate.SystemDelayFactor;

        return (int)Math.Ceiling(adjustedMinutes);
    }
}
