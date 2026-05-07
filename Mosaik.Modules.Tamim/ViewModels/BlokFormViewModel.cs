using System.ComponentModel.DataAnnotations;

namespace Mosaik.Modules.Tamim.ViewModels
{
    // M-07: Entity referansı YASAK. Flat properties, mass assignment koruması.
    public class BlokFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Departman adı zorunlu.")]
        [MaxLength(100)]
        public string DepartmanAdi { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konu zorunlu.")]
        [MaxLength(200)]
        public string Konu { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama zorunlu.")]
        public string Aciklama { get; set; } = string.Empty;

        [Required(ErrorMessage = "Blok türü seçilmeli.")]
        [Range(1, int.MaxValue, ErrorMessage = "Blok türü seçilmeli.")]
        public int BlokTuruId { get; set; }

        public bool Acil { get; set; }

        // Salt okuma (service set eder)
        public string BlokNo { get; set; } = string.Empty;
        public DateTime BlokTarihi { get; set; }
        public int? TamimId { get; set; }
        public string? TamimBaslik { get; set; }

        public string Message { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;

        // Dropdown için
        public List<BlokTuruOption> BlokTuruSecenekleri { get; set; } = new();
    }

    public class BlokTuruOption
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
    }
}
