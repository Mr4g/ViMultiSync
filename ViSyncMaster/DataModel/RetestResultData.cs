namespace ViSyncMaster.DataModel
{
    public class RetestResultData
    {
        public string EventType { get; set; } = "RetestResult";
        public string? Source { get; set; }
        public string? TestObject { get; set; }
        public string? TotalAbs { get; set; }
        public string? Date { get; set; }
        public string? Time { get; set; }
        public string? Fault { get; set; }
        public string? FromPoint { get; set; }
        public string? ToPoint { get; set; }
        public string? Value { get; set; }
        public string? MeasurementType { get; set; }
        public string RawFrame { get; set; } = string.Empty;
    }
}
