namespace Mosaik.Modules.Kvkk.Entities
{
    // Plan 40 (M6) — Process × DataElement junction (M-M). Reverse lookup hot path:
    // IX_PDL_Element(DataElementId). UsageType: 0 collects 1 stores 2 transfers 3 derives.
    public class ProcessDataLink
    {
        public int Id { get; set; }
        public int ProcessId { get; set; }
        public int DataElementId { get; set; }
        public byte UsageType { get; set; }
        public string? Notes { get; set; }

        public KvkkProcess? Process { get; set; }
        public DataElement? DataElement { get; set; }
    }
}
