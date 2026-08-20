import { useEffect, useState, useCallback, useMemo } from "react";
import "./SinirKapisiTrafikIzleme.css";

const API_BASE_URL = "http://localhost:5119";
const POLL_INTERVAL_MS = 45000;

// V3 kararı: yalnızca 5 Trakya kapısı, sabit metadata + gerçek kamera URL'leri
// (T.C. Ticaret Bakanlığı Trakya Bölge Müdürlüğü canlı kamera sayfasından,
// 17 Ağustos 2026'da doğrulanmıştır — 10 dakikada bir güncellenen statik JPG'ler).
const GATES_META = {
  KAP34: {
    name: "Kapıkule",
    location: "Edirne · Bulgaristan sınırı",
    road: "D100 Edirne – Kapıkule bağlantısı",
    cameras: [
      { label: "Yolcu Giriş Peronları", url: "https://trakya.iscoz.com/kapikule/yolcugiris.jpg" },
      { label: "Yolcu Çıkış Hudut", url: "https://trakya.iscoz.com/kapikule/yolcucikis1.jpg" },
      { label: "Yolcu Çıkış Peronları", url: "https://trakya.iscoz.com/kapikule/yolcucikis.jpg" },
      { label: "Yolcu Çıkış Edirne Kapı", url: "https://trakya.iscoz.com/kapikule/edirnekapi.jpg" },
    ],
  },
  HAM11: {
    name: "Hamzabeyli",
    location: "Edirne · Bulgaristan sınırı",
    road: "Edirne – Hamzabeyli bağlantı yolu",
    cameras: [
      { label: "Yolcu Giriş", url: "https://trakya.iscoz.com/hamzabeyli/yolcugiris.jpg" },
      { label: "Yolcu Çıkış", url: "https://trakya.iscoz.com/hamzabeyli/yolcucikis.jpg" },
      { label: "Yolcu Çıkış Türkiye Yolu", url: "https://trakya.iscoz.com/hamzabeyli/turkiyeyolu.jpg" },
    ],
  },
  IPS22: {
    name: "İpsala",
    location: "Edirne · Yunanistan sınırı",
    road: "D110 Keşan – İpsala bağlantısı",
    cameras: [
      { label: "Yolcu Giriş", url: "https://trakya.iscoz.com/ipsala/GirisPeronlar.jpg" },
      { label: "Yolcu Çıkış (Yunan Tarafı)", url: "https://trakya.iscoz.com/ipsala/Turkiye-Giris-YunanTarafi.jpg" },
      { label: "TR Kapı", url: "https://trakya.iscoz.com/ipsala/Turkiye-Cikis-TurkiyeTarafi.jpg" },
    ],
  },
  PZK15: {
    name: "Pazarkule",
    location: "Edirne · Yunanistan sınırı",
    road: "Edirne – Pazarkule bağlantı yolu",
    cameras: [
      { label: "Yolcu Giriş", url: "https://trakya.iscoz.com/pazarkule/yolcugiris.jpg" },
      { label: "Yolcu Çıkış", url: "https://trakya.iscoz.com/pazarkule/yolcucikis.jpg" },
    ],
  },
  DRK09: {
    name: "Dereköy",
    location: "Kırklareli · Bulgaristan sınırı",
    road: "Kırklareli – Dereköy bağlantı yolu",
    cameras: [
      { label: "Yolcu Giriş", url: "https://trakya.iscoz.com/derekoy/turkiyekapi.jpg" },
      { label: "Yolcu Çıkış", url: "https://trakya.iscoz.com/derekoy/hudutkapi.jpg" },
    ],
  },
};

const GATE_ORDER = ["KAP34", "HAM11", "IPS22", "PZK15", "DRK09"];

