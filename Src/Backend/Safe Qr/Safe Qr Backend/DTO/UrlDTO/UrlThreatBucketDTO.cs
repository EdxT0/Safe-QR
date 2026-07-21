using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.DTO.UrlDTO
{
    public class UrlThreatBucketDTO
    {
        public DateTime Date { get; set; }
        public ServiceResultEnum Result { get; set; }
        public int Count { get; set; }
    }
}
