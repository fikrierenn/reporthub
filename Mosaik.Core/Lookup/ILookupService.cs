namespace Mosaik.Core.Lookup
{
    // Plan 17 v2 — Cross-modül lookup erişim. Modüller (Mosaik.Modules.*) bu
    // interface'i inject ederek BlokTuru/BildirimTuru/... lookup değerlerini
    // çeker. Host (Mosaik.Services.LookupService) implement eder.
    public interface ILookupService
    {
        Task<List<DictionaryValue>> GetValuesAsync(string typeCode);
        Task<DictionaryValue?> GetByCodeAsync(string typeCode, string valueCode);
        // never-blank: etiket yoksa raw code döner (boş badge/label olmaz).
        Task<string> LabelAsync(string typeCode, string valueCode);
    }
}
