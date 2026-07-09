namespace Safe_Qr_Backend.Result
{
    public class Result
    {

        public bool IsSucceeded { get; set; }
        public ResultEnum Reasons { get; set; }

        public Result(bool isSucceeded, ResultEnum reasons) {
            IsSucceeded = isSucceeded;
            Reasons = reasons;
        }

        public static Result Succeeded(ResultEnum reasons) {
            return new Result(true, reasons);
        }

        public static Result Failure(ResultEnum reasons)
        {
            return new Result(false, reasons);

        }
    }
}
