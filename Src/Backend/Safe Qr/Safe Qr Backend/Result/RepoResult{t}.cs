using System.Security;

namespace Safe_Qr_Backend.Result
{
    public class RepoResult<T> : RepoResult
    {

        public T? Value { get; set; }
        public RepoResultEnum Reasons { get; set; }
        public RepoResult(T? value, bool isSucceeded, RepoResultEnum Reasons) : base ( isSucceeded, Reasons)
        {
            Value = value;
        }

        public static RepoResult<T> Succeeded(T value, RepoResultEnum Reasons)
        {
            return new RepoResult<T>(value, true , Reasons);
        }

        public static new RepoResult<T> Failure(RepoResultEnum Reasons)
        {
            return new RepoResult<T>(default, false, Reasons);
        }
    }
}
