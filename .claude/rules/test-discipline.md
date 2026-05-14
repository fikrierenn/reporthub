# Test Disiplini — Testleri Kapatmadan Yap

_Kapsam: Yeni feature / bug fix / refactor kapatma kriteri._
_Kural 2026-05-14 (kullanıcı): "testleri kapatmadan yap"._

## Neden bu dosya var

**2026-05-14:** Integration test scaffolding (Mosaik.Tests/Integration/ + WebApplicationFactory + smoke test) eklendi, build geçti ama **test çalıştırılmadan** "tamam" sayıldı. Test koşumu yapıldığında 5/5 başarısız çıktı (`DbContextOptions + UseSqlServer` çakışması). Kullanıcı durdurdu, scaffolding geri alındı.

Kök sebep: **build = test değil.** `dotnet build` yeşil geçen kod runtime'da kırılabilir. Integration test'in varlığı koşulması anlamına gelmez. Bu disiplin tekrar etmemeli — kural dosyasına yazıldı.

## Mutlak Kurallar

1. **Yeni feature / bug fix / refactor → test çalıştırılmadan kapatma.**
   - Etkilenen test'ler `dotnet test` ile **geçti** olmalı (failure ya da skip yetmez).
   - "Test yok ama kod doğru görünüyor" yetmez — boşluk varsa **yeni test yaz**, sonra çalıştır.
   - Build yeşil ≠ test yeşil. Bunu karıştırma.

2. **Yeni test eklendiğinde → en az 1 koşum yap.**
   - Test dosyasını yazmak yetmez; `dotnet test` ile çalıştır + sonucu raporla.
   - Failure varsa **fix et veya scaffolding'i geri al** — yarım kalmış test commit'leme.
   - `[Skip("reason")]` veya `[Trait("Category", "Manual")]` ile devre dışı bırakmadan koşum şart.

3. **Test yokken bug fix → en az regression test yaz.**
   - Fix öncesi: failing test (bug'ı reproduce eden)
   - Fix sonrası: aynı test geçer
   - Sonraki regression yakalanır

4. **Refactor → mevcut test seti yeşil kalmalı.**
   - Refactor öncesi: `dotnet test` 260/260 geçti not'u
   - Refactor sonrası: aynı sayı geçti (veya artmış)
   - Test sayısı azalırsa kasıtlı silme dışında **regression sinyali**

## Anti-pattern (yaparsan kullanıcı haklı olarak kızar)

1. **"Build yeşil, tamam sayalım"** — test çalıştırılmadı, runtime kırık olabilir.
2. **Test yazıp çalıştırmadan commit etmek** — `Mosaik.Tests/Integration/` 14 May örneği. Build geçti ama 5/5 başarısız.
3. **`[Skip]` veya `[Fact(Skip="...")]` ile testi by-pass etmek** — sebep dokümante edilmeden devre dışı kabul edilmez.
4. **Failure'ı "ileride bakacağım" diye bırakmak** — kırık test git history'ye girerse stale-claim olur (todo-verification.md ile aynı mantık).
5. **"Integration test çok karmaşık, unit yeterli"** — controller path'leri integration olmadan denenemez. Karmaşıksa scope'u küçült (smoke set), atlama.

## Workflow

### Yeni feature / bug fix kapatma kontrolü
```bash
# 1. Build
cd Mosaik && dotnet build --nologo

# 2. Test (sadece etkilenen alan)
cd .. && dotnet test --filter "FullyQualifiedName~<Area>" --nologo

# 3. Tam regression (önemli refactor sonrası)
dotnet test --nologo
```

Çıktı `Toplam: N, Başarılı: N, Başarısız: 0, Atlandı: 0` olmalı. Başarısız + 0 değilse **kapatma yok**.

### Yeni test eklendiğinde rapor formatı
Commit message'da test sayısı + sonuç:
```
fix(security): X bulgusu

Test: dotnet test → 260/260 gecti (3 yeni test eklendi)
```

### Integration test özel notu (2026-05-14)
Mosaik integration test infrastructure **henüz yok**. Eklenirse:
- WebApplicationFactory<Program> + DbContextOptions düzgün override edilmeli
- Hangfire `IsEnvironment("Testing")` bypass'i Program.cs'te şart
- Her PR'da en az smoke koşumu (`GET /health`, `GET /Auth/Login`, `GET /Admin` unauthorized)

## Tetikleyiciler

Bu kural **otomatik** uygulanır:

- Yeni `[Fact]` veya `[Theory]` eklendiğinde
- `Mosaik.Tests/` altında dosya değiştiğinde
- Controller / Service / Repository değiştiğinde (regression riski)
- "Tamam / kapatıyorum / bitti" sözcüğü kullanıldığında (commit öncesi)
- `dotnet build` çalıştırıldığında (`dotnet test` ardından gelmeli)

## Override

Kullanıcı açıkça "test çalıştırmadan commit" / "test atla" derse:
- Commit message'da `[test-skipped: <gerekçe>]` notu
- TODO.md'ye "test koşumu borcu" satırı
- Bir sonraki oturum başında session-start hook bunu uyarır

Aksi default = **test koşumu zorunlu**.

## İlişkili

- `.claude/rules/coding-discipline.md` — surgical changes (test scope da surgical)
- `.claude/rules/todo-verification.md` — kanıt disiplini (build = test değil)
- `Mosaik.Tests/` — 260 [Fact]+[Theory], xUnit
- `.claude/rules/csharp-conventions.md` — async test, FluentAssertions yok (vanilla Assert)
