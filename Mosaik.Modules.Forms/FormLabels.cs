using Mosaik.Modules.Forms.Entities;

namespace Mosaik.Modules.Forms
{
    // Plan 41 Faz 2 — byte kod → Türkçe etiket (KvkkLabels ile aynı pattern).
    public static class FormLabels
    {
        public static string Status(byte v) => v switch
        {
            0 => "Taslak", 1 => "Yayında", 2 => "Arşiv", _ => "—"
        };

        public static string StatusClass(byte v) => v switch
        {
            0 => "warn", 1 => "ok", 2 => "", _ => ""
        };

        public static string FieldType(byte v) => v switch
        {
            FormFieldType.Text => "Kısa Metin",
            FormFieldType.TextArea => "Uzun Metin",
            FormFieldType.Number => "Sayı",
            FormFieldType.Date => "Tarih",
            FormFieldType.DateTime => "Tarih + Saat",
            FormFieldType.Select => "Açılır Liste",
            FormFieldType.MultiSelect => "Çoklu Seçim",
            FormFieldType.Radio => "Tekli Seçim (Radyo)",
            FormFieldType.Checkbox => "Evet/Hayır",
            FormFieldType.File => "Dosya",
            FormFieldType.Signature => "İmza",
            FormFieldType.Hidden => "Gizli Alan",
            FormFieldType.Section => "Bölüm Başlığı",
            _ => "—"
        };

        // Options/choices gerektiren tipler — admin UI'da Options alanını sadece bu tiplerde göster.
        public static bool RequiresOptions(byte fieldType) =>
            fieldType is FormFieldType.Select or FormFieldType.MultiSelect or FormFieldType.Radio;
    }
}
