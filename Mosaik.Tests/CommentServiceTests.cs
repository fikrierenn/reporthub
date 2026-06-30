using Mosaik.Services;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 54 M5 — @mention çıkarım logic testleri (saf, DB'siz).
    public class CommentServiceTests
    {
        [Fact]
        public void ExtractMentions_Single()
        {
            var r = CommentService.ExtractMentions("Merhaba @fikri bakar mısın?");
            Assert.Equal(new[] { "fikri" }, r);
        }

        [Fact]
        public void ExtractMentions_Multiple()
        {
            var r = CommentService.ExtractMentions("@ali ve @veli.kaya ve @mehmet_01");
            Assert.Equal(new[] { "ali", "veli.kaya", "mehmet_01" }, r);
        }

        [Fact]
        public void ExtractMentions_CaseInsensitiveDistinct()
        {
            var r = CommentService.ExtractMentions("@Fikri @fikri @FIKRI");
            Assert.Single(r);
        }

        [Fact]
        public void ExtractMentions_NoMention_Empty()
        {
            Assert.Empty(CommentService.ExtractMentions("düz metin, mention yok"));
        }

        [Fact]
        public void ExtractMentions_EmailNotFullyMentioned()
        {
            // foo@bar.com → "@bar.com" yakalanır (mention sözdizimi gereği) — kabul,
            // çözüm aşamasında geçersiz kullanıcı adları aktif-kullanıcı listesinde elenecek.
            var r = CommentService.ExtractMentions("ornek@bar.com adresine yaz");
            Assert.Contains("bar.com", r);
        }

        [Fact]
        public void ExtractMentions_NullOrEmpty()
        {
            Assert.Empty(CommentService.ExtractMentions(""));
            Assert.Empty(CommentService.ExtractMentions(null!));
        }

        [Fact]
        public void ExtractMentions_TooShortTokenIgnored()
        {
            // tek karakter (@a) — regex min 2 karakter ister.
            Assert.Empty(CommentService.ExtractMentions("@a kısa"));
        }
    }
}
