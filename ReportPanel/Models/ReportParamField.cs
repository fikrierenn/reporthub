namespace ReportPanel.Models
{
    public class ReportParamField
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
        public bool Required { get; set; }
        public string Placeholder { get; set; } = string.Empty;
        public string HelpText { get; set; } = string.Empty;
        public string DefaultValue { get; set; } = string.Empty;
    }
}
