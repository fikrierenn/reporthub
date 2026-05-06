## Plan 15 (TASLAK NOTLARI) — vNext + Filter Mimari Genişletme

**Tarih:** 2026-05-07 gece — sabah konuşma için brief
**Yazan:** Claude (Fikri'nin gece düşüncelerinden)
**Durum:** TARTIŞMA NOTU — plan değil, henüz düşünce
**İlişkili:** Plan 14 (filter prod-readiness), TODO.md vNext satırı

---

### Üç soru

Kullanıcı gece bıraktı (özet):

1. **Filter mimarisi diğer modüllerde de kullanılacak** — daha esnek/kullanışlı hale getirilmeli mi?
2. **User + UserFilter tüm modüllerde aktif olmalı** — şirket içi portal olacağı için yeni işe girenler **otomatik onboarding**.
3. **Filter modül bazlı tanımlanmalı** ya da modüle bağlanabilmeli.

---

### vNext modül listesi (CLAUDE.md + TODO satırı)

| Modül | Filter ihtiyacı (örnek) |
|---|---|
| Tamim/sirküler | Departman, Rol, Şube |
| Duyurular | Departman, Şube, Tümü |
| Departman dizini | Şube, Departman |
| Doküman paylaşımı | Klasör, Departman, Proje |
| Mesajlaşma | Grup, Kişisel |
| Form/anket | Departman, Rol |
| SOP | Departman, Süreç |
| Takvim | Takım, Kişisel |
| KPI/OKR | Takım, Sahip, Departman |
| Onay akışları | Başlatan, Onay zinciri |

**Ortak filtre boyutları (cross-module):**
- Departman / Şube / Birim
- Rol / Grup
- Kullanıcı / Sahip
- Proje / Etiket / Kategori

Bu pattern → "Filtre bir kez tanımlanır, birden fazla modülden referans verilir."

---

### Mevcut filter mimarisi sınırları

`FilterDefinition` bugünkü hali:
- `FilterKey` (sube, urunKategori, raporGrubu)
- `Scope` (`spInjection`, `reportAccess`)
- `DataSourceKey` (PDKS / DER / IK / NULL)
- `OptionsQuery` (admin'in yazdığı SELECT)

**Bağlama (binding) sadece DataSourceKey** üzerinden. Modül kavramı yok. vNext'e doğrudan uygulanamaz çünkü modül entity'leri DataSource bazlı değil, EF Core / domain bazlı.

---

### 4 mimari alternatif — düşünce için

#### A. **ModuleKey alanı ekle**
- `FilterDefinition.ModuleKey nvarchar(50) NULL`
- Mevcut DataSourceKey ile coexist eder
- Tipik kayıtlar:
  - `(DataSourceKey=PDKS, FilterKey=sube)` — rapor SP filtresi (mevcut)
  - `(ModuleKey=documents, FilterKey=department)` — doküman modülü filtresi (yeni)
  - `(ModuleKey=NULL, DataSourceKey=NULL, FilterKey=raporGrubu)` — cross-cutting (mevcut)
- En az invaziv, geriye uyumlu
- **Risk:** scope sorgusu (DataSourceKey null ise + ModuleKey null ise + match...) karmaşıklaşır

#### B. **`BindingScopes` çoğul tablosu**
- `FilterDefinitionBinding (FilterDefinitionId, BindingType, BindingKey)` — 1-N
- BindingType: `datasource` | `module` | `report`
- Tek FilterDefinition birden fazla yere bağlanabilir
- **Avantaj:** "departman" filtresi hem rapor SP'leri hem doküman modülü hem duyuru modülü için tek tanım, çoklu binding
- **Risk:** her modülün enforcement noktası bu binding tablosunu sorgulamalı, complexity artar

#### C. **Module-owned filter** (tamamen ayrı sistem)
- Her modül kendi filter tablolarını yönetir (DocumentFilter, AnnouncementFilter…)
- Ortak `IFilterEnforcer<T>` interface
- Reuse YOK — her modül baştan yazar
- **Avantaj:** modül izolasyonu maksimum
- **Risk:** kod duplikasyonu, admin GUI fragmente, audit log dağınık

#### D. **Generic FilterDimension registry + scope adapter**
- `FilterDimension` — boyut tanımı (departman, şube, kullanıcı, proje)
- `FilterDimensionScope` — boyut'un uygulandığı yer (rapor SP, modül, entity)
- Her scope için bir adapter (örn. `IModuleFilterAdapter`, `ISpInjectionAdapter`)
- **Avantaj:** maksimum genelleme, plug-in mimari
- **Risk:** over-engineering — solo dev için ROI düşük

---

### 5 Lens (sabah karar için)

- 🔴 **Contrarian:** D over-engineered. Tek geliştirici + 5 modül için A yeterli. C kötü çünkü DRY ihlali.
- 🔵 **First Principles:** Filter = "kullanıcı X boyutunda Y kapsamında veri görsün". Boyut (departman) modül-bağımsız, kapsam (hangi 5 departman) kullanıcı-bazlı. B yapısal olarak doğru, A pragmatik.
- 🟢 **Expansionist:** İleride external sistem (Tableau, PowerBI) entegrasyonu olursa filter mantığı API olarak dışa açılır → B'nin binding tablosu API yüzeyi temizler.
- ⚪ **Outsider:** Yeni geliştirici sadece "FilterDefinition" tablosunu görüp anlamalı. C olursa "10 ayrı filter tablosu var, hangisi nerede?" → kötü.
- 🟡 **Executor:** A, en kısa yolda 1 kolon migration + 1 enforcement noktası ekleme. B 1 hafta. C 1 ay. D 2 ay. **Karar muhtemelen A → ileride B'ye evrim.**

---

### HR sync (otomatik kullanıcı onboarding)

**Kaynak adayları:**
- `BKM_GENEL` DB (IK DataSource) — `iky.personel` tablosu DerinSISBkm'de zaten görüldü (7 May)
- Aktif personel listesi: muhtemelen `iky.personel WHERE [aktif=1]`

**Tetik mekanizmaları:**
1. **Scheduled job** (günlük 06:00) — basit, en az invaziv
2. **HR write hook** — IK sistemi WebHook'u portal'a haber verir (HR sisteminin kapasitesine bağlı)
3. **Login-time provisioning** — AD login + portal'da yoksa oluştur (lazy)

**Onboard akışı:**
1. IK DB'den aktif personel çek (TC, ad, soyad, departman, şube, pozisyon, telefon, email, AD username)
2. Portal `Users` tablosunda Username (= AD username) yoksa INSERT (PasswordHash boş + IsAdUser=1)
3. Var ise UPDATE (departman, şube, pozisyon değişmişse)
4. Pasif olmuşsa → User.IsActive=0
5. **Yeni user için filter backfill:** her aktif FilterDefinition için `*` veya departman-bazlı default
6. Audit log: `user_provisioned_from_hr` event

**Bağlantı plan'lar:**
- TODO **User P1 · Phone/Dept/Position alanları** (FAZ 2 madde 24) → bu plan'ın ön koşulu
- Plan 13 (G-09 read-only login) → HR sync da read-only login kullanmalı
- Plan 14 (filter prod-ready) → filter backfill mantığı buradan beslenir

---

### Kritik bağımlılıklar / sıralama

```
Plan 14 (Filter prod-ready) — Faz A audit gap kapatılır
        ↓
Plan 12 closure (Brand+Modules smoke) — Module switch sistemi doğrulanır
        ↓
User P1 (Phone/Dept/Position alanları) — User entity genişletilir
        ↓
Plan 15 (vNext + filter genişletme) — ModuleKey ekle (alternatif A)
        ↓
HR sync (otomatik onboarding) — Plan 16
        ↓
vNext modüller (modül-modül, Plan 17, 18, 19...)
```

---

### Sabah konuşma için 4 soru

1. **A/B/C/D'den hangisi?** — Önerim: **A şimdi, B'ye evrim ileride** (incremental).
2. **HR sync ne zaman?** — Plan 14 + Plan 12 closure sonrası mı? Yoksa paralel?
3. **Hangi vNext modül ilk?** — kullanıcı ihtiyacına göre (en çok talep gelen?). Önerim: Duyurular (en basit, hızlı win), sonra Doküman.
4. **HR DB ile authoritative entegrasyon var mı?** — IK sistemi WebHook destekliyor mu, yoksa scheduled poll mu?

---

### NOT — Bu dosya plan değil

Bu sadece tartışma notu. Sabah karar verilince:
- A seçilirse → Plan 15 olarak formalize et
- B seçilirse → Plan 15 daha büyük scope, 2 plan'a böl
- HR sync → Plan 16 ayrı plan
- vNext modüller → Plan 17+ her modül kendi plan
