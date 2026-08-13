using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Services.VirusTotal;

namespace Safe_Qr_Backend.Tests.Services.VirusTotal
{
    public class VirusTotalApiServiceTests
    {
        // Mirrors VirusTotalApiService's private `baseUrl` field (trailing slash included,
        // so interpolated patterns below reproduce its double-slash request URIs).
        private const string BaseUrl = "https://www.virustotal.com/api/v3/";

        private static VirusTotalApiService CreateSut(MockHttpMessageHandler mockHttp)
        {
            var httpClient = new HttpClient(mockHttp);
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["VirusTotal:ApiKey"] = "test-api-key" })
                .Build();

            return new VirusTotalApiService(httpClient, config, NullLogger<VirusTotalApiService>.Instance);
        }

        [Fact]
        public async Task EvaluateUrlAsync_CacheHit_MaliciousStats_ReturnsMalicious()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, $"{BaseUrl}/urls/*")
                    .Respond("application/json", """
                    {
                      "data": {
                        "attributes": {
                          "last_analysis_stats": {
                            "malicious": 5,
                            "suspicious": 1,
                            "harmless": 60,
                            "undetected": 10,
                            "timeout": 0
                          }
                        }
                      }
                    }
                    """);

            var sut = CreateSut(mockHttp);

            var result = await sut.EvaluateUrlAsync("https://example.com", CancellationToken.None);

            Assert.Equal(VendorEnum.VirusTotal, result.Vendor);
            Assert.Equal(ServiceResultEnum.malicious, result.ServiceResult);
            Assert.Equal(new[] { "VirusTotal: 5 engine(s) flagged as malicious" }, result.Reasons);
        }

        [Fact]
        public async Task EvaluateUrlAsync_CacheHit_OnlyUndetected_ReturnsSuspicious()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, $"{BaseUrl}/urls/*")
                    .Respond("application/json", """
                    {
                      "data": {
                        "attributes": {
                          "last_analysis_stats": {
                            "malicious": 0,
                            "suspicious": 0,
                            "harmless": 0,
                            "undetected": 7,
                            "timeout": 0
                          }
                        }
                      }
                    }
                    """);

            var sut = CreateSut(mockHttp);

            var result = await sut.EvaluateUrlAsync("https://example.com", CancellationToken.None);

            Assert.Equal(ServiceResultEnum.suspicious, result.ServiceResult);
            Assert.Equal(new[] { "VirusTotal: 7 engine(s) undetected — no verdict" }, result.Reasons);
        }

        // This test genuinely waits out the production 3s polling delay in
        // GetAnalysisResult (it isn't injectable/mockable), so it's slower
        // than the others (~3s instead of milliseconds).
        [Fact]
        public async Task EvaluateUrlAsync_CacheMiss_SubmitsAndPollsUntilCompleted_ReturnsResult()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, $"{BaseUrl}/urls/*")
                    .Respond(HttpStatusCode.NotFound);

            mockHttp.When(HttpMethod.Post, $"{BaseUrl}/urls")
                    .Respond("application/json", """{ "data": { "id": "u-test-analysis-id", "type": "analysis" } }""");

            mockHttp.When(HttpMethod.Get, $"{BaseUrl}/analyses/*")
                    .Respond("application/json", """
                    {
                      "data": {
                        "attributes": {
                          "status": "completed",
                          "stats": { "malicious": 0, "suspicious": 0, "harmless": 55, "undetected": 3, "timeout": 0 }
                        }
                      }
                    }
                    """);

            var sut = CreateSut(mockHttp);

            var result = await sut.EvaluateUrlAsync("https://example.com", CancellationToken.None);

            Assert.Equal(ServiceResultEnum.safe, result.ServiceResult);
            Assert.Equal(new[] { "VirusTotal: 55 engine(s) confirmed safe" }, result.Reasons);
        }

        // Exercises the full 5-attempt polling loop with none of them ever reporting
        // "completed" — GetAnalysisResult's Task.Delay(3000) is not injectable, so this
        // genuinely waits out all 5 attempts (~15s). Tagged Slow so it can be filtered
        // out of quick local runs, e.g. `dotnet test --filter "Category!=Slow"`.
        [Fact]
        [Trait("Category", "Slow")]
        public async Task EvaluateUrlAsync_CacheMiss_PollingNeverCompletes_ReturnsSuspiciousFallback()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, $"{BaseUrl}/urls/*")
                    .Respond(HttpStatusCode.NotFound);

            mockHttp.When(HttpMethod.Post, $"{BaseUrl}/urls")
                    .Respond("application/json", """{ "data": { "id": "u-test-analysis-id", "type": "analysis" } }""");

            mockHttp.When(HttpMethod.Get, $"{BaseUrl}/analyses/*")
                    .Respond("application/json", """
                    {
                      "data": {
                        "attributes": {
                          "status": "queued",
                          "stats": { "malicious": 0, "suspicious": 0, "harmless": 0, "undetected": 0, "timeout": 0 }
                        }
                      }
                    }
                    """);

            var sut = CreateSut(mockHttp);

            var result = await sut.EvaluateUrlAsync("https://example.com", CancellationToken.None);

            Assert.Equal(VendorEnum.VirusTotal, result.Vendor);
            Assert.Equal(ServiceResultEnum.suspicious, result.ServiceResult);
            Assert.Equal(new[] { "Virus Total API didn't manage to get result" }, result.Reasons);
        }

        [Fact]
        public async Task EvaluateUrlAsync_ErrorResponse_ThrowsHttpRequestException()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, $"{BaseUrl}/urls/*")
                    .Respond(HttpStatusCode.InternalServerError);

            var sut = CreateSut(mockHttp);

            await Assert.ThrowsAsync<HttpRequestException>(
                () => sut.EvaluateUrlAsync("https://example.com", CancellationToken.None));
        }

        [Fact]
        public async Task EvaluateUrlAsync_Timeout_ThrowsAndIsNotSwallowed()
        {
            // See GoogleSafeApiServiceTests.EvaluateUrlAsync_Timeout_ThrowsAndIsNotSwallowed
            // for why this simulates the timeout directly rather than racing a real
            // HttpClient.Timeout against an artificial delay.
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Get, $"{BaseUrl}/urls/*")
                    .Respond(() => throw new TaskCanceledException("Simulated request timeout"));

            var sut = CreateSut(mockHttp);

            // Same as GoogleSafeApiService: a client-side timeout is a TaskCanceledException,
            // which the outer catch(HttpRequestException) here does not handle either.
            await Assert.ThrowsAsync<TaskCanceledException>(
                () => sut.EvaluateUrlAsync("https://example.com", CancellationToken.None));
        }
    }
}
