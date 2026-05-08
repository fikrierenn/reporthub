using Mosaik.Core.Domain;
using Mosaik.Services;

namespace Mosaik.ViewModels
{
    // Plan 20 Faz C — Public org chart view (canlı incumbent).
    public class OrgChartPublicViewModel
    {
        public List<OrgPosition> Positions { get; set; } = new();
        public Dictionary<string, List<OrgIncumbent>> IncumbentsByCode { get; set; } = new();
        public List<OrgIncumbent> UnmatchedIncumbents { get; set; } = new();
        public string? ZirveError { get; set; }
        public DateTime FetchedAtUtc { get; set; }
        public string CurrentView { get; set; } = "tree";
    }

    // Plan 20 Faz B — Admin OrgChart liste görünümü.
    public class AdminOrgChartViewModel : UserMessageViewModel
    {
        public List<OrgPosition> Positions { get; set; } = new();

        // Zirve'de var ama Mosaik OrgPositions'ta olmayan unvanlar.
        // Admin tek-tık import edebilir. Zirve erişimi yoksa boş döner.
        public List<string> UnknownZirveCodes { get; set; } = new();

        // Zirve canlı erişim hatası bilgisi (varsa banner göster).
        public string? ZirveDiscoveryError { get; set; }

        // Aktif görünüm tipi: "list" (drag-drop nested), "tree" (D3 top-down), "horizontal" (D3 sol-sağ).
        // Sadece "list" düzenlenebilir; diğerleri read-only.
        public string CurrentView { get; set; } = "list";
    }

    public class AdminOrgPositionFormViewModel : UserMessageViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int? ParentPositionId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }

        public List<OrgPosition> AvailableParents { get; set; } = new();
        public bool IsCreate => Id == 0;
    }

    // Drag-drop reorder isteği — JSON body olarak gelir.
    public class OrgChartReorderRequest
    {
        public int PositionId { get; set; }
        public int? NewParentId { get; set; }
        public int[] SiblingOrderIds { get; set; } = Array.Empty<int>();
    }
}
