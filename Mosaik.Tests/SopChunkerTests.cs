using Mosaik.Modules.SOP.Services;

namespace Mosaik.Tests;

// Plan 34.1 Faz 1 A-09 — chunker boundary + overlap + Türkçe karakter.
public class SopChunkerTests
{
    [Fact]
    public void Split_Empty_ReturnsEmpty()
    {
        Assert.Empty(SopChunker.Split(""));
        Assert.Empty(SopChunker.Split("   "));
        Assert.Empty(SopChunker.Split(null!));
    }

    [Fact]
    public void Split_ShortText_SingleChunk()
    {
        var text = "Kısa prosedür içeriği. İzin formu doldur, yöneticiye onaylat.";
        var chunks = SopChunker.Split(text);

        Assert.Single(chunks);
        Assert.Equal(text, chunks[0]);
    }

    [Fact]
    public void Split_LongText_MultipleChunksWithOverlap()
    {
        // ~5000 char Türkçe içerik — 3 chunk üretmeli (2048 + step 1792).
        var text = string.Join(' ',
            Enumerable.Range(0, 800)
                .Select(i => $"madde-{i}"));

        var chunks = SopChunker.Split(text);

        Assert.True(chunks.Count >= 2, $"Beklenen ≥2 chunk, üretilen {chunks.Count}");
        Assert.All(chunks, c => Assert.True(c.Length <= 2048 + 50, $"chunk uzunluğu {c.Length}"));

        // Overlap: ardışık chunkların son/ilk kelimeleri kesişmeli (~256 char overlap ≈ 30 kelime).
        if (chunks.Count >= 2)
        {
            var tailWords = chunks[0].Split(' ').TakeLast(40).ToHashSet();
            var headWords = chunks[1].Split(' ').Take(40).ToHashSet();
            Assert.True(tailWords.Overlaps(headWords), "Ardışık chunk'lar arası overlap yok.");
        }
    }

    [Fact]
    public void Split_LongText_RespectsWordBoundary()
    {
        // Kelime sınırında kesilmeli — orta kelime parçalanmasın.
        var text = string.Join(' ',
            Enumerable.Range(0, 600).Select(i => $"prosedür-madde-{i:D4}"));
        var chunks = SopChunker.Split(text);

        foreach (var chunk in chunks.Take(chunks.Count - 1))                // son chunk hariç
        {
            // Boşluk olmayan karakter ile bitmemeli (kelime ortası kesim)
            Assert.False(chunk.EndsWith("-"), $"Kelime ortasından kesilmiş: ...{chunk[^30..]}");
        }
    }

    [Fact]
    public void Split_TurkishCharacters_Preserved()
    {
        var text = "Şirket içi İletişim Politikası. " + new string('ç', 3000) +
                   " Ürün geri çağırma süreci başlığında özel karakterler korunmalı.";
        var chunks = SopChunker.Split(text);

        Assert.True(chunks.Count >= 2);
        Assert.Contains(chunks, c => c.Contains("Şirket"));
        Assert.All(chunks, c => Assert.DoesNotContain('�', c));         // replacement char yok
    }
}
