using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Entities
{
    public class ThreatFeedback
    {
        public int Id { get; set; }

        /// <summary>Null when the report was submitted anonymously (no login required to report).</summary>
        public int? UserId { get; set; }
        public User? User { get; set; }

        public required string Payload { get; set; }
        public required string PayloadType { get; set; }

        /// <summary>Snapshot of what the system classified this as, at the time of reporting.</summary>
        public required AggregatedFinalResult SystemClassification { get; set; }

        /// <summary>What the reporting user believes the correct classification should be.</summary>
        public required ServiceResultEnum ReportedRiskLevel { get; set; }

        public string? Comment { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public uint RowVersion { get; set; }
    }
}
