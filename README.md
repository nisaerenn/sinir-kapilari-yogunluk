# Sınır Kapısı Yoğunluk

Takobil platformuna entegre edilmek üzere geliştirilen, Trakya bölgesindeki sınır kapılarının gerçek zamanlı bekleme yoğunluğunu gösteren bir modül.

## Sistem Nasıl Çalışıyor

1. Backend, **Kapıkule, Hamzabeyli, İpsala, Pazarkule ve Dereköy** sınır kapıları için **TomTom Traffic API**'den canlı yol trafiği verisi çeker (yaklaşım noktası ile kapı arasındaki güncel/normal seyahat süresi farkı).
2. Bu fark, her kapının kendi peron sayısı ve işlem hızına göre Kuyruk Teorisi (Little's Law) formülüyle tahmini bekleme süresine çevrilir.
3. TomTom'dan yanıt alınamazsa sistem **uydurma bir sayı göstermez** — dürüstçe "veri alınamıyor" durumunu döner.
4. Arayüz, bu veriyi kart bazlı bir dashboard'da gösterir; her karta tıklandığında TomTom'un ham trafik verisi ve T.C. Ticaret Bakanlığı'nın resmi canlı kamera görüntüsü (varsa) detay olarak açılır.
5. Veri kaynağı katmanı Adapter Pattern ile izole edilmiştir — ileride gerçek bir kurumsal veri kaynağına geçilirse, değişecek tek yer burasıdır.

## Kurulum

### Backend

```bash
cd backend
dotnet restore
dotnet run
```

`appsettings.Development.json` dosyasına kendi TomTom API anahtarınızı ekleyin:
```json
{
  "TomTom": { "ApiKey": "BURAYA_ANAHTARINIZI_YAZIN" }
}
```
Bu dosya `.gitignore` ile depoya dahil edilmez.

### Frontend

```bash
cd frontend
npm install
npm run dev
```

Uygulama `http://localhost:5173` üzerinde çalışır, backend'e `http://localhost:5119` üzerinden bağlanır.

## Klasör Yapısı

```
backend/     → C# / .NET 8 API
frontend/    → React arayüz
docs/        → SRS ve veri kaynağı raporları
```
