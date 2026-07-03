using Mosaik.Modules.GorevTanimlari.Entities;
using Mosaik.Modules.GorevTanimlari.Services;

namespace Mosaik.Modules.GorevTanimlari.ViewModels;

// Zirve personel → görev-tanımı eşleme ekranı model'i (kişi listesi + doküman seçenekleri).
public sealed class MappingIndexViewModel
{
    public List<MappingService.PersonRow> People { get; set; } = new();
    public List<GorevDocument> Docs { get; set; } = new();
}
