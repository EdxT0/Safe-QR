using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Entities
{
    public class ScanHistory
    {
        public int Id { get; set; }
        public required int UserId { get; set; }
        public User? User { get; set; }

        public required string Payload { get; set; }
        public required string PayloadType { get; set; }
        public required AggregatedFinalResult Results { get; set; }

        public DateTimeOffset ScannedAt { get; set; } = DateTimeOffset.UtcNow;

        public uint RowVersion { get; set; }
    }
}
