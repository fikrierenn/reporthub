using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mosaik.Core.Logging;
using Mosaik.Core.Lookup;
using Mosaik.Core.Workflow;
using Mosaik.Modules.Circular.Services;
using System.Text.Json;

namespace Mosaik.Modules.Circular.Areas.Circular.Controllers
{
    // Plan 17 Faz C — Circular okuma sayfaları (tüm Mosaik kullanıcıları erişebilir).
    // Detail sayfa ziyaretinde IAuditLog "circular_read" event'i otomatik yazılır.
    [Area("Circular")]
    [Authorize]
    public class CircularController : Controller
    {
        private readonly CircularService _service;
        private readonly ILookupService _lookup;
        private readonly BlockFileService _dosya;
        private readonly IAuditLog _audit;
        private readonly IEntityWorkflowProvider _workflows;

        public CircularController(
            CircularService service,
            ILookupService lookup,
            BlockFileService file,
            IAuditLog audit,
            IEntityWorkflowProvider workflows)
        {
            _service = service;
            _lookup = lookup;
            _dosya = file;
            _audit = audit;
            _workflows = workflows;
        }

        public async Task<IActionResult> Index()
        {
            var circulars = await _service.ListAsync(sonNGun: 30);
            return View(circulars);
        }

        public async Task<IActionResult> Details(int id)
        {
            var circular = await _service.GetAsync(id);
            if (circular == null) return NotFound();

            var blocks = await _service.GetBlocksAsync(id);
            ViewBag.Blocks = blocks;
            ViewBag.BlockTypeMap = (await _lookup.GetValuesAsync("blockType"))
                .ToDictionary(v => v.Id, v => v.Label);

            // Bloklara bağlı dosyaları toplu çek (BlockId → List<BlockFile>)
            var blockIds = blocks.Select(b => b.Id).ToList();
            var files = await _dosya.ListByBlockIdsAsync(blockIds);
            ViewBag.FileMap = files
                .GroupBy(d => d.BlockId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Audit log: "circular_read" — kullanıcı sayfa ziyaret etti
            await _service.TrackReadAsync(id);

            // Plan 36 — bu Circular'a bağlı workflow akışları
            ViewBag.Workflows = await _workflows.GetForEntityAsync("Circular", id);

            return View(circular);
        }

        // GET /Circular/Circular/Print/{id} — yazdırma için sade HTML (browser print → PDF)
        public async Task<IActionResult> Print(int id)
        {
            var circular = await _service.GetAsync(id);
            if (circular == null) return NotFound();

            var blocks = await _service.GetBlocksAsync(id);
            var blockIds = blocks.Select(b => b.Id).ToList();
            var files = await _dosya.ListByBlockIdsAsync(blockIds);
            ViewBag.Blocks = blocks;
            ViewBag.FileMap = files.GroupBy(d => d.BlockId).ToDictionary(g => g.Key, g => g.ToList());
            ViewBag.BlockTypeMap = (await _lookup.GetValuesAsync("blockType"))
                .ToDictionary(v => v.Id, v => v.Label);

            await _audit.LogAsync(
                eventType: "circular_export_print",
                targetType: "circular",
                targetKey: id.ToString());

            return View(circular);
        }

        // GET /Circular/Circular/ExportXlsx/{id} — Excel: tamim + bloklar
        public async Task<IActionResult> ExportXlsx(int id)
        {
            var circular = await _service.GetAsync(id);
            if (circular == null) return NotFound();

            var blocks = await _service.GetBlocksAsync(id);
            var typeMap = (await _lookup.GetValuesAsync("blockType")).ToDictionary(v => v.Id, v => v.Label);

            using var wb = new XLWorkbook();

            // Sheet 1: Tamim Bilgileri
            var infoSheet = wb.Worksheets.Add("Tamim");
            infoSheet.Cell(1, 1).Value = "Tamim No";
            infoSheet.Cell(1, 2).Value = circular.CircularNumber;
            infoSheet.Cell(2, 1).Value = "Başlık";
            infoSheet.Cell(2, 2).Value = circular.Title;
            infoSheet.Cell(3, 1).Value = "Tarih";
            infoSheet.Cell(3, 2).Value = circular.CircularDate.ToString("dd.MM.yyyy");
            infoSheet.Cell(4, 1).Value = "Yayın";
            infoSheet.Cell(4, 2).Value = circular.PublishedAt.ToString("dd.MM.yyyy HH:mm");
            infoSheet.Cell(5, 1).Value = "Blok Sayısı";
            infoSheet.Cell(5, 2).Value = blocks.Count;

            // AI özet (varsa)
            if (!string.IsNullOrWhiteSpace(circular.AiSummaryJson))
            {
                infoSheet.Cell(7, 1).Value = "AI Özet";
                infoSheet.Cell(7, 1).Style.Font.Bold = true;
                int row = 8;
                try
                {
                    using var doc = JsonDocument.Parse(circular.AiSummaryJson);
                    if (doc.RootElement.TryGetProperty("summary", out var s) && s.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in s.EnumerateArray())
                        {
                            infoSheet.Cell(row++, 1).Value = "•";
                            infoSheet.Cell(row - 1, 2).Value = item.GetString();
                        }
                    }
                }
                catch { /* malformed JSON, skip */ }
            }

            infoSheet.Column(1).Width = 18;
            infoSheet.Column(2).Width = 80;
            infoSheet.Range(1, 1, 5, 1).Style.Font.Bold = true;

            // Sheet 2: Bloklar
            var blockSheet = wb.Worksheets.Add("Bloklar");
            blockSheet.Cell(1, 1).Value = "Sıra";
            blockSheet.Cell(1, 2).Value = "Blok No";
            blockSheet.Cell(1, 3).Value = "Tür";
            blockSheet.Cell(1, 4).Value = "Departman";
            blockSheet.Cell(1, 5).Value = "Konu";
            blockSheet.Cell(1, 6).Value = "Acil";
            blockSheet.Cell(1, 7).Value = "İçerik";
            blockSheet.Range(1, 1, 1, 7).Style.Font.Bold = true;
            blockSheet.Range(1, 1, 1, 7).Style.Fill.BackgroundColor = XLColor.LightGray;

            int r = 2;
            foreach (var b in blocks.OrderByDescending(x => x.IsUrgent).ThenBy(x => x.Id))
            {
                typeMap.TryGetValue(b.BlockTypeId, out var typeLabel);
                blockSheet.Cell(r, 1).Value = r - 1;
                blockSheet.Cell(r, 2).Value = b.BlockNumber;
                blockSheet.Cell(r, 3).Value = typeLabel ?? "";
                blockSheet.Cell(r, 4).Value = b.Department;
                blockSheet.Cell(r, 5).Value = b.Subject;
                blockSheet.Cell(r, 6).Value = b.IsUrgent ? "EVET" : "";
                blockSheet.Cell(r, 7).Value = StripHtml(b.Content);
                if (b.IsUrgent) blockSheet.Range(r, 1, r, 7).Style.Fill.BackgroundColor = XLColor.LightSalmon;
                r++;
            }

            blockSheet.Column(1).Width = 6;
            blockSheet.Column(2).Width = 18;
            blockSheet.Column(3).Width = 12;
            blockSheet.Column(4).Width = 18;
            blockSheet.Column(5).Width = 40;
            blockSheet.Column(6).Width = 8;
            blockSheet.Column(7).Width = 80;
            blockSheet.Range(1, 1, blocks.Count + 1, 7).Style.Alignment.WrapText = true;
            blockSheet.SheetView.FreezeRows(1);

            await _audit.LogAsync(
                eventType: "circular_export_xlsx",
                targetType: "circular",
                targetKey: id.ToString());

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            var fileName = $"{circular.CircularNumber}.xlsx";
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private static string StripHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html)) return "";
            var t = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
            t = System.Net.WebUtility.HtmlDecode(t);
            return System.Text.RegularExpressions.Regex.Replace(t, @"\s+", " ").Trim();
        }
    }
}
