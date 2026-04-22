using System.Text.Json.Serialization;

namespace ReportPanel.Models
{
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

        // ADR-007 Faz 1: Widget.Result > Widget.ResultSet precedence + bounds check.
        //
        // Resolver kurali:
        //   1. comp.Result != null        ->  contract hit + bounds OK  ? entry.ResultSet : null
        //   2. comp.ResultSet.HasValue    ->  bounds OK                 ? comp.ResultSet.Value : null
        //   3. hicbir binding yok         ->  null
        //
        // null donen durumda renderer placeholder basar (Faz 4'te audit event eklenecek).
        // -1 sentinel KULLANILMIYOR — contract UI davranisina bagli kalmasin.
        public int? ResolveResultSet(DashboardComponent comp, int resultSetCount)
        {
            // 1. name-based binding
            if (!string.IsNullOrEmpty(comp.Result))
            {
                if (ResultContract == null || !ResultContract.TryGetValue(comp.Result, out var entry))
                    return null; // unknown name
                if (entry.ResultSet < 0 || entry.ResultSet >= resultSetCount)
                    return null; // out of bounds
                return entry.ResultSet;
            }

            // 2. legacy index binding
            if (comp.ResultSet.HasValue)
            {
                var idx = comp.ResultSet.Value;
                if (idx < 0 || idx >= resultSetCount)
                    return null;
                return idx;
            }

            // 3. no binding
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
    }

    public class DashboardComponent
    {
        // ADR-007: Stabil widget id (auto-gen on first save). Immutable once set.
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "kpi"; // kpi, chart, table — unknown types render as "removed widget" placeholder

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("span")]
        public int Span { get; set; } = 1; // 1-4 grid span

        // ADR-007: Name-based binding (preferred). If null, falls back to ResultSet int.
        [JsonPropertyName("result")]
        public string? Result { get; set; }

        [JsonPropertyName("color")]
        public string Color { get; set; } = "blue";

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = "fas fa-chart-bar";

        // ADR-007 (Faz 6'da kaldirilacak): legacy index binding. Result field'i varsa yok sayilir.
        // int? — "hic set edilmemis" ile "0'a set edilmis" ayirt edilebilsin diye nullable.
        [JsonPropertyName("resultSet")]
        public int? ResultSet { get; set; }

        [JsonPropertyName("agg")]
        public string Agg { get; set; } = "count"; // count, sum, avg, min, max, first, countWhere

        [JsonPropertyName("column")]
        public string? Column { get; set; }

        [JsonPropertyName("condition")]
        public string? Condition { get; set; } // notNull, isNull

        [JsonPropertyName("subtitle")]
        public string? Subtitle { get; set; }

        // Chart fields
        [JsonPropertyName("chartType")]
        public string ChartType { get; set; } = "line"; // line, bar, doughnut, pie

        [JsonPropertyName("labelColumn")]
        public string? LabelColumn { get; set; }

        [JsonPropertyName("datasets")]
        public List<ChartDataset>? Datasets { get; set; }

        // Table fields
        [JsonPropertyName("columns")]
        public List<TableColumnDef>? Columns { get; set; }

        [JsonPropertyName("clickDetail")]
        public bool ClickDetail { get; set; }
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
    }
}
