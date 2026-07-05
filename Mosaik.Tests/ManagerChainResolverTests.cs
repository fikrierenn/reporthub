using Mosaik.Services.Workflow;
using Xunit;

namespace Mosaik.Tests
{
    // Plan 57 Part C — org-omurga amir zinciri (saf, DB'siz).
    // Ağaç: GM(1,"100-BKM") → Müdür(2,"200-BKM") → Şef(3,"300-BKM") → Uzman(4,"400-BKM")
    public class ManagerChainResolverTests
    {
        private static List<PositionNode> Tree() =>
        [
            new(1, null, "100-BKM"),   // GM
            new(2, 1, "200-BKM"),      // Müdür
            new(3, 2, "300-BKM"),      // Şef
            new(4, 3, "400-BKM"),      // Uzman
            new(5, 2, null),           // boş pozisyon (holder yok), parent=Müdür
            new(6, 5, "600-BKM")       // boş pozisyonun altındaki kişi
        ];

        [Fact]
        public void Resolve_DirectHolder_ReturnsParentHolder()
        {
            // Uzman(400) → amiri Şef(300)
            var m = ManagerChainResolver.Resolve(Tree(), "400-BKM", mappedPositionId: null);
            Assert.Equal("300-BKM", m);
        }

        [Fact]
        public void Resolve_EmptyParentHolder_SkipsToGrandparent()
        {
            // 600'ün pozisyonunun parent'ı (5) holder'sız → bir üst (Müdür 200) bulunur.
            var m = ManagerChainResolver.Resolve(Tree(), "600-BKM", mappedPositionId: null);
            Assert.Equal("200-BKM", m);
        }

        [Fact]
        public void Resolve_MappedPositionFallback_WhenNotHolder()
        {
            // Frontline kişi (holder değil) — GorevPersonelMap pozisyon 4'e bağlamış → amiri Şef.
            var m = ManagerChainResolver.Resolve(Tree(), "999-BKM", mappedPositionId: 4);
            Assert.Equal("300-BKM", m);
        }

        [Fact]
        public void Resolve_TopOfTree_ReturnsNull()
        {
            // GM'in amiri yok.
            var m = ManagerChainResolver.Resolve(Tree(), "100-BKM", mappedPositionId: null);
            Assert.Null(m);
        }

        [Fact]
        public void Resolve_UnknownPerson_NoMapping_ReturnsNull()
        {
            var m = ManagerChainResolver.Resolve(Tree(), "999-BKM", mappedPositionId: null);
            Assert.Null(m);
        }

        [Fact]
        public void Resolve_SelfAsParentHolder_SkipsUp()
        {
            // Kişi hem pozisyon 3'ün hem parent 2'nin holder'ı (vekalet) → kendini amir SAYMAZ, GM'e çıkar.
            List<PositionNode> tree =
            [
                new(1, null, "100-BKM"),
                new(2, 1, "300-BKM"),   // aynı kişi üstte de holder
                new(3, 2, "300-BKM")
            ];
            var m = ManagerChainResolver.Resolve(tree, "300-BKM", mappedPositionId: null);
            Assert.Equal("100-BKM", m);
        }

        [Fact]
        public void Resolve_CycleInTree_ReturnsNull_NoInfiniteLoop()
        {
            List<PositionNode> tree =
            [
                new(1, 2, null),
                new(2, 1, null),
                new(3, 1, "300-BKM")
            ];
            var m = ManagerChainResolver.Resolve(tree, "300-BKM", mappedPositionId: null);
            Assert.Null(m);
        }

        [Fact]
        public void Resolve_CaseAndWhitespace_Insensitive()
        {
            var m = ManagerChainResolver.Resolve(Tree(), "  400-bkm ", mappedPositionId: null);
            Assert.Equal("300-BKM", m);
        }
    }
}
