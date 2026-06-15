using Safe_Qr_Backend.DTO;

namespace Safe_Qr_Backend.Services
{
    public class EvaluateUrlService
    {
        private readonly Phishing_Url_ONNX _phishing_Url_ONNX;


        public EvaluateUrlService(Phishing_Url_ONNX phishing_Url_ONNX)
        {
            _phishing_Url_ONNX = phishing_Url_ONNX;
        }

        public IReadOnlyList<ONNXPhishingResult> EvaluateUrl( IReadOnlyList<String> urls)
        {
            
            var ONNXResult = _phishing_Url_ONNX.Predict(urls);

            return ONNXResult;
        }
    }
}
