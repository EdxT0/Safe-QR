using Safe_Qr_Backend.Result;
using System.ComponentModel.DataAnnotations;

namespace Safe_Qr_Backend.Entities
{
    public class UrlReport
    {

        public int Id { get; set; }
        public required string Url { get; set; }
        public required List<ServiceResult> Results { get; set; } = new List<ServiceResult>();

        public bool FlaggedForWrong { get; set; } = false;

        public uint RowVersion { get; set; }

    }
}
