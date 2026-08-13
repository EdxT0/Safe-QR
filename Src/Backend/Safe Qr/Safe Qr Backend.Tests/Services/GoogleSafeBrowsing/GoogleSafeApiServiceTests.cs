using System.Net;
using Google.Protobuf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using Safe_Qr_Backend.DTO.GoogleSafeBrowsingDTO;
using Safe_Qr_Backend.Protos.SafeBrowsing;
using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Services.Google_Safe_Browsing;

namespace Safe_Qr_Backend.Tests.Services.GoogleSafeBrowsing
{
    public class GoogleSafeApiServiceTests
    {
        private const string BaseUrl = "https://fake-safebrowsing.test";

        private static GoogleSafeApiService CreateSut(MockHttpMessageHandler mockHttp)
        {
            var httpClient = new HttpClient(mockHttp) { BaseAddress = new Uri(BaseUrl) };
            var options = Options.Create(new SafeBrowsingOptions { ApiKey = "test-api-key", BaseUrl = BaseUrl });
            var config = new ConfigurationBuilder().Build();

            return new GoogleSafeApiService(httpClient, config, NullLogger<GoogleSafeApiService>.Instance, options);
        }

        [Fact]
        public async Task EvaluateUrlAsync_SuccessfulResponse_NoThreats_ReturnsSafe()
        {
            var mockHttp = new MockHttpMessageHandler();
            var response = new SearchUrlsResponse();
            mockHttp.When(HttpMethod.Get, $"{BaseUrl}/v5/urls:search*")
                    .Respond(HttpStatusCode.OK, new ByteArrayContent(response.ToByteArray()));

            var sut = CreateSut(mockHttp);

            var result = await sut.EvaluateUrlAsync("https://example.com", CancellationToken.None);

            Assert.Equal(VendorEnum.Google, result.Vendor);
            Assert.Equal(ServiceResultEnum.safe, result.ServiceResult);
            Assert.Equal(new[] { "no threats found" }, result.Reasons);
        }

        [Fact]
        public async Task EvaluateUrlAsync_SuccessfulResponse_WithThreats_ReturnsMaliciousWithDistinctThreatTypes()
        {
            var mockHttp = new MockHttpMessageHandler();
            var response = new SearchUrlsResponse
            {
                Threats =
                {
                    new ThreatUrl { Url = "https://evil.example.com", ThreatTypes = { ThreatType.Malware, ThreatType.SocialEngineering } },
                    new ThreatUrl { Url = "https://evil.example.com/other", ThreatTypes = { ThreatType.Malware } },
                }
            };
            mockHttp.When(HttpMethod.Get, $"{BaseUrl}/v5/urls:search*")
                    .Respond(HttpStatusCode.OK, new ByteArrayContent(response.ToByteArray()));

            var sut = CreateSut(mockHttp);

            var result = await sut.EvaluateUrlAsync("https://evil.example.com", CancellationToken.None);

            Assert.Equal(VendorEnum.Google, result.Vendor);
            Assert.Equal(ServiceResultEnum.malicious, result.ServiceResult);
            // Malware appears on both threat entries — recognizedThreatTypes is de-duplicated via .Distinct().
            Assert.Equal(new[] { "Malware", "SocialEngineering" }, result.Reasons);
        }

        [Fact]
        public async Task EvaluateUrlAsync_ErrorResponse_ReturnsHighRisk()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, $"{BaseUrl}/v5/urls:search*")
                    .Respond(HttpStatusCode.InternalServerError, "text/plain", "server exploded");

            var sut = CreateSut(mockHttp);

            var result = await sut.EvaluateUrlAsync("https://example.com", CancellationToken.None);

            Assert.Equal(VendorEnum.Google, result.Vendor);
            Assert.Equal(ServiceResultEnum.highRisk, result.ServiceResult);
            Assert.Equal(new[] { "Safe Browsing returned an error." }, result.Reasons);
        }

        [Fact]
        public async Task EvaluateUrlAsync_Timeout_ThrowsAndIsNotSwallowed()
        {
            // MockHttp 7.1.0 has no cancellation-aware Respond overload or .Throw() helper,
            // so rather than racing a real HttpClient.Timeout against an artificial delay
            // (whose success would depend on whether the mock handler even observes the
            // linked cancellation token), we simulate a timeout directly: this is exactly
            // the exception type HttpClient.Timeout produces when it expires.
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, $"{BaseUrl}/v5/urls:search*")
                    .Respond(() => throw new TaskCanceledException("Simulated request timeout"));

            var sut = CreateSut(mockHttp);

            // The service's catch block only handles HttpRequestException — a client-side
            // timeout surfaces as TaskCanceledException and is expected to propagate uncaught.
            await Assert.ThrowsAsync<TaskCanceledException>(
                () => sut.EvaluateUrlAsync("https://example.com", CancellationToken.None));
        }
    }
}
