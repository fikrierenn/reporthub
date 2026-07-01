namespace Mosaik.Modules.Forms.Entities
{
    // Plan 41 §4.1 — FormField.FieldType sabitleri (pure-technical, lookup değil — 13 sabit tip,
    // kullanıcı yönetmez, feedback_mosaik_status_enum_lookup_pattern istisnası).
    public static class FormFieldType
    {
        public const byte Text = 0;
        public const byte TextArea = 1;
        public const byte Number = 2;
        public const byte Date = 3;
        public const byte DateTime = 4;
        public const byte Select = 5;
        public const byte MultiSelect = 6;
        public const byte Radio = 7;
        public const byte Checkbox = 8;
        public const byte File = 9;
        public const byte Signature = 10;
        public const byte Hidden = 11;
        public const byte Section = 12;
    }
}
