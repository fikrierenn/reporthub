# Bilinen Sorunlar (blocker değil, bekliyor)

## Kaspersky EBADF — Claude Code token rename hatası

**Belirti:**
```
EBADF: bad file descriptor, rename
  'C:\Users\fikri.eren\AppData\Roaming\Claude\buddy-tokens.json.tmp-<hash>'
  -> 'C:\Users\fikri.eren\AppData\Roaming\Claude\buddy-tokens.json'
```

**Sebep:** Kaspersky Endpoint Security for Windows (BKM kurumsal AV) real-time scanning ile `.tmp-*` dosyalarını tararken file handle tutuyor — Claude rename edemiyor.

**Etki:** Kozmetik. Claude Code retry ile token'ı yenileyebiliyor, işlevsellik etkilenmiyor. Sadece konsol gürültüsü.

**Kalıcı çözüm:** BKM IT departmanından Kaspersky exclusion iste:
- Dizin: `C:\Users\fikri.eren\AppData\Roaming\Claude\`
- Ya da executable: `C:\Users\fikri.eren\AppData\Roaming\Claude\claude-code\*\claude.exe`

**User workaround (kurumsal policy izin verirse):**
- Kaspersky UI → Settings → Trusted Zone / Exclusions → klasör ekle.
- Policy kilitliyse: "Disabled by administrator" yazar → IT'ye git.

**Teşhis komutu:**
```powershell
Get-CimInstance -Namespace "root\SecurityCenter2" -ClassName AntiVirusProduct
# Kaspersky Endpoint Security + productState 266240 görünür
```

**Tarih:** 21 Nisan 2026'da teşhis edildi.

---

> **Arşiv notu (2026-06-29):** Bu dosyadaki 4 eski madde (SP Önizle handler, Uncommitted 32 Dosya, AGENT.md yanıltıcı, Test Coverage <%10) **çözüldü/eskidi** — `consolidate-mosaik` curator ile kaldırıldı. git history'de duruyor. Sadece Kaspersky canlı.