/* ---------- İkonlar (Sidebar/Header) ---------- */
const SIcon = {
  grid: (p) => <svg viewBox="0 0 24 24" width="18" height="18" {...p}><rect x="3" y="3" width="8" height="8" rx="1.5" fill="none" stroke="currentColor" strokeWidth="1.8"/><rect x="13" y="3" width="8" height="8" rx="1.5" fill="none" stroke="currentColor" strokeWidth="1.8"/><rect x="3" y="13" width="8" height="8" rx="1.5" fill="none" stroke="currentColor" strokeWidth="1.8"/><rect x="13" y="13" width="8" height="8" rx="1.5" fill="none" stroke="currentColor" strokeWidth="1.8"/></svg>,
  truck: (p) => <svg viewBox="0 0 24 24" width="18" height="18" {...p}><path fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" d="M3 7h10v9H3V7ZM13 11h4l3 3v2h-7v-5ZM6.5 19a1.7 1.7 0 1 0 0-3.4 1.7 1.7 0 0 0 0 3.4ZM16.5 19a1.7 1.7 0 1 0 0-3.4 1.7 1.7 0 0 0 0 3.4Z"/></svg>,
  doc: (p) => <svg viewBox="0 0 24 24" width="18" height="18" {...p}><path fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" d="M6 3h9l4 4v14H6zM14 3v5h5"/></svg>,
  pin: (p) => <svg viewBox="0 0 24 24" width="18" height="18" {...p}><path fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" d="M12 21s7-6.1 7-11.5A7 7 0 0 0 5 9.5C5 14.9 12 21 12 21Z"/><circle cx="12" cy="9.5" r="2.3" fill="none" stroke="currentColor" strokeWidth="1.8"/></svg>,
  route: (p) => <svg viewBox="0 0 24 24" width="18" height="18" {...p}><circle cx="6" cy="6" r="2.3" fill="none" stroke="currentColor" strokeWidth="1.8"/><circle cx="18" cy="18" r="2.3" fill="none" stroke="currentColor" strokeWidth="1.8"/><path fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" d="M8 6h6a4 4 0 0 1 4 4v4"/></svg>,
  chart: (p) => <svg viewBox="0 0 24 24" width="18" height="18" {...p}><path fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" d="M4 20V10M10 20V4M16 20v-7M4 20h16"/></svg>,
  settings: (p) => <svg viewBox="0 0 24 24" width="18" height="18" {...p}><circle cx="12" cy="12" r="3" fill="none" stroke="currentColor" strokeWidth="1.8"/><path fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" d="M19.4 13.5a1.7 1.7 0 0 0 .3 1.9l.1.1a2 2 0 1 1-2.9 2.9l-.1-.1a1.7 1.7 0 0 0-1.9-.3 1.7 1.7 0 0 0-1 1.6v.2a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1.1-1.6 1.7 1.7 0 0 0-1.9.3l-.1.1a2 2 0 1 1-2.9-2.9l.1-.1a1.7 1.7 0 0 0 .3-1.9 1.7 1.7 0 0 0-1.6-1H4a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.6-1 1.7 1.7 0 0 0-.3-1.9l-.1-.1a2 2 0 1 1 2.9-2.9l.1.1a1.7 1.7 0 0 0 1.9.3H10a1.7 1.7 0 0 0 1-1.6V4a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.6 1.7 1.7 0 0 0 1.9-.3l.1-.1a2 2 0 1 1 2.9 2.9l-.1.1a1.7 1.7 0 0 0-.3 1.9V10a1.7 1.7 0 0 0 1.6 1h.2a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.6 1Z"/></svg>,
  bell: (p) => <svg viewBox="0 0 24 24" width="17" height="17" {...p}><path fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" d="M6 9a6 6 0 1 1 12 0c0 4 1.5 5.5 1.5 5.5H4.5S6 13 6 9Z"/><path fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" d="M10 18.5a2 2 0 0 0 4 0"/></svg>,
  panel: (p) => <svg viewBox="0 0 24 24" width="17" height="17" {...p}><rect x="3" y="4" width="18" height="16" rx="2" fill="none" stroke="currentColor" strokeWidth="1.6"/><line x1="9" y1="4" x2="9" y2="20" stroke="currentColor" strokeWidth="1.6"/></svg>,
};

