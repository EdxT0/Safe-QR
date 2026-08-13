using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Services;

namespace Safe_Qr_Backend.Tests.Services.UrlOnnxModel
{
    public class PhishingUrlOnnxBucketingTests
    {
        private const float Epsilon = 0.0001f;

        [Theory]
        [InlineData(0f, ServiceResultEnum.safe)]
        [InlineData(Phishing_Url_ONNX.SafeMaxThreshold, ServiceResultEnum.safe)]
        [InlineData(Phishing_Url_ONNX.SafeMaxThreshold + Epsilon, ServiceResultEnum.suspicious)]
        [InlineData(0.5f, ServiceResultEnum.suspicious)]
        [InlineData(Phishing_Url_ONNX.SuspiciousMaxThreshold, ServiceResultEnum.suspicious)]
        [InlineData(Phishing_Url_ONNX.SuspiciousMaxThreshold + Epsilon, ServiceResultEnum.highRisk)]
        [InlineData(0.7f, ServiceResultEnum.highRisk)]
        [InlineData(Phishing_Url_ONNX.HighRiskMaxThreshold - Epsilon, ServiceResultEnum.highRisk)]
        [InlineData(Phishing_Url_ONNX.HighRiskMaxThreshold, ServiceResultEnum.malicious)]
        [InlineData(1f, ServiceResultEnum.malicious)]
        public void GetServiceResultWithProb_BucketsByThreshold(float phishingProb, ServiceResultEnum expected)
        {
            var result = Phishing_Url_ONNX.GetServiceResultWithProb(phishingProb, VendorEnum.ONNX);

            Assert.Equal(expected, result.ServiceResult);
            Assert.Equal(VendorEnum.ONNX, result.Vendor);
            var reason = Assert.Single(result.Reasons);
            Assert.Contains("ONNX Model Phishing probability is around", reason);
        }
    }
}
