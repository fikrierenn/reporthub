using System.ComponentModel.DataAnnotations;

namespace Mosaik.Models
{
    public class Holiday
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        public HolidayType Type { get; set; } = HolidayType.National;

        // Sabit gregoryen tatil için ay+gün (Yılbaşı=1/1, 29 Ekim=10/29). Lunar için null.
        public int? FixedMonth { get; set; }
        public int? FixedDay { get; set; }

        public bool IsHalfDay { get; set; } = false;
        public bool IsSystemDefined { get; set; } = true;
        public bool IsActive { get; set; } = true;

        public ICollection<HolidayOccurrence> Occurrences { get; set; } = new List<HolidayOccurrence>();
    }

    public class HolidayOccurrence
    {
        public int Id { get; set; }
        public int HolidayId { get; set; }
        public int Year { get; set; }
        public DateOnly Date { get; set; }

        public Holiday Holiday { get; set; } = null!;
    }

    public enum HolidayType
    {
        National,   // Resmi ulusal tatil
        Religious,  // Dini bayram (Ramazan/Kurban)
        HalfDay     // Yarım gün (bazı tatil arifesi)
    }
}
