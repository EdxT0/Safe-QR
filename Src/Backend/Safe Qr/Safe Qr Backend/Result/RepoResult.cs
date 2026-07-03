namespace Safe_Qr_Backend.Result
{
    public class RepoResult
    {

        public bool IsSucceeded { get; set; }
        public RepoResultEnum Reasons { get; set; }

        public RepoResult(bool isSucceeded, RepoResultEnum reasons) {
            IsSucceeded = isSucceeded;
            Reasons = reasons;
        }

        public static RepoResult Succeeded(RepoResultEnum reasons) {
            return new RepoResult(true, reasons);
        }

        public static RepoResult Failure(RepoResultEnum reasons)
        {
            return new RepoResult(false, reasons);

        }
    }
}
