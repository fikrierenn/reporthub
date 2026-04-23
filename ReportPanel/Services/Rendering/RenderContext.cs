namespace ReportPanel.Services.Rendering
{
    // M-11 F-2: DashboardRenderer split paylaşılan yardımcıları.
    // Tüm per-widget renderer'lar bu context'ten okur (renk map'leri + Esc).
    // Static helper — M-11 geri kalanı stateless render pattern'ini koruyor.
    internal static class RenderContext
    {
        public static readonly Dictionary<string, (string Bg, string Text, string Border, string Light)> ColorMap = new()
        {
            ["blue"]   = ("bg-blue-600",    "text-blue-600",    "border-blue-200",   "bg-blue-50"),
            ["green"]  = ("bg-emerald-600", "text-emerald-600", "border-emerald-200","bg-emerald-50"),
            ["red"]    = ("bg-red-600",     "text-red-600",     "border-red-200",    "bg-red-50"),
            ["yellow"] = ("bg-amber-500",   "text-amber-600",   "border-amber-200",  "bg-amber-50"),
            ["gray"]   = ("bg-gray-600",    "text-gray-600",    "border-gray-200",   "bg-gray-50"),
            ["indigo"] = ("bg-indigo-600",  "text-indigo-600",  "border-indigo-200", "bg-indigo-50"),
            ["purple"] = ("bg-purple-600",  "text-purple-600",  "border-purple-200", "bg-purple-50"),
        };

        public static readonly Dictionary<string, string> ChartColorHex = new()
        {
            ["blue"]   = "#3b82f6",
            ["green"]  = "#10b981",
            ["red"]    = "#ef4444",
            ["yellow"] = "#f59e0b",
            ["gray"]   = "#6b7280",
            ["indigo"] = "#6366f1",
            ["purple"] = "#a855f7",
        };

        public static (string Bg, string Text, string Border, string Light) GetColor(string color)
            => ColorMap.GetValueOrDefault(color, ColorMap["blue"]);

        public static string Esc(string? text) => System.Net.WebUtility.HtmlEncode(text ?? "");
    }
}
