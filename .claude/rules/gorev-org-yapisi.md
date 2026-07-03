# Görev Tanımları + Org Yapısı — Canonical Harita (ATLANMASI YASAK)

_Core rule — `paths:` YOK, her oturum context'te. Org / görev-tanımı / OrgChart / OrgPositions / eşleme işine dokunmadan ÖNCE bu dosya + bkm `DECISIONS.md` okunur. Yeniden keşif YASAK — canon burada._

## Neden bu dosya var

2026-07-03: org yapısını tekrar tekrar sıfırdan keşfettim, `org.json`'un canonical olduğunu, DECISIONS G/H'yi bilmeden plan yaptım, kilitli kararla çelişen adım attım. Kullanıcı: _"var olan yapıyı hiçbir zaman atlamayacak hale getir — başka oturum/proje olsa bile bileceğin."_ Bu dosya o önlem.

## İki repo topolojisi (EZBERLE)

| Rol | Yer | Git? |
|---|---|---|
| **KOD** (Mosaik modülü, EF, MVC, DB migration) | `D:\Dev\reporthub` → `Mosaik.Modules.GorevTanimlari` | ✅ git |
| **İÇERİK + KARAR** (org tasarım, görev tanımı metni, kilitli kararlar) | `D:\Dev\gorevtanimlari\bkm` | ❌ git değil |

**Canonical içerik kaynağı:** `bkm/BKM_Gorev_Tanimlari.md` → üretir → `bkm/data/org.json` (normalize ayna) → üretir → Mosaik DB.
**Karar canonu:** `bkm/DECISIONS.md` (A kilitli / G-H-I org modeli), `bkm/HANDOFF.md`, `bkm/.claude/rules/work-protocol.md`.

## Org modeli — kilitli karar (DECISIONS I, 03.07.2026 — G'yi supersede eder)

- **`OrgPositions` = BİZİM tasarım = ANA OMURGA** (org.json: 53 ağaç düğümü + 4 bağımsız danışman = **57 pozisyon**, GM + 9 müdürlük). Zirve aynası DEĞİL.
- **Zirve personeli omurgaya EŞLENİR** (maps-in), omurga Zirve'den türetilmez. Eşleme = `GorevPersonelMap` (Personelno kişi-düzeyi, `/GorevTanimlari/Mapping` ekranı) + `HolderName` (org.json tasarım holder).
- Eşlenmeyen Zirve frontline (~220 kişi: satış danışmanı/kasiyer/garson…) = **gap** (görev tanımı yazılacak roller, T-08/T-09).
- DECISIONS **G eski** ("OrgPositions = Zirve aynası, Direktör-modeli") → **I ile geçersiz.** Eski 60 Zirve-türevi satır `IsActive=0` (silinmedi, `*_bak_plan55` yedek).

## Tablolar (Mosaik DB, BT-FIKRI\SQLEXPRESS)

| Tablo | İçerik | Not |
|---|---|---|
| `OrgPositions` | Org omurgası (57 aktif) | +`HolderName`/`HolderPersonelno`/`ZirveMatchKey` (Plan 55 migration 81) |
| `GorevDocuments` (48) | Pozisyon başına 1 görev-tanımı doc | `OrgPositionId` → omurga (47 bağlı; "Şube Müdürü" generic rol bağsız) |
| `GorevVersions` (48) | Versiyonlu içerik (SopVersions deseni, Rev1 yayın) | ContentJson {paragraphs, kpi} |
| `GorevZirveMap` (34) | Rol-düzeyi öneri (Departman+Unvan) | |
| `GorevPersonelMap` | Kişi-düzeyi eşleme (Personelno UNIQUE) | override; rol-map fallback |

- **Personelno = STRING** ("4634-BKM"), int değil.
- **IK datasource** (Zirve `vw_PersonelDepartman`, aktif = `Ict IS NULL` = 271 kişi): connstring **port'lu** `192.168.40.25,64507` (named-instance error 26 fix). Kadro otoritesi = Zirve `perbilgi.Gorevi` serbest metin.

## Çift-ünvan (Code UNIQUE) kuralı

org.json 5 çift-ünvan (GMY/Kampanya Uzman Yrd/Sosyal Medya/İK Uzmanı/Ön Muhasebe ×2). `OrgPositions.Code` = ünvan + " (Kişi)" suffix; `Title` temiz; `ZirveMatchKey` temiz ünvan. GorevDocuments GMY zaten kişi-suffix'li.

## DOKUNMADAN ÖNCE (zorunlu sıra)

1. **Bu dosyayı oku** + `bkm/DECISIONS.md` (A kilitli kararlar + G/H/I org modeli).
2. **work-protocol** (bkm): danış → yap → kontrol ettir → smoke. Org tasarım kararı → `bkm-gorev-danismani` (o **tasarım** danışmanı; depolama/mimari DEĞİL — onu DECISIONS + reporthub advisor verir).
3. Canlı DB state için sqlcli: `cd /d/Dev/sqlcli && dotnet run -- query --conn "Server=BT-FIKRI\SQLEXPRESS;Database=Mosaik;Integrated Security=true;TrustServerCertificate=true;" "..."`. Migration dosyası: `dotnet run -- script <file> --conn "..."` (GO/temp tek connection).
4. **Yeniden keşif YASAK** — canon burada + bkm'de. Varsayım yapma, oku.

## Plan 55 durumu (org.json → OrgPositions omurga)

- ✅ Faz 1 (OrgPositions +3 kolon) · ✅ Faz 2 (match ZirveMatchKey) · ✅ Faz 3 (bkm `migrate-to-mosaik.mjs` → `03-orgpositions.sql`, 57 düğüm) · ✅ Faz 4 (uygula: 57 aktif omurga, 47/48 doc bağlı, backup) · ⏳ Faz 5 (render: `/OrgChart` HolderName + node→görev tanımı; incumbent HolderName+GorevPersonelMap'e pivot) · Faz 6 kapanış.
- Plan: [`plans/55-bkm-org-structure-import.md`](../../plans/55-bkm-org-structure-import.md).

## Cross-project (başka oturum/proje için)

- **Semantik katman:** `D:\Dev\pusula\sema\entities.yaml` — BKM_GENEL + Mosaik entity'leri (perbilgi/vw_PersonelDepartman/OrgPositions/GorevDocuments + görev eşleme köprüsü). Şema gerçeği değişince oradan güncelle.
- **Brain vault:** `D:/Dev/brain` cross-project sentez. Org backbone kararı buraya push edilir (wiki-keeper).
- Başka projede görev/org işi çıkarsa: önce `bkm/DECISIONS.md` + pusula sema oku.
