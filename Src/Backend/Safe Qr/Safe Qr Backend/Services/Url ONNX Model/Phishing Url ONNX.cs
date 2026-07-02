using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Safe_Qr_Backend.Result;
using System.Runtime.CompilerServices;
namespace Safe_Qr_Backend.Services
{
    public class Phishing_Url_ONNX
    {


        private readonly InferenceSession _session;
        private readonly string vendor = "ONNX Model";

        public Phishing_Url_ONNX(InferenceSession session){
            _session = session;
        }

        public async Task<IReadOnlyList<AllServiceResult>> Predict(IReadOnlyList<string> urls)
        {
            var allServiceResultList = new List<AllServiceResult>();
            var urlArray = urls.ToArray();

            var inputTensor = new DenseTensor<string>(urlArray, [urlArray.Length]);
            using var inputOrtValue = OrtValue.CreateFromStringTensor(inputTensor);

            string[] inputNames = ["inputs"];
            string[] outputNames = ["label", "probabilities"];

            using var results = _session.Run(new RunOptions(), inputNames, new[] { inputOrtValue }, outputNames);

            var probSpan = results[1].GetTensorDataAsSpan<float>();

            var predictions = new List<ONNXPhishingResult>(urls.Count);

            for( int i = 0; i < urls.Count; i++)
            {
                float legitProb = probSpan[i * 2 + 0];
                float phishProb = probSpan[i * 2 + 1];

                predictions.Add(new ONNXPhishingResult(
                    urls[i],
                    phishProb,
                    legitProb,
                    phishProb > 0.6 
                    ));
            }for(int i =0; i < predictions.Count; i++)
            {
                float phishingProb = predictions[i].PhishingProbability;
                allServiceResultList.Add(GetServiceResultWithProb(phishingProb, vendor));
            }

            return allServiceResultList;
        }

        private static AllServiceResult GetServiceResultWithProb(float phishingProb, string vendor)
        {       
            

            if (phishingProb <= 40)
            {
                return new AllServiceResult(vendor, ServiceResultEnum.safe, [$"ONNX Model Phishing probability is around {phishingProb}%"]);
            }
            else if (phishingProb > 40 && phishingProb <= 60)
            {
                return new AllServiceResult(vendor, ServiceResultEnum.suspicious, [$"ONNX Model Phishing probability is around {phishingProb}%"]);
            }
            else if (phishingProb > 60 && phishingProb < 80)
            {
                return new AllServiceResult(vendor, ServiceResultEnum.highRisk, [$"ONNX Model Phishing probability is around {phishingProb}%"]);
            }
            else
            {
                return new AllServiceResult(vendor, ServiceResultEnum.malicious, [$"ONNX Model Phishing probability is around {phishingProb}%"]);
            }
        }
    }
}
