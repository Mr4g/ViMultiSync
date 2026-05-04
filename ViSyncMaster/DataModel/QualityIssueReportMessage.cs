using System;
using ViSyncMaster.Entitys;

namespace ViSyncMaster.DataModel
{
    public class QualityIssueReportMessage : MachineStatus
    {
        public QualityIssueReportMessage()
        {
            Name = "S11.QualityIssueReport";
            Status = "QUALITY_REPORT_ONLY";
            Value = "true";
            IsQualityReportOnly = true;
        }

        public string? AppMode { get; set; }
        public string? ActiveProductNumber { get; set; }
        public string? ElementType { get; set; }
        public string? QualityReason { get; set; }
        public string? CustomDescription { get; set; }
        public string? EventType { get; set; }
        public string? ReportNature { get; set; }
        public DateTime TimestampUtc { get; set; }
        public bool IsQualityReportOnly { get; set; }
    }
}
