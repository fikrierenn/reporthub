using Mosaik.Modules.Kvkk.Entities;
using Mosaik.Modules.Kvkk.Services;

namespace Mosaik.Modules.Kvkk.Areas.Kvkk.ViewModels
{
    // Plan 40 Faz 4 — reverse search sonucu: veri öğesi + işlendiği süreçler + uyumluluk skoru.
    public class DataElementReverseViewModel
    {
        public DataElement Element { get; set; } = null!;
        public List<DataElementService.ProcessUsage> Processes { get; set; } = new();
        public int CompliancePercent { get; set; }
        public int WithoutRetention { get; set; }
    }
}
