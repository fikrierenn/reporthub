# Plan 04 — V2 Builder UX Redesign (Pratik & Kullanıcı Dostu)

**Tarih:** 2026-04-29
**Yazan:** Fikri direktif + Claude (web araştırması + dosyaya geçirildi)
**Durum:** Onaylandı (29 Nisan 2026, "yap bakalım her şeyin en iyisini al")
**Branch:** `feature/m-11-dashboard-builder-redesign`
**İlişkili:** Plan 03 (V2 stand-alone) tamamlandı

---

## Özet

V2 builder mevcut 3-pane (palette + canvas + drawer) + drawer 3-tab + multi-list palette yapısı kullanıcı için karmaşık. Endüstri standardı 2-pane (canvas + context properties) layout'una geçiş. Looker Studio + Power BI + Metabase + Superset best practice'lerinden esinlenir.

**Kabul kriteri:** Yeni admin sıfırdan rapor oluşturduğunda 5 tıkta dashboard yapabilir: (1) DataSource seç, (2) SP seç, (3) "+ KPI" tıkla, (4) Setup tab'da kolon seç, (5) Kaydet.

**Bağlam:** Kullanıcı kararı (29 Nisan oturum 3): "hala çok karmaşık, kullanıcı dostu değil — internette araştır, esinlen, pratik bir ekran yap".

---

## Araştırma Özeti

| Tool | Layout | Properties |
|---|---|---|
| **Looker Studio** | 2-pane: canvas + sağ properties | Setup (veri) / Style 2 tab |
| **Power BI** | Multi-pane: canvas + Visualizations + Fields + Properties | Context-aware Visualizations panel |
| **Metabase** | Card-grid canvas + sağ drawer (sadece selected) | Drag-drop card move/resize |
| **Superset** | Sol Dataset + sağ Customize | Metric/dimension drag-drop |

