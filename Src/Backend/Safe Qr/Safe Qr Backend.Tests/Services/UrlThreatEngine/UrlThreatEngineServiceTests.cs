using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Services.UrlThreatEngine;

namespace Safe_Qr_Backend.Tests.Services.UrlThreatEngine
{
    public class UrlThreatEngineServiceTests
    {
        private readonly UrlThreatEngineService _sut = new();

        [Theory]
        [InlineData("not-a-valid-url", ServiceResultEnum.malicious, "malformed Url")]
        [InlineData("/just/a/relative/path", ServiceResultEnum.malicious, "malformed Url")]
        [InlineData("https://example.com/", ServiceResultEnum.safe, "none of the engine got flagged")]
        public void EvaluateUrl_MalformedUrlCheck(string url, ServiceResultEnum expected, string expectedReasonSubstring)
        {
            var result = _sut.EvaluateUrl(url);

            Assert.Equal(VendorEnum.InHouse, result.Vendor);
            Assert.Equal(expected, result.ServiceResult);
            var reason = Assert.Single(result.Reasons);
            Assert.Contains(expectedReasonSubstring, reason);
        }

        [Theory]
        [InlineData("ftp://example.com/", ServiceResultEnum.highRisk, "not http or https")]
        [InlineData("mailto:test@example.com", ServiceResultEnum.highRisk, "not http or https")]
        [InlineData("http://example.com/", ServiceResultEnum.safe, "none of the engine got flagged")]
        [InlineData("https://example.com/", ServiceResultEnum.safe, "none of the engine got flagged")]
        public void EvaluateUrl_SchemeCheck(string url, ServiceResultEnum expected, string expectedReasonSubstring)
        {
            var result = _sut.EvaluateUrl(url);

            Assert.Equal(VendorEnum.InHouse, result.Vendor);
            Assert.Equal(expected, result.ServiceResult);
            var reason = Assert.Single(result.Reasons);
            Assert.Contains(expectedReasonSubstring, reason);
        }

        [Theory]
        [InlineData("https://user:pass@example.com/", ServiceResultEnum.malicious, "user info redirect")]
        [InlineData("https://accounts.google.com@evil.com/login", ServiceResultEnum.malicious, "user info redirect")]
        [InlineData("https://example.com/", ServiceResultEnum.safe, "none of the engine got flagged")]
        public void EvaluateUrl_UserInfoCheck(string url, ServiceResultEnum expected, string expectedReasonSubstring)
        {
            var result = _sut.EvaluateUrl(url);

            Assert.Equal(VendorEnum.InHouse, result.Vendor);
            Assert.Equal(expected, result.ServiceResult);
            var reason = Assert.Single(result.Reasons);
            Assert.Contains(expectedReasonSubstring, reason);
        }

        [Theory]
        [InlineData("http://192.168.1.1/", ServiceResultEnum.malicious, "Host type is raw ip address")]
        [InlineData("http://[2001:db8::1]/", ServiceResultEnum.malicious, "Host type is raw ip address")]
        [InlineData("http://example.com/", ServiceResultEnum.safe, "none of the engine got flagged")]
        [InlineData("http://localhost/", ServiceResultEnum.safe, "none of the engine got flagged")]
        public void EvaluateUrl_RawIpHostCheck(string url, ServiceResultEnum expected, string expectedReasonSubstring)
        {
            var result = _sut.EvaluateUrl(url);

            Assert.Equal(VendorEnum.InHouse, result.Vendor);
            Assert.Equal(expected, result.ServiceResult);
            var reason = Assert.Single(result.Reasons);
            Assert.Contains(expectedReasonSubstring, reason);
        }

        [Theory]
        [InlineData("https://example.com/page?redirect=http://evil.com", ServiceResultEnum.highRisk)]
        [InlineData("https://example.com/page?url=evil.com", ServiceResultEnum.highRisk)]
        [InlineData("https://example.com/page?next=http://evil.com", ServiceResultEnum.highRisk)]
        [InlineData("https://example.com/page?continue=http://evil.com", ServiceResultEnum.highRisk)]
        [InlineData("https://example.com/page?returnUrl=http://evil.com", ServiceResultEnum.highRisk)]
        [InlineData("https://example.com/page?dest=http://evil.com", ServiceResultEnum.highRisk)]
        [InlineData("https://example.com/page?redirect=home", ServiceResultEnum.safe)]
        [InlineData("https://example.com/page?foo=bar", ServiceResultEnum.safe)]
        [InlineData("https://example.com/page", ServiceResultEnum.safe)]
        public void EvaluateUrl_RedirectQueryParamCheck(string url, ServiceResultEnum expected)
        {
            var result = _sut.EvaluateUrl(url);

            Assert.Equal(VendorEnum.InHouse, result.Vendor);
            Assert.Equal(expected, result.ServiceResult);
            var reason = Assert.Single(result.Reasons);

            if (expected == ServiceResultEnum.highRisk)
            {
                Assert.Contains("Query param", reason);
                Assert.Contains("URL-like value", reason);
            }
            else
            {
                Assert.Equal("none of the engine got flagged", reason);
            }
        }
    }
}
