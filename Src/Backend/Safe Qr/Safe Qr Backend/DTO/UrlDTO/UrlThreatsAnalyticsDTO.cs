namespace Safe_Qr_Backend.DTO.UrlDTO
{
    public class UrlThreatsAnalyticsDTO{
        public List<UrlThreatBucketDTO> DailyBreakdowns { get; set; } = new();
        public int TotalScanned { get; set; }
        public int MaliciousCount { get; set; }
    }
}
