using Mosaik.Modules.SOP.Entities;

namespace Mosaik.Modules.SOP.ViewModels
{
    // Plan 34 Faz C — Details ekranı (admin).
    public class SopDetailsViewModel
    {
        public SopDocument Document { get; set; } = null!;
        public List<SopVersion> Versions { get; set; } = new();
        public SopVersion? ActiveVersion { get; set; }
    }
}
