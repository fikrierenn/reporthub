using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReportPanel.Models
{
    // Plan 05 B: JsonExtensionData — frontend'in eklediği typed olmayan alanlar Extra dict'e
    // düşer, save/load sırasında round-trip korunur. Yeni alan ihtiyacı çıktığında frontend
    // serbest ekler; backend o alanı **kullanmak istediğinde** Extra'dan parse eder.
    // Typed alanlar mevcut behavior; Extra "henüz typed olmayan ama frontend'in iletmek
    // istediği her şey" için bypass valve.
    public class DashboardConfig
    {
        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;

        [JsonPropertyName("layout")]
        public string Layout { get; set; } = "standard";

        [JsonPropertyName("resultContract")]
        public Dictionary<string, ResultContractEntry>? ResultContract { get; set; }

        [JsonPropertyName("tabs")]
        public List<DashboardTab> Tabs { get; set; } = new();

        // ADR-008 · Schema v2: Client-side turetilmis alan (AST-sandbox parser F-8'de).
        // Widget column/labelColumn/datasets referansina katilabilir.
        [JsonPropertyName("calculatedFields")]
        public List<CalculatedField>? CalculatedFields { get; set; }

        // Plan 05 B: schema-by-default + bypass valve.
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }

        // ADR-007 Faz 6: Tek source = comp.Result. Legacy comp.ResultSet int index dal Migration 27 ile kaldirildi.
        //
        // Resolver kurali:
        //   1. comp.Result -> ResultContract entry varsa entry.ResultSet (bounds OK), yoksa "rsN" regex int parse fallback (V2 builder default)
        //   2. binding yok -> null
        //
        // null donen durumda renderer placeholder basar + audit event "dashboard_required_result_missing".
        public int? ResolveResultSet(DashboardComponent comp, int resultSetCount)
        {
            if (string.IsNullOrEmpty(comp.Result))
                return null;

            if (ResultContract != null && ResultContract.TryGetValue(comp.Result, out var entry))
            {
                if (entry.ResultSet < 0 || entry.ResultSet >= resultSetCount)
                    return null;
                return entry.ResultSet;
            }

            // V2 builder default "rsN" pattern fallback — yeni widget'lar `result: "rs0"` ile gelir
            var m = System.Text.RegularExpressions.Regex.Match(comp.Result, @"^rs(\d+)$");
            if (m.Success && int.TryParse(m.Groups[1].Value, out var rsIdx)
                && rsIdx >= 0 && rsIdx < resultSetCount)
                return rsIdx;

            return null;
        }
    }

    public class ResultContractEntry
    {
        [JsonPropertyName("resultSet")]
        public int ResultSet { get; set; }

        [JsonPropertyName("required")]
        public bool Required { get; set; } = false;

        [JsonPropertyName("shape")]
        public string? Shape { get; set; } // "row" | "table" — enforcement Faz 4 (declare now, enforce later)
    }

    public class DashboardTab
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = "Genel";

        [JsonPropertyName("components")]
        public List<DashboardComponent> Components { get; set; } = new();

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }

    public class DashboardComponent
    {
        // ADR-007: Stabil widget id (auto-gen on first save). Immutable once set.
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "kpi"; // kpi, chart, table — unknown types render as "removed widget" placeholder

        // ADR-008 · v2: Alt-tip (KPI: basic/delta/sparkline/progress | chart: line/area/bar/hbar/stacked/pie/doughnut/radar/polarArea/scatter)
        // Table icin yok. Null ise legacy: kpi->basic, chart->ChartType alanindan kopyalanir (Migration 18 v1->v2).
        [JsonPropertyName("variant")]
        public string? Variant { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("span")]
        public int Span { get; set; } = 1; // 1-4 grid span

        // ADR-007: Name-based binding. Tek source (Faz 6 sonrasi: legacy ResultSet int kaldirildi).
        [JsonPropertyName("result")]
        public string? Result { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; } = "blue";

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = "fas fa-chart-bar";

        [JsonPropertyName("agg")]
        public string Agg { get; set; } = "count"; // count, sum, avg, min, max, first, countWhere

        [JsonPropertyName("column")]
        public string? Column { get; set; }

        [JsonPropertyName("condition")]
        public string? Condition { get; set; } // notNull, isNull

        [JsonPropertyName("subtitle")]
        public string? Subtitle { get; set; }

        // ADR-008 v2: Sayi formati (KPI + chart Y ekseni). auto/currency/currency-short/percent/decimal2
        [JsonPropertyName("numberFormat")]
        public string? NumberFormat { get; set; }

        // KPI variant-spesifik alt-config'ler (sadece ilgili variant'ta anlamli)
        [JsonPropertyName("delta")]
        public DeltaConfig? Delta { get; set; }

        [JsonPropertyName("trend")]
        public TrendConfig? Trend { get; set; }

        [JsonPropertyName("progress")]
        public ProgressConfig? Progress { get; set; }

        // Chart fields
        [JsonPropertyName("chartType")]
        public string ChartType { get; set; } = "line"; // line, bar, doughnut, pie — v2'de variant'a tasindi, legacy

        [JsonPropertyName("labelColumn")]
        public string? LabelColumn { get; set; }

        [JsonPropertyName("datasets")]
        public List<ChartDataset>? Datasets { get; set; }

        // ADR-008 v2: Chart eksen ve gorunum secenekleri
        [JsonPropertyName("axisOptions")]
        public AxisOptions? AxisOptions { get; set; }

        // Table fields
        [JsonPropertyName("columns")]
        public List<TableColumnDef>? Columns { get; set; }

        [JsonPropertyName("clickDetail")]
        public bool ClickDetail { get; set; }

        // ADR-008 v2: Tablo ayarlari (toplam satiri, cizgili, sticky baslik, client arama, sayfalama)
        [JsonPropertyName("tableOptions")]
        public TableOptions? TableOptions { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }

    public class ChartDataset
    {
        [JsonPropertyName("column")]
        public string Column { get; set; } = "";

        [JsonPropertyName("label")]
        public string Label { get; set; } = "";

        [JsonPropertyName("color")]
        public string Color { get; set; } = "blue";
    }

    public class TableColumnDef
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = "";

        [JsonPropertyName("label")]
        public string Label { get; set; } = "";

        [JsonPropertyName("align")]
        public string Align { get; set; } = "left"; // left, right, center

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        // ADR-008 v2: Kolon format hint (Metin/Para/Sayi/Tarih/Yuzde). Null ise auto-detect.
        [JsonPropertyName("format")]
        public string? Format { get; set; }

        // F-6 scope: kosullu format (dataBar/colorScale/iconUpDown/negativeRed).
        // Simdilik schema'da declare — renderer tarafi F-6'da.
        [JsonPropertyName("conditionalFormat")]
        public ConditionalFormat? ConditionalFormat { get; set; }

        // Plan 05.B: kolon hesaplı ise formula sözdizimi (FormulaEvaluator AST).
        // Null = SP'den gelen ham kolon. Var = render-time satır-bazlı eval, row[Key] = result.
        // Save-time validator FormulaParser.TryParse ile sözdizimi doğrular.
        [JsonPropertyName("formula")]
        public string? Formula { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; set; }
    }

    // ============================================================
    // ADR-008 · v2 alt-config tipleri
    // ============================================================

    public class AxisOptions
    {
        [JsonPropertyName("showLegend")]
        public bool ShowLegend { get; set; } = true;

        [JsonPropertyName("showGrid")]
        public bool ShowGrid { get; set; } = true;

        [JsonPropertyName("beginAtZero")]
        public bool BeginAtZero { get; set; } = false;

        [JsonPropertyName("tooltip")]
        public bool Tooltip { get; set; } = true;

        [JsonPropertyName("dataLabels")]
        public bool DataLabels { get; set; } = false;

        [JsonPropertyName("smooth")]
        public bool Smooth { get; set; } = true;
    }

    public class TableOptions
    {
        [JsonPropertyName("totalRow")]
        public bool TotalRow { get; set; } = false;

        [JsonPropertyName("stripe")]
        public bool Stripe { get; set; } = true;

        [JsonPropertyName("stickyHeader")]
        public bool StickyHeader { get; set; } = true;

        [JsonPropertyName("clientSearch")]
        public bool ClientSearch { get; set; } = false;

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; } = 0; // 0 = kapalı, tipik: 20/50/100
    }

    public class DeltaConfig
    {
        [JsonPropertyName("compareColumn")]
        public string? CompareColumn { get; set; }

        [JsonPropertyName("compareLabel")]
        public string? CompareLabel { get; set; }
    }

    public class TrendConfig
    {
        [JsonPropertyName("labelColumn")]
        public string? LabelColumn { get; set; }

        [JsonPropertyName("valueColumn")]
        public string? ValueColumn { get; set; }
    }

    public class ProgressConfig
    {
        [JsonPropertyName("targetColumn")]
        public string? TargetColumn { get; set; }

        [JsonPropertyName("targetValue")]
        public double? TargetValue { get; set; } // static hedef (kolon yerine)
    }

    public class ConditionalFormat
    {
        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "none"; // none/dataBar/colorScale/iconUpDown/negativeRed

        [JsonPropertyName("color")]
        public string? Color { get; set; } // blue/green/red/... (brand palette)
    }

    public class CalculatedField
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = ""; // camelCase, unique

        [JsonPropertyName("formula")]
        public string Formula { get; set; } = ""; // örn. "BugunCiro - GecenYilBugun"

        [JsonPropertyName("format")]
        public string? Format { get; set; } // auto/currency/percent/decimal2

        [JsonPropertyName("resultScope")]
        public string? ResultScope { get; set; } // hangi resultSet'e uygulanir (isim veya null = tum kullanimlar)
    }
}
