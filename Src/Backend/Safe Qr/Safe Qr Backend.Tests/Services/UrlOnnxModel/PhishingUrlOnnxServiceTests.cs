using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.ML.OnnxRuntime;
using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Services;

namespace Safe_Qr_Backend.Tests.Services.UrlOnnxModel
{
    // Loads the real trained model once and shares the InferenceSession across
    // every test in the class — construction does real (slow) I/O + graph load.
    public class OnnxInferenceSessionFixture : IDisposable
    {
        public InferenceSession Session { get; }

        public OnnxInferenceSessionFixture()
        {
            var modelPath = Path.Combine(AppContext.BaseDirectory, "Models", "model.onnx");
            var options = new SessionOptions
            {
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                IntraOpNumThreads = 1
            };
            Session = new InferenceSession(modelPath, options);
        }

        public void Dispose() => Session.Dispose();
    }

    public class PhishingUrlOnnxServiceTests : IClassFixture<OnnxInferenceSessionFixture>
    {
        private readonly Phishing_Url_ONNX _sut;

        public PhishingUrlOnnxServiceTests(OnnxInferenceSessionFixture fixture)
        {
            _sut = new Phishing_Url_ONNX(fixture.Session);
        }

        [Theory]
        [InlineData("http://example.com")]
        [InlineData("https://www.google.com/search?q=test")]
        [InlineData("http://192.168.1.50/secure-login/verify-account.php?redirect=http://evil.test")]
        public async Task Predict_RunsRealInference_BucketsConsistentlyWithThresholds(string url)
        {
            var result = await _sut.Predict(url);

            Assert.Equal(VendorEnum.ONNX, result.Vendor);
            var reason = Assert.Single(result.Reasons);

            // We don't know in advance what the real trained model will output for
            // a given URL, so rather than asserting a specific bucket, cross-check
            // that whatever probability it actually produced was bucketed the same
            // way the pure threshold logic buckets it. This exercises the real
            // model end-to-end while staying deterministic regardless of model weights.
            var phishingProb = ParseProbabilityFromReason(reason);
            var expected = Phishing_Url_ONNX.GetServiceResultWithProb(phishingProb, VendorEnum.ONNX);

            Assert.Equal(expected.ServiceResult, result.ServiceResult);
        }

        private static float ParseProbabilityFromReason(string reason)
        {
            var match = Regex.Match(reason, @"(?<value>[-+]?[0-9][0-9.,]*)%");
            Assert.True(match.Success, $"Could not find a probability percentage in reason text: '{reason}'");

            var percent = float.Parse(match.Groups["value"].Value, NumberStyles.Float, CultureInfo.CurrentCulture);
            return percent / 100f;
        }
    }
}
