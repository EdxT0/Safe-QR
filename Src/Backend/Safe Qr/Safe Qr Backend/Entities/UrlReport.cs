namespace Safe_Qr_Backend.Entities
{
    public class UrlReport
    {

        public int Id { get; set; }
        public required string Url { get; set; }
        public required ServiceResult Result { get; set; }

        public bool FlaggedForWrong { get; set; } = false;
    }
}
