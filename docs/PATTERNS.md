# Real-World Patterns

Bu dosya gerçek projelerde yaşanan durumları ve çözümlerini anlatır. Her örnek bir anti-pattern + çözüm çifti.

- [P-1: 65 dosya uncommitted, hepsi iç içe](#p-1-65-dosya-uncommitted-hepsi-iç-içe)
- [P-2: Pre-commit hook eski koddaki ihlali yakalıyor, commit-split blok](#p-2-pre-commit-hook-eski-koddaki-ihlali-yakalıyor-commit-split-blok)
- [P-3: Hook çıktısı context'te var, "atla" varsayımı](#p-3-hook-çıktısı-contextte-var-atla-varsayımı)
- [P-4: Kullanıcı kural söyledi, Claude aklında tuttu](#p-4-kullanıcı-kural-söyledi-claude-aklında-tuttu)
- [P-5: 1700+ satır tek controller](#p-5-1700-satır-tek-controller)
- [P-6: Hardcoded şifre git history'e kaçtı](#p-6-hardcoded-şifre-git-historye-kaçtı)
- [P-7: Deprecated dosya "DEPRECATED" banner ile kalmış](#p-7-deprecated-dosya-deprecated-banner-ile-kalmış)
- [P-8: Session başında yanlış cevap (2 kez)](#p-8-session-başında-yanlış-cevap-2-kez)
- [P-9: Dev server Production mode'da başladı](#p-9-dev-server-production-modeda-başladı)
- [P-10: İç içe feature'lar refactor mı bırak mı](#p-10-iç-içe-featurelar-refactor-mı-bırak-mı)

---

## P-1: 65 dosya uncommitted, hepsi iç içe

**Senaryo:** 7 feature (rol, kategori, favori, AD user, data filter, dashboard, SP preview) paralel geliştirildi. `git status` 65 dosya:
- 33 modified (controllers, views, models, migrations)
- 32 untracked (yeni models, migrations, viewmodels, views, JS, docs)

Hunk-level split (`git add -p`) saatlerce sürer; mega-commit anti-pattern.

**Çözüm: Orta yol (pragmatik bucket) — 3 katmanlı strateji:**

1. **Yeni (untracked) dosyalar:** feature başına ayrı commit.
   - `feat: role system with junction table` (Role.cs + UserRole.cs + migration 08 + EditRole.cshtml)
   - `feat: report categories` (ReportCategory.cs + ReportCategoryLink.cs + migration 07)
   - `feat: report favorites` (ReportFavorite.cs + migration 06)
   - `feat: AD user support schema` (migration 09)
   - `feat: user data filter` (UserDataFilter.cs + migration 13)
   - `feat: dashboard engine` (DashboardConfig.cs + DashboardRenderer.cs + dashboard-builder.js + migration 10-14 + SP'ler)

2. **Shared model infrastructure** (modified ama tüm feature'ları etkiler):
   - `feat: shared model infrastructure` (DbContext + User + ReportCatalog + Program.cs + csproj + migration 02/03)

3. **Modified controller/view dosyaları** — **controller scope** ile böl (3-4 consolidated commit):
   - `feat(admin): consolidated admin panel` (AdminController + 5 view + 3 viewmodel + admin-report-form.js)
   - `feat(auth): AD user + role CSV` (AuthController + Login.cshtml)
   - `feat(reports): favorites + filter + dashboard iframe` (ReportsController + 2 viewmodel + 2 view)

4. **Docs:** `docs: user guide + install notes`

**Sonuç:** 65 dosya → 11-14 commit, her biri anlamlı. Commit 10b'de AdminController 1736 satır anti-pattern kabul edilir, commit mesajında **Known technical debt** notu düşer, refactor ayrı TODO'ya bağlanır.

### Anahtar: "Mesaj gövdesinde teknik borç itirafı"

Consolidated commit'te:

```
feat(admin): consolidated admin panel for new features

Known technical debt (tracked for refactor):
- AdminController.cs is now 1736 lines. Service extraction
  (UserManagementService, ReportManagementService, DataSourceService)
  is planned as TODO M-01 (Faz 2, ~2 days).
- Several catch blocks still surface ex.Message to the user
  (M-02 - exception sanitize, Faz 1).
```

6 ay sonra git blame bakan geliştirici "niye bu kadar büyük?" sorusuna cevap bulur.

---

## P-2: Pre-commit hook eski koddaki ihlali yakalıyor, commit-split blok

**Senaryo:** Uncommitted 65 dosya var, hook aktif, commit-split'e başladın. `git commit` çalıştırdığında pre-commit hook **mevcut koddaki** `ex.Message` ihlalini yakalıyor (yıllardır orada, düzeltmedin). Commit blok.

**Seçenekler:**

| Yaklaşım | Artı | Eksi |
|---|---|---|
| İhlali şimdi düzelt | Temiz | Scope crawl, 50 dosya düzeltme |
| `--no-verify` | Hızlı | Claude Code hook'ları bypass etmez, sadece git hooks |
| Hook'u disable et | Hızlı, temiz | Kural geçici kapalı |

**Tercih edilen: Hook'u geçici disable + sonda re-enable commit'i.**

```bash
# 1. settings.json'dan PreToolUse bloğunu kaldır (elle edit)
# 2. Commit-split'i tamamla (10-15 commit)
# 3. settings.json'a PreToolUse tekrar ekle
# 4. Son commit: "chore: re-enable pre-commit antipattern hook"
```

**Commit mesajında neden:**

```
chore: re-enable pre-commit antipattern hook

The PreToolUse hook was temporarily removed from settings.json during
the commit-split so pre-existing violations (ex.Message user leakage
in legacy controllers, etc.) would not block splitting the 65-file
backlog. With the backlog organised, the hook is re-registered so new
work is checked again.

Known violations still present in existing code will trigger the hook
when those files are next edited; they are tracked as M-02, M-08, and
M-09 and will be fixed in Faz 1/2 refactor passes.
```

**Ders:** Hook, *yeni* kod için kalite koruması. Eski ihlalleri yakalaması **sistemik borcun işareti** — TODO'ya ekle, refactor phase'e bağla, şimdi bloklamasın.

---

## P-3: Hook çıktısı context'te var, "atla" varsayımı

**Senaryo:** Claude oturum açıldı. SessionStart hook fire etti, çıktı context'te görünüyor (git log + TODO + journal). Claude düşünüyor: "Hook çalıştı, özet önümde, `bash` tekrar çalıştırmaya gerek yok." Cevap verir. Ama cevap **hafızadan**, çünkü context'teki çıktı stale olabilir (dün fire etmiş, bugün farklı durum vb.).

**Gerçek olay (ReportHub, 22 Nisan 2026):**
- Sabah: Claude "günaydın" sorusuna context'teki hook çıktısından cevap verdi. Kullanıcı fark etti, "neden çalıştırmadın?" diye sordu.
- Öğleden sonra: Kullanıcı "nerede kaldık?" dedi. Claude yine context'ten cevapladı. Kullanıcı: "varsayım yapma, her oturumda çalıştır, nereye yazıyorsan da yaz."

**Çözüm:** `.claude/rules/session-protocol.md` içinde **koşulsuz kural**:

> **Koşulsuz kural:** Context'te hook çıktısı görünse bile, her oturumun başında `bash .claude/hooks/session-start.sh` komutunu elle çalıştır. "Hook fire etti, atla" varsayımı **YASAK**.

CLAUDE.md §0'da da aynı kural tekrarlanır. Çünkü `CLAUDE.md` her oturum enjekte olur; rule dosyası bazı durumlarda atlanabilir. Redundant yazım kasıtlı — kritik kural kaybolmasın.

**Ders:** Context'te bir bilgi olması "taze" olması demek değil. Kritik okuma için her seferinde fresh çağır.

---

## P-4: Kullanıcı kural söyledi, Claude aklında tuttu

**Senaryo:** Kullanıcı: "artık tüm commit mesajları Türkçe yazılsın". Claude: "Tamam, akılda tutuyorum." 3 saat sonra compact/clear. Yeni commit: İngilizce. Kullanıcı: "Türkçe demiştim ya."

**Çözüm:** Kural **dosyaya yazılır**, konuşmada kalmaz.

```bash
# .claude/rules/commit-discipline.md dosyasına ekle
echo "
## Dil kurali
Commit mesajlari Turkce yazilir. Istisna: build/CI/tooling mesajlari Ingilizce olabilir.
" >> .claude/rules/commit-discipline.md
```

**Prensip (session-protocol.md):**
> Kullanıcı yeni bir kural / tercih söylüyorsa konuşmada kalmaz, hemen ilgili `.claude/rules/*.md` dosyasına eklenir. "Aklında tut" demez — Claude konuşma hafızasından kural çekemez.

---

## P-5: 1700+ satır tek controller

**Senaryo:** `AdminController.cs` 1736 satır. Role + category + favorite + dashboard + user + datasource CRUD + SP preview hepsi içinde. Yeni bir feature eklemek için bu dosyaya dokunmak zorundasın, conflict'ler, merge patlamaları.

**Değişken çözüm seçenekleri:**

1. **Hemen service extraction (refactor):** 2-3 gün iş. UserManagementService, ReportManagementService, DataSourceService. Controller endpoint'e iner, ~400 satıra düşer. Ama commit-split ve backlog donar.

2. **Pragmatik: Consolidated commit, refactor ayrı ADR ile Faz 2'ye:** Şu an dokunma, "Known technical debt" notuyla ileri it.

3. **Hybrid: Her yeni feature eklerken o feature'la ilgili kısmı service'e çıkar:** kademeli refactor. Uzun vadeli temiz.

**Tercih: Proje aşamasına bak.**
- Greenfield, 1 kişi, 1 ay: `(2)` pragmatic — zaman darlığında doğru tercih.
- Mature, takım, production: `(1)` hemen refactor, stabilize.
- Arada: `(3)` hybrid — her dokunuşta biraz düzelt.

**Commit mesajında netlik:**

```
Known technical debt (tracked for refactor):
- AdminController.cs is now 1736 lines. Service extraction is planned
  as TODO M-01 (Faz 2, ~2 days). This commit is bundled rather than
  hunk-split to keep the change landable; the refactor is a separate
  concern and will land as its own set of commits.
```

---

## P-6: Hardcoded şifre git history'e kaçtı

**Senaryo:** `appsettings.json` içinde `Password=mydev123` literal. Tracked. Push edildi. GitHub'da görünür.

**Düşünce seçenekleri:**

1. **Şifreyi rotate et** + tracked dosyadan temizle + git history'den temizle (BFG / filter-repo):
   - Doğru, güvenli, zor.
   - Güvenli: rotasyon sonrası history'deki şifre kullanılamaz.
   - Zor: force-push gerekir, takım arkadaşlarını etkiler.

2. **Şifreyi rotate et** + tracked temizle, history'ye dokunma:
   - History'de şifre kalır ama artık çalışmaz (rotasyon).
   - Orta-doğru.

3. **Tracked dosyadan temizle, şifreyi rotate etme** (dev-only credentials):
   - Leak stop going forward, ama history'deki şifre hala çalışır.
   - **Sadece** şifre gerçekten önemsizse (local dev SA, vb.) geçerli.

**Tercih kriteri:** Şifre kritik mi?
- Prod DB şifresi: `(1)` zorunlu.
- Dev SA, local dev: `(3)` kabul edilebilir (kullanıcı onayı ile).

**ReportHub örneği:** Kullanıcı "şifreler önemli değil, dev ortamı lokal" dedi → `(3)` uygulandı. Commit mesajında not:

```
Note: this fix stops the leak going forward. Historic git commits
still contain the passwords; rotation is not required per project
decision (dev-only credentials, local environment).
```

**Her durumda:**
- `appsettings.json` → empty connection string
- `appsettings.Development.json` → gerçek conn string, **gitignored**
- Staging/prod → env var

---

## P-7: Deprecated dosya "DEPRECATED" banner ile kalmış

**Senaryo:** Eski bir dosya (`Views/Auth/AGENT.md`) "proje MVC değil, Razor Pages" gibi yanıltıcı içeriğe sahip. Banner ekledin: `> ⚠️ DEPRECATED — ...`. Ama dosya hala orada, her session okunurken kafa karıştırıyor.

**Kullanıcı geri bildirimi (ReportHub):** "geçerliliği olmayan dosyaları silsek nasıl olur kafa karıştıracağına".

**Çözüm:** Silmek > banner'lamak.

- **Sil:** `git rm` + commit. History'de kalıyor zaten, gerektiğinde `git show`.
- **Sadece banner bırak:** dosya hala açılıyor, Claude hala okuyor, kafa karışık kalıyor.

**İstisna:** Dosyanın içeriği **hala değerli** ama güncel değil ise (eski ADR, deprecated API docs) → banner + aynı konuda yeni dosyaya link.

**Genel kural:** "Yanıltıcı > Silinmiş". Yanıltıcı dosya net iş kaybı; silinmiş dosya git history'de rahatlıkla bulunur.

---

## P-8: Session başında yanlış cevap (2 kez)

**Senaryo:** Aynı gün, aynı hatayı 2 kez yaptın. Sabah: hook çıktısını context'te görüp atladı. Öğleden sonra: aynı şey. Kullanıcı ikincide sinirlendi: "varsayım yapma, nereye yazıyorsan da yaz".

**Eylem üçlüsü (session-protocol.md):**

1. **Kabul et.** Savunmaya geçme. "Hook fire etmedi" mazeret değil — elle okuma sorumluluğu vardır.
2. **Anında kapat.** Hook'u manuel çalıştır, journal oku, TODO gözden geçir.
3. **Önlemini dosyaya yaz.** Kural güçlendir, koşulsuz hale getir.

Bu 3 adım template'te `session-protocol.md` "Ritüel atlandığında" bölümünde yazılı.

**Ders:** Aynı hatayı 2. kez yapmak **sistem eksikliği**. Kural güçlendirme ritüel hatasının değil.

---

## P-9: Dev server Production mode'da başladı

**Senaryo:** G-01 sonrası `appsettings.json` connection string boş. Local dev için `appsettings.Development.json` (gitignored) conn string tutuyor. Ama `dotnet run --no-launch-profile` Production başlatır → Development.json yüklenmez → DB bağlanamaz → Login patlar.

**Çözüm (launch.json için):**

```json
{
  "runtimeArgs": ["run", "--project", "MyApi", "--launch-profile", "http"]
}
```

`launchSettings.json`'da "http" profili `ASPNETCORE_ENVIRONMENT=Development` set eder.

**Alternatif:** `--environment Development` flag, ama dotnet sürümüne göre davranış tutarsız.

**Ders:** Launch config dev'de mutlaka Development mode zorlar. Prod deployment ayrı env var ile gelir. Greenfield'de `.claude/launch.json` şablona "http" profili default girer.

---

## P-10: İç içe feature'lar refactor mı bırak mı

**Senaryo:** Aynı controller'da 7 feature'ın ekleri iç içe. Commit-split sırasında her dosyanın hunk'larını ilgili feature'a dağıtmak istiyorsun. `git add -p` her hunk için soru soruyor, 3 saat sürüyor.

**Tercih:** Pragmatik. Hunk-level split sadece aşağıdaki durumlarda değer:
- Open-source PR, temiz history şart
- Dış denetçi, audit log için
- Multi-contributor, conflict çözümü için

Solo dev + kısa vadeli projede hunk-level split **yatırım değil maliyet**. Consolidated commit + "Known debt" notu daha akıllı.

**Kural of thumb:** Commit history'yi kim okuyacak?
- Sen (solo): consolidated OK
- Takım: controller-scoped split (3-5 consolidated commit)
- Public OSS: hunk-level

---

## Özet — Her Pattern'in Dersi

| Pattern | Ders |
|---|---|
| P-1 | 65 dosya için orta yol: yeni dosyalar feature-başına, modified controller-scoped |
| P-2 | Hook eski kodu yakalarsa: geçici disable + "Known debt" commit |
| P-3 | Context'te bilgi varsa bile fresh çağır, "atla" varsayımı yasak |
| P-4 | Kural konuşmada kalmaz, dosyaya yaz |
| P-5 | 1700+ satır dosya: pragmatic now + ADR/TODO ile refactor plan |
| P-6 | Şifre rotation kriteri: kritik mi yoksa dev-only mu |
| P-7 | Yanıltıcı dosya = sil, banner kafa karıştırır |
| P-8 | Aynı hata 2 kez = kural eksikliği, önlemi dosyaya yaz |
| P-9 | Dev launch config Development mode zorlar |
| P-10 | Commit-split yatırımı hedef kitleye göre ayarla |
