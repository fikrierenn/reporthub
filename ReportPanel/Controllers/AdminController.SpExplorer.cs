using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReportPanel.Services.Eval;

namespace ReportPanel.Controllers
{
    // Partial split (csharp-conventions hard-limit). SP keşif/önizleme + formula validation
    // (admin builder runtime'i için).
    public partial class AdminController
    {
        [HttpGet]
        [Authorize(Roles = "admin")]
        [Route("Admin/ProcParams")]
        // M-13 R4.1: Logic SpExplorerService.GetParametersAsync'e tasindi (28 Nisan 2026).
        public async Task<IActionResult> ProcParams(string dataSourceKey, string procName)
        {
            var result = await _spExplorer.GetParametersAsync(dataSourceKey, procName);
            if (!result.Success)
            {
                return BadRequest(result.Error ?? "ProcParams failed.");
            }
            return Json(new { fields = result.Fields });
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        [Route("Admin/FilterOptions")]
        // Plan 07 Faz 3: DataSourceKey parametresi kaldirildi (FilterDefinition master'dan otorite).
        public async Task<IActionResult> FilterOptions(string filterKey)
        {
            var result = await _filterOptions.GetAsync(filterKey);
            if (!result.Success && result.Error == "FilterKey gerekli.")
            {
                return BadRequest(result.Error);
            }
            return Json(new { options = result.Options });
        }

        // Plan 05.B: Hesaplı kolon formula sözdizim doğrulama (V2 builder textarea blur + Kaydet öncesi).
        // Tek source-of-truth backend FormulaParser; client-side parser portu yok.
        [HttpPost]
        [Route("Admin/ValidateFormula")]
        [ValidateAntiForgeryToken]
        public IActionResult ValidateFormula([FromForm] string? formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
                return Ok(new { valid = false, error = "Formül boş olamaz.", position = 0 });

            if (FormulaParser.TryParse(formula, out _, out var err, out var pos))
                return Ok(new { valid = true });

            return Ok(new { valid = false, error = err, position = pos });
        }

        // DataSource'taki stored procedure listesi (builder dropdown'i icin)
        [HttpGet]
        [Route("Admin/SpList")]
        // M-13 R4.1: Logic SpExplorerService.ListAsync'e tasindi (28 Nisan 2026).
        public async Task<IActionResult> SpList(string dataSourceKey)
        {
            var result = await _spExplorer.ListAsync(dataSourceKey);
            return Json(new { success = result.Success, error = result.Error, procedures = result.Procedures });
        }

        // Stored procedure onizleme: SP'yi tip-bazli default'larla calistir; admin override
        // isterse paramsJson query parametresiyle (key = param adi @ prefix'siz, value = string)
        // belirli parametrelerin degerini gecersiz kilar.
        [HttpGet]
        [Route("Admin/SpPreview")]
        // M-13 R4.2: Logic SpExplorerService.PreviewAsync'e tasindi (28 Nisan 2026).
        public async Task<IActionResult> SpPreview(string dataSourceKey, string procName, int maxRows = 10, string? paramsJson = null)
        {
            // Admin override: parametre adi -> string deger haritasi (case-insensitive).
            // paramsJson parse hatasi sessizce gecilir (default'larla devam).
            Dictionary<string, string>? overrides = null;
            if (!string.IsNullOrWhiteSpace(paramsJson))
            {
                try
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(paramsJson);
                    if (parsed != null)
                    {
                        overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var kv in parsed)
                        {
                            if (!string.IsNullOrWhiteSpace(kv.Key)) overrides[kv.Key.TrimStart('@')] = kv.Value ?? "";
                        }
                    }
                }
                catch { /* gecersiz JSON -> default'larla devam */ }
            }

            var result = await _spExplorer.PreviewAsync(dataSourceKey, procName, maxRows, overrides);
            return Json(new { success = result.Success, error = result.Error, resultSets = result.ResultSets });
        }
    }
}
