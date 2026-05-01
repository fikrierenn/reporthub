# Kodlama Disiplini (Karpathy Prensipleri)

_Kaynak: Andrej Karpathy'nin LLM coding gözlemleri. Sadece mevcut kurallarla örtüşmeyen 2 prensip._

## Simplicity First — Spekülatif Kod Yasak

- İstenen dışında feature ekleme. "İleride lazım olur" gerekçesiyle abstraction yok.
- Tek kullanımlık kod için class/interface/strategy pattern çıkarma.
- İmkânsız senaryolar için error handling yazma (iç kodda framework garantilerine güven).
- 200 satır yazıp 50'ye düşürebiliyorsan → yeniden yaz.
- Test: "Kıdemli bir mühendis buna 'gereksiz karmaşık' der mi?" Evet → sadeleştir.

**ReportHub örneği (YAPMA):**
```csharp
// Kullanıcı "discount hesapla" dedi → Strategy pattern + factory + config sınıfı çıkarma
public interface IDiscountStrategy { ... }
public class PercentageDiscount : IDiscountStrategy { ... }
public class DiscountCalculator { ... }
```
**YAP:**
```csharp
public static decimal CalculateDiscount(decimal amount, decimal percent)
    => amount * (percent / 100m);
```

## Surgical Changes — Sadece İstenen Satıra Dokun

- Bug fix yaparken komşu kodu "iyileştirme", yorum düzenleme, stil değiştirme yasak.
- Mevcut stili taklit et — farklı yapardın bile olsa.
- İlgisiz dead code fark edersen: **raporla, silme** (kullanıcı kararı).
- Senin değişikliğin yüzünden orphan kalan import/değişken/fonksiyonu sil. Önceden var olan dead code'a dokunma.

**Kontrol testi:** Her değiştirilen satır, kullanıcının talebine doğrudan izlenebilmeli. İzlenemiyorsa → o satırı geri al.

**code-simplifier agent ile ilişki:** `code-simplifier` scope = recently-modified kod. Drive-by refactoring değil, kendi yazdığın kodu sadeleştirme. İkisi çelişmez — simplifier yalnızca senin dokunduğun yere bakar.

## Mevcut Kurallarla İlişki

| Karpathy prensibi | ReportHub karşılığı | Durum |
|---|---|---|
| Think Before Coding | `plan-first.md` (Tier 3 plan zorunlu) | Zaten var |
| Goal-Driven Execution | `plan-first.md` Done criteria + session-protocol TodoWrite | Zaten var |
| **Simplicity First** | — | **Bu dosya** |
| **Surgical Changes** | — | **Bu dosya** |
