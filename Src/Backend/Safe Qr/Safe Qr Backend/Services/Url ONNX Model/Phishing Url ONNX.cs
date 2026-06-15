using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Safe_Qr_Backend.DTO;
namespace Safe_Qr_Backend.Services
{
    public class Phishing_Url_ONNX
    {


        private readonly InferenceSession _session;

        public Phishing_Url_ONNX(InferenceSession session){
            _session = session;
        }

        public IReadOnlyList<ONNXPhishingResult> Predict(IReadOnlyList<string> urls)
        {
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
            }

            return predictions;
        }
    }
}
