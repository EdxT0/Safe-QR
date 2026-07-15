using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Entities
{
    public class UrlReport
    {

        public int Id { get; set; }
        public required string Url { get; set; }
        public required AggregatedFinalResult Results { get; set; } 

        public bool FlaggedForWrong { get; set; } = false;

        public uint RowVersion { get; set; }

    }
}
