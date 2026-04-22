using System.Text.Json.Serialization;

namespace ReportPanel.Models
{
    public class DashboardConfig
    {
        [JsonPropertyName("layout")]
        public string Layout { get; set; } = "standard";

        [JsonPropertyName("tabs")]
        public List<DashboardTab> Tabs { get; set; } = new();
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
        [JsonPropertyName("type")]
        public string Type { get; set; } = "kpi"; // kpi, chart, table

        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("span")]
        public int Span { get; set; } = 1; // 1-4 grid span

        [JsonPropertyName("color")]
        public string Color { get; set; } = "blue";

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = "fas fa-chart-bar";

        // KPI fields
        [JsonPropertyName("resultSet")]
        public int ResultSet { get; set; }

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
