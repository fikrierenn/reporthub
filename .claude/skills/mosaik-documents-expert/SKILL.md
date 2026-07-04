---
name: mosaik-documents-expert
description: Mosaik Documents modülü uzmanı — dosya upload/download/versiyon/izin/AI-insight. Documents işine (ContractFile, DocumentVersion, DocumentPermission, upload/download, versiyon geçmişi, doküman AI) dokunulurken tetiklenir. Modülün TAM mimarisi (entity/servis/controller file:line) + konvansiyonlar + bilinen gap'lerin insertion noktaları. Yeni Documents kodu yazmadan önce oku.
---

# Mosaik Documents — Uzman

_Kaynak: kılcal dalış 2026-07-05 (Plan 56 M-B Faz 0). Cross-cutting: `mosaik-security` (upload/path-guard) + `mosaik-csharp-razor`._

## Kritik kimlik gerçeği
**"Document" entity YOK** — dosya varlığı = **`ContractFile`** (`Mosaik/Models/ContractFile.cs:9` yorum: "Document" adı Plan 19 ile çakışıyordu, tarihsel). Documents modülü **ana projede** (ayrı csproj değil; ADR-015 çıkarma sırası Eyl 2026).

## Entity + şema
- **ContractFile** (`ContractFile.cs:11-64`, BaseEntity): FirmaId(BindNever) / ContractId? / ObligationId? / FileName / FilePath(ContentRoot-relative) / FileSize / MimeType / **Version(int)** / **ContentText?**(PDF metni, LIKE-search, mig 69) / AiSummary?+AiTagsJson?+AiClassifiedAt?(mig 54). Nav: AiExtractions, DocumentVersions.
- **DocumentVersion** (`DocumentVersion.cs:9-31`, mig 69): ContractFileId FK **ON DELETE CASCADE** / VersionNumber / FilePath / ArchivedAt / ArchivedById. Eski versiyon snapshot; fiziksel dosya diskte kalır.
- **DocumentPermission** (`DocumentPermission.cs:9-31`, mig 74): SubjectType(user|role) / SubjectId / ContractFileId? / **FolderId?(karşılığı olan Folder entity YOK — dangling)** / Level(1 Oku|2 Yaz|3 Yönet) / ValidUntil?.
- DbSet: `MosaikContext.cs:63-65`.

## Akışlar (DocumentsController.cs:12-414, [Authorize] class-level)
- **Index:48-82** — FirmaIds multi-tenant, AsNoTracking, q → FileName|ContentText Contains (FTS kurulu değil, LIKE fallback :67), Take(200).
- **Upload:86-260** — 50MB + magic-byte (PDF %PDF / Office PK, :113-121) + path-traversal guard (:129-137) → `App_Data/contracts/{firmaId}/{guid}{ext}`. `replaceFileId` → **versiyon dalı** (:145-219: eski snapshot→DocumentVersion, Version+=1, AI alanları sıfırlanır, `doc_version_upload` audit). PDF → AiPipelineQueue + fire-and-forget `RunDocumentInsightAsync` (:348-412: manuel `IServiceScopeFactory.CreateScope()` + `_lifetime.ApplicationStopping` — controller-scope dispose çözümü; Task.Run içine DbContext enjekte ETME).
- **Download:300-343** — firma guard + `allowedRoot` prefix guard (:313-320) + `doc_download` audit + PhysicalFile.
- **Delete:264-294** — AiExtractions+AiSuggestions önce, disk try/catch (LogWarning non-blocking), DB remove.
- **AI:** `DocumentInsightService` (upload-sonrası özet+tag, 8K cap) · `DocumentChatService` (tek-doküman chat, RAG DEĞİL düz context 30K, `AiController.CreateContract.cs:22-71`).

## Konvansiyon (taklit et)
Path guard `GetFullPath`+`StartsWith(root, OrdinalIgnoreCase)` her disk erişiminde · magic-byte zorunlu (MIME'a güvenme) · audit her aksiyonda (`doc_*`) · fire-and-forget = manuel scope + ApplicationStopping · UI ui-patterns tam uyumlu (hero+filter-bar+.dt+pill/mono/ago+dashed empty).

## GAP'ler (Plan 56 M-B — 2026-07-05 kapananlar işaretli)
1. **Versiyon geçmişi UI ✅ KAPANDI:** `GET Documents/Versions/{id}` (liste + arşivleyen adları) + `DownloadVersion?versionId=` (DocumentVersion.FilePath — firma+izin+allowedRoot guard+`doc_version_download` audit) + Views/Documents/Versions.cshtml + Index'te Geçmiş linki (Version>1). E2E: v1→v2 replace→liste→eski indirme 200+%PDF.
2. **DocumentPermission ✅ ENFORCE + ADMIN UI (kullanıcı kararı):** Documents Index (batch `FilterReadableIdsAsync`, gizlenince Warning banner) / Download / Delete / Upload-replace + **C-1 `ContractsController.Download`** + **C-2 `AiController.AskDocument`** gate'li (AYNI ContractFile'ı servis eden HER endpoint gate almalı — security CRITICAL dersi; yeni endpoint eklerken UNUTMA). Admin bypass controller'da (`User.IsInRole("admin")` — servis rol bilmez). Grant/Revoke: `Documents/Permissions/{id}` admin-only UI (subject existence H-1 + dup-guard + IDOR firma-guard). Semantik: **kural yok=serbest, kural var=eşleşme şart**; `FilterReadableIdsAsync` DbException'da bilerek 500 — **fail-open fallback EKLEME**. E2E: yonetim-only kural → ik gizli+Forbid, kuralsız dosya serbest.
3. **FolderId dangling (AÇIK):** DocumentPermission.FolderId var, Folder entity YOK — izin sistemi artık canlı; Folder modeli gelirse bağlanır, gelmezse alan sil (backlog).
4. **Delete orphan versiyon dosyaları ✅ KAPANDI:** Delete versiyon FilePath'lerini de diskten siler (best-effort IOException/UnauthorizedAccess catch). E2E: delete sonrası disk 0.
5. **Kod tekrarı (AÇIK backlog):** ContractsController.Files ≈ DocumentsController upload/download kopyası; +şimdi permission-gate de 3 yerde kopya → ortak `IContractFileAccess` choke-point refactor adayı (kullanıcı onayı şart). `Ai/Detail` extraction RawText derinliği de gate değerlendirmesi bekliyor (backlog).
- **Legacy data notu:** Plan 33 öncesi ContractFiles satırları `uploads/...` FilePath'li (App_Data değil) → Download path-guard 404 verir (doğru davranış); fiziksel dosyaları da yok (stale dev kayıtları id 5-6).

## Cross-modül
Contracts = birincil tüketici (aynı ContractFile tablosu, paralel implementasyon). KVKK bağı yok. RAG: Documents henüz RAG'a bağlı DEĞİL — Plan 27 Faz E öncesi **Plan 44 chunk-permission ZORUNLU ön-koşul** (plans/44:255).

## İlişkili
`mosaik-security` (upload 10 kural) · Plan 56 M-B · Plan 27 (Documents roadmap) · Plan 44 (RAG guard) · `feedback_yedek_almadan_silme_yok` (silme kararlarında).