/* ---------- Sidebar ---------- */
function Sidebar({ open, onClose }) {
  return (
    <>
      {open && <div className="skti-sidebar-overlay" onClick={onClose} />}
      <aside className={`skti-sidebar ${open ? "open" : ""}`}>
        <div className="skti-sidebar-logo">
          <span className="skti-logo-mark">T</span>
          <div>
            <div className="skti-logo-text">Takobil</div>
            <div className="skti-logo-sub">Lojistik Platformu</div>
          </div>
        </div>

        <div className="skti-nav-group-label">Yönetim</div>
        <nav className="skti-nav">
          <div className="skti-nav-item"><SIcon.grid /><span>Genel Bakış</span></div>
          <div className="skti-nav-item"><SIcon.truck /><span>Seferler</span></div>
          <div className="skti-nav-item"><SIcon.doc /><span>Belgeler</span></div>
        </nav>

        <div className="skti-nav-group-label">Operasyon</div>
        <nav className="skti-nav">
          <div className="skti-nav-item active"><SIcon.pin /><span>Sınır Kapıları Yoğunluk</span></div>
          <div className="skti-nav-item"><SIcon.route /><span>Rota Planlama</span></div>
          <div className="skti-nav-item"><SIcon.chart /><span>Raporlar</span></div>
        </nav>

        <div className="skti-nav-group-label">Sistem</div>
        <nav className="skti-nav">
          <div className="skti-nav-item"><SIcon.settings /><span>Ayarlar</span></div>
        </nav>
      </aside>
    </>
  );
}

/* ---------- Header ---------- */
function TopHeader({ onToggleSidebar }) {
  return (
    <div className="skti-topheader">
      <button className="skti-panel-toggle" type="button" aria-label="Kenar çubuğunu aç/kapat" onClick={onToggleSidebar}>
        <SIcon.panel />
      </button>
      <div className="skti-topheader-right">
        <SIcon.bell className="skti-bell" />
        <div className="skti-avatar">HE</div>
        <div className="skti-user-text">
          <div className="skti-user-name">Hayrunnisa Eren</div>
          <div className="skti-user-role">Operasyon</div>
        </div>
      </div>
    </div>
  );
}

/* ---------- Yardımcılar ---------- */
function statusFor(minutes) {
  if (minutes <= 30) return { key: "low", label: "Düşük" };
  if (minutes <= 90) return { key: "medium", label: "Orta" };
  if (minutes <= 240) return { key: "high", label: "Yüksek" };
  return { key: "critical", label: "Kritik" };
}

