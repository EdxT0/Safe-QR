using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Safe_Qr_Backend.Result;
using System.Runtime.CompilerServices;
namespace Safe_Qr_Backend.Services
{
    public class Phishing_Url_ONNX : IPhishingUrlOnnxService
    {


        private readonly InferenceSession _session;
        private readonly VendorEnum vendor = VendorEnum.ONNX;

        public Phishing_Url_ONNX(InferenceSession session)
        {
            _session = session;
        }

        public async Task<ServiceScanResult> Predict(string url)
        {
            var allServiceResultList = new List<ServiceScanResult>();

            var inputTensor = new DenseTensor<string>(new string[] { url }, new int[] { 1 });
            using var inputOrtValue = OrtValue.CreateFromStringTensor(inputTensor);

            string[] inputNames = ["inputs"];
            string[] outputNames = ["label", "probabilities"];

            using var results = _session.Run(new RunOptions(), inputNames, new[] { inputOrtValue }, outputNames);

            var probSpan = results[1].GetTensorDataAsSpan<float>();


            float legitProb = probSpan[0];
            float phishProb = probSpan[1];

            return GetServiceResultWithProb(phishProb, vendor);



        }

        internal const float SafeMaxThreshold = 0.40f;
        internal const float SuspiciousMaxThreshold = 0.60f;
        internal const float HighRiskMaxThreshold = 0.80f;

        internal static ServiceScanResult GetServiceResultWithProb(float phishingProb, VendorEnum vendor)
        {


            if (phishingProb <= SafeMaxThreshold)
            {
                return new ServiceScanResult(vendor, ServiceResultEnum.safe, [$"ONNX Model Phishing probability is around {phishingProb * 100}%"]);
            }
            else if (phishingProb > SafeMaxThreshold && phishingProb <= SuspiciousMaxThreshold)
            {
                return new ServiceScanResult(vendor, ServiceResultEnum.suspicious, [$"ONNX Model Phishing probability is around {phishingProb * 100}%"]);
            }
            else if (phishingProb > SuspiciousMaxThreshold && phishingProb < HighRiskMaxThreshold)
            {
                return new ServiceScanResult(vendor, ServiceResultEnum.highRisk, [$"ONNX Model Phishing probability is around {phishingProb*100}%"]);
            }
            else
            {
                return new ServiceScanResult(vendor, ServiceResultEnum.malicious, [$"ONNX Model Phishing probability is around {phishingProb * 100}%"]);
            }
        }
    }
}
