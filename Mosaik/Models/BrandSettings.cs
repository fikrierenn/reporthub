using System.ComponentModel.DataAnnotations;

namespace Mosaik.Models
{
    public class BrandSettings
    {
        public int Id { get; set; } = 1;

        [Required, MaxLength(100)]
        public string SiteTitle { get; set; } = "Mosaik";

        [MaxLength(200)]
        public string? Slogan { get; set; }

        [MaxLength(500)]
        public string? LogoPath { get; set; }

        [Required, MaxLength(7)]
        public string PrimaryColor { get; set; } = "#6366f1";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