function timeAgo(date) {
  const seconds = Math.floor((Date.now() - date) / 1000);
  if (seconds < 10) return "az önce";
  if (seconds < 60) return `${seconds} sn önce`;
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes} dk önce`;
  return `${Math.floor(minutes / 60)} sa önce`;
}

function formatDuration(totalMinutes) {
  const h = Math.floor(totalMinutes / 60);
  const m = totalMinutes % 60;
  if (h === 0) return `${m} dk`;
  return `${h} sa ${m} dk`;
}

function barPercent(minutes) {
  return Math.min(100, Math.round((minutes / 240) * 100));
}

/* ---------- Kamera görüntüsü (gerçek görüntü denenir; Cloudflare koruması
   nedeniyle başarısız olursa zarifçe "alınamıyor" mesajı gösterilir) ---------- */
function LiveCameraImage({ url, alt }) {
  const [failed, setFailed] = useState(false);
  useEffect(() => setFailed(false), [url]);

  if (failed) {
    return (
      <div className="camera-error">
        <span className="camera-error-icon">📷</span>
        <span>Canlı kamera görüntüsü şu anda alınamıyor</span>
      </div>
    );
  }
  return (
    <img
      key={url}
      src={url}
      alt={alt}
      className="camera-image"
      onError={() => setFailed(true)}
    />
  );
}

/* ---------- Kapı Kartı ---------- */
function GateCard({ gateId, gate, onOpen }) {
  const meta = GATES_META[gateId];
  const isUnavailable = gate.source === "Unavailable";
  const status = statusFor(gate.estimatedWaitMinutes);
  const isLive = gate.confidenceLevel === "NearRealTime";

  return (
    <div className="gate-card" onClick={() => onOpen(gateId)}>
      <div className="gate-card-top">
        <div>
          <h3>{meta.name}</h3>
          <span className="gate-location">{meta.location}</span>
        </div>
        {isUnavailable ? (
          <span className="level-badge level-unavailable">Veri Yok</span>
        ) : (
          <span className={`level-badge level-${status.key}`}>{status.label}</span>
        )}
      </div>

      {isUnavailable ? (
        <div className="unavailable-block">TomTom'dan şu an veri alınamıyor</div>
      ) : (
        <>
          <div className="gate-bar-track">
            <div className={`gate-bar-fill level-${status.key}`} style={{ width: `${barPercent(gate.estimatedWaitMinutes)}%` }} />
          </div>

          <div className="gate-stats-row">
            <div>
              <span className="stat-label">Bekleyen araç</span>
              <span className="stat-value">{gate.waitingVehicleCount}</span>
            </div>
            <div>
              <span className="stat-label">Tahmini bekleme</span>
              <span className="stat-value">{formatDuration(gate.estimatedWaitMinutes)}</span>
            </div>
          </div>
        </>
      )}

      <div className="gate-card-footer">
        <span className="confidence-text">
          {isUnavailable ? "—" : isLive ? "Gerçek zamanlıya yakın" : "Tahmini"}
        </span>
        <span className="updated-text">{timeAgo(new Date(gate.lastUpdated))}</span>
      </div>

      <button className="detail-link">Detayları görüntüle ›</button>
    </div>
  );
}

/* ---------- Detay Modalı ---------- */
function GateDetailModal({ gateId, gate, onClose }) {
  const meta = GATES_META[gateId];
  const isUnavailable = gate.source === "Unavailable";
  const status = statusFor(gate.estimatedWaitMinutes);
  const isLive = gate.confidenceLevel === "NearRealTime";
  const estimatedKm = +((gate.waitingVehicleCount * 15) / 1000).toFixed(1);
  const hasTomTom = gate.source === "TomTom" && gate.liveTravelTimeSeconds != null;
  const [activeCam, setActiveCam] = useState(0);

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-panel" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <div className="modal-title-row">
            <h2>{meta.name}</h2>
            {isUnavailable ? (
              <span className="level-badge level-unavailable">Veri Yok</span>
            ) : (
              <span className={`level-badge level-${status.key}`}>{status.label}</span>
            )}
          </div>
          <button className="modal-close" onClick={onClose}>✕</button>
        </div>
        <div className="modal-subline">
          {isUnavailable
            ? "TomTom'dan şu an veri alınamıyor — bir sonraki otomatik yenilemede tekrar denenecek."
            : `${isLive ? "Gerçek zamanlıya yakın" : "Tahmini"} · Son güncelleme ${new Date(gate.lastUpdated).toLocaleTimeString("tr-TR", { hour: "2-digit", minute: "2-digit" })} (${timeAgo(new Date(gate.lastUpdated))})`}
        </div>

        {!isUnavailable && (
          <div className="modal-stats-grid">
            <div>
              <span className="stat-label">Bekleyen araç</span>
              <span className="stat-value-lg">{gate.waitingVehicleCount}</span>
            </div>
            <div>
              <span className="stat-label">Tahmini bekleme</span>
              <span className="stat-value-lg">{formatDuration(gate.estimatedWaitMinutes)}</span>
            </div>
            <div>
              <span className="stat-label">Kuyruk mesafesi</span>
              <span className="stat-value-lg">≈ {estimatedKm} km</span>
            </div>
            <div>
              <span className="stat-label">Son güncelleme</span>
              <span className="stat-value-lg stat-value-sm">
                {new Date(gate.lastUpdated).toLocaleTimeString("tr-TR", { hour: "2-digit", minute: "2-digit" })}
              </span>
            </div>
          </div>
        )}

        {/* Kapıya Ulaşım Trafiği */}
        <div className="modal-section">
          <div className="modal-section-header">
            <h4>KAPIYA ULAŞIM TRAFİĞİ</h4>
            {!isUnavailable && <span className={`level-badge level-${status.key}`}>{status.label}</span>}
          </div>
          <div className="modal-section-subline">{meta.road}</div>
          {hasTomTom ? (
            <>
              <div className="traffic-stats-grid">
                <div>
                  <span className="stat-label">Mevcut süre</span>
                  <span className="stat-value">{Math.round(gate.liveTravelTimeSeconds / 60)} dk</span>
                </div>
                <div>
                  <span className="stat-label">Normal süre</span>
                  <span className="stat-value">{Math.round(gate.noTrafficTravelTimeSeconds / 60)} dk</span>
                </div>
                <div>
                  <span className="stat-label">Gecikme</span>
                  <span className="stat-value">+{Math.round(gate.trafficDelaySeconds / 60)} dk</span>
                </div>
              </div>
              <div className="source-tag">TomTom trafik verisi</div>
            </>
          ) : (
            <div className="source-tag">
              {isUnavailable
                ? "TomTom'dan şu an yanıt alınamıyor."
                : "Bu kapı için canlı yol trafiği verisi kullanılamıyor."}
            </div>
          )}
        </div>

        {/* Canlı Kamera */}
        <div className="modal-section">
          <h4>CANLI KAMERA</h4>
          <div className="camera-tabs">
            {meta.cameras.map((cam, i) => (
              <button
                key={cam.url}
                className={`camera-tab ${activeCam === i ? "active" : ""}`}
                onClick={() => setActiveCam(i)}
              >
                {cam.label}
              </button>
            ))}
          </div>
          <div className="camera-frame">
            <LiveCameraImage url={meta.cameras[activeCam].url} alt={`${meta.name} - ${meta.cameras[activeCam].label}`} />
          </div>
          <p className="camera-caption">
            Resmî kamera görüntüsü ham olarak gösterilir; görsel doğrulama amaçlıdır.
          </p>
        </div>
      </div>
    </div>
  );
}

/* ---------- Ana Sayfa ---------- */
export default function SinirKapisiTrafikIzleme() {
  const [gateData, setGateData] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [openGateId, setOpenGateId] = useState(null);
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const fetchAll = useCallback(async () => {
    try {
      const res = await fetch(`${API_BASE_URL}/api/border-gates`);
      if (!res.ok) throw new Error("API yanıt vermedi");
      const data = await res.json();
      const byId = {};
      data.forEach((g) => {
        if (GATE_ORDER.includes(g.gateId)) byId[g.gateId] = g;
      });
      setGateData(byId);
      setError(null);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchAll();
    const id = setInterval(fetchAll, POLL_INTERVAL_MS);
    return () => clearInterval(id);
  }, [fetchAll]);

  const visibleGateIds = useMemo(() => {
    // Not: JavaScript'in varsayılan .toLowerCase() metodu Türkçe "İ" harfini
    // doğru küçük harfe çevirmiyor (Unicode'un genel kuralını kullanıyor,
    // Türkçe'ye özel değil). .toLocaleLowerCase("tr-TR") bunu düzeltir —
    // aksi halde "İpsala" gibi büyük İ ile başlayan kapı adları aramada
    // eksik/hatalı eşleşebiliyordu.
    const term = searchTerm.toLocaleLowerCase("tr-TR").trim();
    return GATE_ORDER.filter((id) => {
      if (!gateData[id]) return false;
      if (!term) return true;
      return GATES_META[id].name.toLocaleLowerCase("tr-TR").includes(term);
    });
  }, [gateData, searchTerm]);

  return (
    <div className="skti-shell">
      <Sidebar open={sidebarOpen} onClose={() => setSidebarOpen(false)} />
      <div className="skti-main">
        <TopHeader onToggleSidebar={() => setSidebarOpen((v) => !v)} />
        <div className="skti-page">
      <div className="skti-header">
        <div>
          <h1 className="skti-main-title">Sınır Kapıları Yoğunluk Durumu</h1>
        </div>
        <div className="skti-search">
          <input
            type="text"
            placeholder="Sınır kapısı ara"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
      </div>

      {loading && <div className="skti-state">Yükleniyor…</div>}
      {!loading && error && (
        <div className="skti-state skti-state-error">
          Veri alınamadı. Backend'in çalıştığından emin olun ({API_BASE_URL}).
        </div>
      )}
      {!loading && !error && visibleGateIds.length === 0 && (
        <div className="skti-state">Aramanıza uyan bir sınır kapısı bulunamadı.</div>
      )}

      {!loading && !error && visibleGateIds.length > 0 && (
        <div className="gate-grid">
          {visibleGateIds.map((id) => (
            <GateCard key={id} gateId={id} gate={gateData[id]} onOpen={setOpenGateId} />
          ))}
        </div>
      )}

      {openGateId && gateData[openGateId] && (
        <GateDetailModal
          gateId={openGateId}
          gate={gateData[openGateId]}
          onClose={() => setOpenGateId(null)}
        />
      )}
        </div>
      </div>
    </div>
  );
}
