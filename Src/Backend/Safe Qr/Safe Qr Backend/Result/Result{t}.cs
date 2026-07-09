using System.Security;

namespace Safe_Qr_Backend.Result
{
    public class Result<T> : Result
    {

        public T? Value { get; set; }
        public Result(T? value, bool isSucceeded, ResultEnum Reasons) : base ( isSucceeded, Reasons)
        {
            Value = value;
        }

        public static Result<T> Succeeded(T value, ResultEnum Reasons)
        {
            return new Result<T>(value, true , Reasons);
        }

        public static new Result<T> Failure(ResultEnum Reasons)
        {
            return new Result<T>(default, false, Reasons);
        }

        public static Result<T> Failure(T? value,ResultEnum Reasons)
        {
            return new Result<T>(value, false, Reasons);
        }
    }
}
