using SinirKapisiYogunluk.Adapters;
using SinirKapisiYogunluk.Interfaces;
using SinirKapisiYogunluk.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// --- VERİ KAYNAĞI ---
// V3 kararı gereği: sistemin TEK canlı veri kaynağı TomTom'dur.
// Rastgele/sentetik fallback KALDIRILDI — TomTom başarısız olursa dürüstçe
// "Unavailable" durumu döner (bkz. TrafficApiBorderDataFetcher).
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<TrafficApiBorderDataFetcher>();
builder.Services.AddScoped<IBorderDataFetcher>(sp => sp.GetRequiredService<TrafficApiBorderDataFetcher>());

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseCors();

// --- API UÇ NOKTALARI ---

app.MapGet("/api/border-gates", async (IBorderDataFetcher fetcher) =>
{
    var results = await fetcher.GetAllGateStatusesAsync();
    return Results.Ok(results);
});

app.MapGet("/api/border-gates/{gateId}", async (string gateId, IBorderDataFetcher fetcher) =>
{
    try
    {
        var result = await fetcher.GetGateStatusAsync(gateId);
        return Results.Ok(result);
    }
    catch (ArgumentException)
    {
        return Results.NotFound(new { message = $"'{gateId}' koduna sahip bir sınır kapısı bulunamadı." });
    }
});

// --- KAMERA PROXY ---
// T.C. Ticaret Bakanlığı'nın halka açık kamera görüntülerini, tarayıcının
// doğrudan erişiminde karşılaşılan olası hotlink/Referer engelini aşmak için
// backend üzerinden aktarır. Güvenlik için YALNIZCA trakya.iscoz.com
// alan adından gelen görüntülere izin verilir.
app.MapGet("/api/camera-proxy", async (string url, IHttpClientFactory httpClientFactory) =>
{
    if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("https://trakya.iscoz.com/"))
    {
        return Results.BadRequest(new { message = "Yalnızca trakya.iscoz.com kaynaklı görüntülere izin verilir." });
    }

    var client = httpClientFactory.CreateClient();
    // Gerçekçi bir tarayıcı kimliği + resmi sayfadan geliyormuş gibi bir
    // "Referer" ekliyoruz — bazı sunucular hotlink korumasında bunu arar.
    client.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Add("Referer", "https://trakya.ticaret.gov.tr/yayinlar/canli-kameralar");
    client.DefaultRequestHeaders.Add("Accept", "image/jpeg,image/*,*/*");

    try
    {
        var response = await client.GetAsync(url);
        var bytes = await response.Content.ReadAsByteArrayAsync();

        if (!response.IsSuccessStatusCode)
        {
            // Artık hatayı GÖREBİLİYORUZ — sessizce yutmuyoruz.
            var bodyPreview = bytes.Length > 0 && bytes.Length < 2000
                ? System.Text.Encoding.UTF8.GetString(bytes)
                : $"({bytes.Length} bayt, metin olarak gösterilemiyor)";
            Console.WriteLine($"[TANI] Kamera proxy: {url} -> HTTP {(int)response.StatusCode}. Gövde: {bodyPreview}");
            return Results.Json(new
            {
                message = "Kaynak sunucudan görüntü alınamadı.",
                statusCode = (int)response.StatusCode,
                bodyPreview
            }, statusCode: (int)response.StatusCode);
        }

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
        return Results.File(bytes, contentType);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TANI] Kamera proxy hatası ({url}): {ex.GetType().Name}: {ex.Message}");
        return Results.Json(new { message = "Proxy isteği başarısız oldu.", error = ex.Message }, statusCode: 502);
    }
});

app.Run();