**Ortak prensipler (2026):**
- Maks 7-8 görünür element
- Context-aware properties (selected widget'a göre değişir)
- Drag-drop ana etkileşim
- Setup vs Style ayrımı
- Boş canvas state'i için onboarding CTA

---

## Hedef Layout

```
┌──────────────────────────────────────────────────────────┐
│ Topbar: BKM > [Rapor Adı] | Düzenle/Önizle | Tam Önizle | JSON | Kaydet │
├──────────────────────────────────────────────────────────┤
│ Toolbar: [+KPI] [+Grafik] [+Tablo]  Veri Setleri: ⭐7×254s │
├──────────────────────────────────────────────────────────┤
│ Sekme strip: Özet · Vardiya · Mesai · [+Yeni]            │
├──────────────────────────────────────────────────────────┤
│ Param chip'leri: 📅 Tarih: 29.04.2026 [Çalıştır]         │
├──────────────────────────────────────┬───────────────────┤
│                                      │ ▼ Setup ▼ Stil    │
│                                      │                   │
│      CANVAS (büyük, full width-300px)│  Setup:           │
│                                      │   Başlık ____     │
│      Boş ise:                        │   Veri Seti ▼     │
│      "İlk widget'ı ekleyin"          │   Kolon ▼ □○△     │
│      [+KPI büyük] [+Grafik] [+Tablo] │   Hesap sum ▼     │
│                                      │   İkon ▼          │
│                                      │                   │
│                                      │  Stil:            │
│                                      │   Genişlik 1/2/3  │
│                                      │   Renk            │
│                                      │   Format          │
└──────────────────────────────────────┴───────────────────┘
```

---

## Key Changes

### 1. Sol Palette → Topbar Toolbar
- `aside.palette` (3 liste: Bileşen Ekle / Veri Setleri / Bileşenler) **kaldırılır**
- Topbar altında ince toolbar:
  - `[+KPI] [+Grafik] [+Tablo]` butonları (drag-drop kalır)
  - Sağda: "Veri Setleri" chip'i (tıklanınca data modal açılır — mevcut)

### 2. Sağ Drawer → Properties Panel (2 tab)
- Drawer 3 tab (Veri / Görünüm / Ayarlar) → 2 tab (**Setup** / **Stil**)
- **Hiçbir widget seçili değil** → Properties = Rapor Ayarları (DataSource/SP/ParamSchema, mevcut Ayarlar tab içeriği bu duruma taşınır)
- **Widget seçili** → Setup + Stil
  - **Setup**: Başlık, Veri Seti dropdown, Kolon picker (tip rozeti: 🔢 sayı / 🔤 metin / 📅 tarih), Hesap (sum/avg/count/first), İkon picker
  - **Stil**: Genişlik (span 1-4), Renk, Format (auto/yüzde/para), Alt yazı

### 3. Boş Canvas Empty State
- `components.length === 0` durumunda canvas ortasında büyük CTA:
  - "Bu sekmede henüz bileşen yok"
  - 3 büyük kart: [+ KPI] [+ Grafik] [+ Tablo] (icon + label)
  - Tıklayınca direkt eklenir + properties Setup açılır

### 4. Veri Setleri Topbar Chip'i
- Topbar toolbar sağında: "Veri Setleri: 7 set · 254 satır" chip
- Tıklayınca mevcut data modal açılır (RS list)
- Veri modalı zaten var (Plan 03'te kuruldu), sadece tetikleyici yer değişir

### 5. Kolon Picker (Setup Tab)
- Dropdown / custom select: `<select>` veya headless dropdown
- Her option: `[tip-rozeti] Kolon Adı (örnek: 254)` formatında
- Looker Studio metric (mavi) / dimension (yeşil) renk ayrımı yerine: sayı (🔵) / tarih (🟡) / metin (🟢)
- Search input (kolon çoksa filtrele)

### 6. Korunanlar
- DashboardConfigJson schema (tabs/components/resultContract)
- Tüm backend (RunJsonV2, RunJsonV2Preview, PreviewDashboardV2, DashboardConfigValidator)
- Multi-tab strip + tab switch (Plan 03)
- Tam Önizle iframe (Plan 03)
- Validator save banner (Plan 03)
- Param-bar (Plan 02 alt-2V)
- Mode toggle (Düzenle/Önizle)
- Save dirty chip + beforeunload guard

---

## Atlanan / Ertelenen

- **AI suggestion** — modern trend (Looker, weweb), ama V2 stand-alone hedefi pratiklik; ileri faz
- **Drag-drop kolon** (Superset/Power BI gibi sürükle bırak veri-bağlama) — şimdilik dropdown picker yeterli
- **Slash command empty state** (Notion-style) — büyük CTA daha sezgisel
- **Calculated field full AST** (Plan 02 #9) — formula 2-kolon MVP korunur

---

## Uygulama Sırası

1. **CSS rewrite** — `builder-v2.css` 2-pane layout (palette kaldır, drawer → properties, canvas büyür, empty state stilleri)
2. **View shell** — `EditReportV2.cshtml` + `CreateReportV2.cshtml`:
   - aside.palette **silinir**
   - Topbar toolbar (KPI/Grafik/Tablo + Veri Setleri chip)
   - Drawer içeriği baştan yazılır: 2 tab (Setup/Stil) + context-aware (selected'a göre)
   - Empty canvas CTA bloğu
3. **Properties panel context-aware** — drawerMixin / settingsMixin'de:
   - `properties = !selected ? 'report' : 'widget'` getter
   - selected ise drawer 2-tab (Setup/Stil), değilse Rapor Ayarları
4. **Kolon picker** — Setup tab'da dropdown + tip rozeti + arama
5. **Smoke test** — Edit/Create iki yol için 4-5 tıklamada widget ekleyip bağla testi
6. **Commit** — bundled `feat(m-11): V2 builder UX redesign — 2-pane (plan: 04)`

---

## Test Planı

- `dotnet build reporthub.sln --nologo`
- JS parse check
- Browser smoke (her view edit sonrası TagHelper deja vu kontrolü zorunlu):
  - `/Admin/EditReportV2/13` — mevcut rapor 7 widget + 3 sekme görünür mü
  - `/Admin/CreateReportV2` — boş canvas CTA görünür mü, KPI ekle → Setup açılır mı
  - Setup tab kolon picker dropdown çalışır
  - Mode toggle (Düzenle/Önizle) bozulmadı
  - Tam Önizle iframe açılır
  - Multi-tab switch korunur

---

## Done Kriterleri

- 3-pane → 2-pane geçişi tamamlandı
- Drawer 3-tab → Setup/Stil 2-tab + context-aware (rapor ayarları | widget setup)
- Empty canvas CTA çalışır
- Kolon picker dropdown çalışır
- Mevcut tüm Plan 02-03 davranışları korunur (multi-tab, Tam Önizle, validator, param-bar, save dirty)
- Build + JS parse yeşil
- Browser smoke geçer
- Plan 04 archive'a taşınır
