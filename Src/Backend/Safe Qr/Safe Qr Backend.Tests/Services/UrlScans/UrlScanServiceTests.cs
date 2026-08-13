using System.Diagnostics;
using Moq;
using Polly.Timeout;
using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Repository.UrlReports;
using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Services;
using Safe_Qr_Backend.Services.Google_Safe_Browsing;
using Safe_Qr_Backend.Services.UrlScans;
using Safe_Qr_Backend.Services.UrlThreatEngine;
using Safe_Qr_Backend.Services.VirusTotal;

namespace Safe_Qr_Backend.Tests.Services.UrlScans
{
    public class UrlScanServiceTests
    {
        private const string Url = "https://example.com";

        private static ServiceScanResult MakeResult(VendorEnum vendor, ServiceResultEnum level) =>
            new(vendor, level, new[] { $"{vendor} says {level}" });

        private static (
            Mock<IPhishingUrlOnnxService> Onnx,
            Mock<IGoogleSafeApiService> Google,
            Mock<IVirusTotalApiService> VirusTotal,
            Mock<IUrlThreatEngineService> InHouse,
            Mock<IUrlReportRepository> Repository) CreateMocks()
        {
            var onnx = new Mock<IPhishingUrlOnnxService>();
            var google = new Mock<IGoogleSafeApiService>();
            var virusTotal = new Mock<IVirusTotalApiService>();
            var inHouse = new Mock<IUrlThreatEngineService>();
            var repository = new Mock<IUrlReportRepository>();

            repository
                .Setup(r => r.AddUrlAsync(It.IsAny<string>(), It.IsAny<AggregatedFinalResult>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string url, AggregatedFinalResult result, CancellationToken _) =>
                    Result<UrlReport>.Succeeded(new UrlReport { Url = url, Results = result }, ResultEnum.Successful));

            return (onnx, google, virusTotal, inHouse, repository);
        }

        private static void SetupAllSafe(
            Mock<IPhishingUrlOnnxService> onnx,
            Mock<IGoogleSafeApiService> google,
            Mock<IVirusTotalApiService> virusTotal,
            Mock<IUrlThreatEngineService> inHouse)
        {
            google.Setup(x => x.EvaluateUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(MakeResult(VendorEnum.Google, ServiceResultEnum.safe));
            virusTotal.Setup(x => x.EvaluateUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(MakeResult(VendorEnum.VirusTotal, ServiceResultEnum.safe));
            onnx.Setup(x => x.Predict(It.IsAny<string>()))
                .ReturnsAsync(MakeResult(VendorEnum.ONNX, ServiceResultEnum.safe));
            inHouse.Setup(x => x.EvaluateUrl(It.IsAny<string>()))
                   .Returns(MakeResult(VendorEnum.InHouse, ServiceResultEnum.safe));
        }

        private static UrlScanService CreateSut(
            Mock<IPhishingUrlOnnxService> onnx,
            Mock<IGoogleSafeApiService> google,
            Mock<IVirusTotalApiService> virusTotal,
            Mock<IUrlThreatEngineService> inHouse,
            Mock<IUrlReportRepository> repository) =>
            new(onnx.Object, google.Object, virusTotal.Object, repository.Object, inHouse.Object);

        // ---- All checks agree ----

        [Fact]
        public async Task EvaluateUrlAsync_AllChecksAgree_UnanimousSafe_ReturnsSafe()
        {
            var (onnx, google, virusTotal, inHouse, repository) = CreateMocks();
            SetupAllSafe(onnx, google, virusTotal, inHouse);

            var sut = CreateSut(onnx, google, virusTotal, inHouse, repository);

            var result = await sut.EvaluateUrlAsync(Url, CancellationToken.None);

            Assert.Equal(ServiceResultEnum.safe, result.ServiceResultEnum);
            Assert.Equal(
                new[] { VendorEnum.Google, VendorEnum.VirusTotal, VendorEnum.ONNX, VendorEnum.InHouse },
                result.ServiceScanResult.Select(r => r.Vendor));
        }

        [Fact]
        public async Task EvaluateUrlAsync_AllChecksAgree_UnanimousMalicious_ReturnsMalicious()
        {
            var (onnx, google, virusTotal, inHouse, repository) = CreateMocks();
            google.Setup(x => x.EvaluateUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(MakeResult(VendorEnum.Google, ServiceResultEnum.malicious));
            virusTotal.Setup(x => x.EvaluateUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(MakeResult(VendorEnum.VirusTotal, ServiceResultEnum.malicious));
            onnx.Setup(x => x.Predict(It.IsAny<string>()))
                .ReturnsAsync(MakeResult(VendorEnum.ONNX, ServiceResultEnum.malicious));
            inHouse.Setup(x => x.EvaluateUrl(It.IsAny<string>()))
                   .Returns(MakeResult(VendorEnum.InHouse, ServiceResultEnum.malicious));

            var sut = CreateSut(onnx, google, virusTotal, inHouse, repository);

            var result = await sut.EvaluateUrlAsync(Url, CancellationToken.None);

            Assert.Equal(ServiceResultEnum.malicious, result.ServiceResultEnum);
        }

        // ---- Checks disagree with each other ----

        [Theory]
        // A single malicious verdict from Google, VirusTotal, or ONNX individually overrides everything else.
        [InlineData(ServiceResultEnum.malicious, ServiceResultEnum.safe, ServiceResultEnum.safe, ServiceResultEnum.safe, ServiceResultEnum.malicious)]
        [InlineData(ServiceResultEnum.safe, ServiceResultEnum.malicious, ServiceResultEnum.safe, ServiceResultEnum.safe, ServiceResultEnum.malicious)]
        [InlineData(ServiceResultEnum.safe, ServiceResultEnum.safe, ServiceResultEnum.malicious, ServiceResultEnum.safe, ServiceResultEnum.malicious)]
        // Two independent highRisk verdicts reach consensus (HighRiskConsensusCount = 2) and escalate to malicious.
        [InlineData(ServiceResultEnum.highRisk, ServiceResultEnum.highRisk, ServiceResultEnum.safe, ServiceResultEnum.safe, ServiceResultEnum.malicious)]
        // A single highRisk verdict alone stays at highRisk (no consensus reached).
        [InlineData(ServiceResultEnum.highRisk, ServiceResultEnum.safe, ServiceResultEnum.safe, ServiceResultEnum.safe, ServiceResultEnum.highRisk)]
        // A single suspicious verdict alone escalates the overall result to suspicious.
        [InlineData(ServiceResultEnum.suspicious, ServiceResultEnum.safe, ServiceResultEnum.safe, ServiceResultEnum.safe, ServiceResultEnum.suspicious)]
        public async Task EvaluateUrlAsync_MixedVerdicts_AppliesConsensusRules(
            ServiceResultEnum googleLevel,
            ServiceResultEnum virusTotalLevel,
            ServiceResultEnum onnxLevel,
            ServiceResultEnum inHouseLevel,
            ServiceResultEnum expected)
        {
            var (onnx, google, virusTotal, inHouse, repository) = CreateMocks();
            google.Setup(x => x.EvaluateUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(MakeResult(VendorEnum.Google, googleLevel));
            virusTotal.Setup(x => x.EvaluateUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(MakeResult(VendorEnum.VirusTotal, virusTotalLevel));
            onnx.Setup(x => x.Predict(It.IsAny<string>()))
                .ReturnsAsync(MakeResult(VendorEnum.ONNX, onnxLevel));
            inHouse.Setup(x => x.EvaluateUrl(It.IsAny<string>()))
                   .Returns(MakeResult(VendorEnum.InHouse, inHouseLevel));

            var sut = CreateSut(onnx, google, virusTotal, inHouse, repository);

            var result = await sut.EvaluateUrlAsync(Url, CancellationToken.None);

            Assert.Equal(expected, result.ServiceResultEnum);
        }

        // ---- Repository persistence (PipelineEvaluate's happy path) ----

        [Fact]
        public async Task PipelineEvaluate_HappyPath_PersistsViaAddUrlAsync()
        {
            var (onnx, google, virusTotal, inHouse, repository) = CreateMocks();
            SetupAllSafe(onnx, google, virusTotal, inHouse);

            var sut = CreateSut(onnx, google, virusTotal, inHouse, repository);

            var result = await sut.PipelineEvaluate(Url, CancellationToken.None);

            Assert.Equal(ServiceResultEnum.safe, result.ServiceResultEnum);
            repository.Verify(r => r.AddUrlAsync(Url, It.IsAny<AggregatedFinalResult>(), It.IsAny<CancellationToken>()), Times.Once);
            repository.Verify(r => r.UpdateUrlAsync(It.IsAny<string>(), It.IsAny<AggregatedFinalResult>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task PipelineEvaluate_WhenAddIsDuplicate_FallsBackToUpdateUrlAsync()
        {
            var (onnx, google, virusTotal, inHouse, repository) = CreateMocks();
            SetupAllSafe(onnx, google, virusTotal, inHouse);
            repository
                .Setup(r => r.AddUrlAsync(It.IsAny<string>(), It.IsAny<AggregatedFinalResult>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<UrlReport>.Failure(ResultEnum.Duplicate));
            repository
                .Setup(r => r.UpdateUrlAsync(It.IsAny<string>(), It.IsAny<AggregatedFinalResult>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string url, AggregatedFinalResult result, CancellationToken _) =>
                    Result<UrlReport>.Succeeded(new UrlReport { Url = url, Results = result }, ResultEnum.Successful));

            var sut = CreateSut(onnx, google, virusTotal, inHouse, repository);

            await sut.PipelineEvaluate(Url, CancellationToken.None);

            repository.Verify(r => r.UpdateUrlAsync(Url, It.IsAny<AggregatedFinalResult>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---- Downstream failure -> Polly retries, then Fallback kicks in ----

        public static IEnumerable<object[]> DownstreamFailureExceptions()
        {
            yield return new object[] { new HttpRequestException("simulated network failure") };
            yield return new object[] { new TimeoutRejectedException("simulated per-attempt timeout") };
        }

        // Real retry delays (exponential backoff, MaxRetryAttempts = 2) genuinely elapse here
        // (~2-4s) since they aren't injectable/mockable, so this is slower than the others.
        [Theory]
        [MemberData(nameof(DownstreamFailureExceptions))]
        [Trait("Category", "Slow")]
        public async Task PipelineEvaluate_DownstreamCheckFails_RetriesThenFallsBackToLocalEngines(Exception thrownByGoogle)
        {
            var (onnx, google, virusTotal, inHouse, repository) = CreateMocks();
            SetupAllSafe(onnx, google, virusTotal, inHouse);
            google.Setup(x => x.EvaluateUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(thrownByGoogle);

            var sut = CreateSut(onnx, google, virusTotal, inHouse, repository);

            var result = await sut.PipelineEvaluate(Url, CancellationToken.None);

            // 1 initial attempt + 2 retries (MaxRetryAttempts = 2) before Fallback takes over.
            google.Verify(x => x.EvaluateUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
            // RunLocalFallbackAsync only aggregates ONNX + in-house — this proves the fallback
            // path produced the result, not the normal 4-way EvaluateUrlAsync aggregation.
            Assert.Equal(new[] { VendorEnum.ONNX, VendorEnum.InHouse }, result.ServiceScanResult.Select(r => r.Vendor));
        }

        // ---- Repeated failures trip the circuit breaker ----

        // MinimumThroughput = 5 with FailureRatio = 0.5: two failing top-level calls (3 attempts +
        // 2 attempts = 5 failed attempts recorded by the circuit breaker) are enough to trip it
        // open. Genuinely waits out the real retry delays from the first two calls (~7-9s).
        [Fact]
        [Trait("Category", "Slow")]
        public async Task PipelineEvaluate_RepeatedFailures_TripsCircuitBreaker_ThenBypassesDownstreamCalls()
        {
            var (onnx, google, virusTotal, inHouse, repository) = CreateMocks();
            SetupAllSafe(onnx, google, virusTotal, inHouse);
            google.Setup(x => x.EvaluateUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ThrowsAsync(new HttpRequestException("simulated network failure"));

            var sut = CreateSut(onnx, google, virusTotal, inHouse, repository);

            await sut.PipelineEvaluate(Url, CancellationToken.None);
            await sut.PipelineEvaluate(Url, CancellationToken.None);

            // 3 attempts from the first call + 2 attempts from the second (the second call's
            // final retry is short-circuited once the breaker opens mid-way through it).
            google.Verify(x => x.EvaluateUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(5));

            // Circuit is now open (BreakDuration = 30s) — this call should short-circuit to
            // Fallback immediately, without invoking Google again at all.
            var sw = Stopwatch.StartNew();
            var result = await sut.PipelineEvaluate(Url, CancellationToken.None);
            sw.Stop();

            google.Verify(x => x.EvaluateUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
            Assert.Equal(new[] { VendorEnum.ONNX, VendorEnum.InHouse }, result.ServiceScanResult.Select(r => r.Vendor));
            Assert.True(sw.ElapsedMilliseconds < 1000, $"Expected the open circuit to short-circuit quickly, took {sw.ElapsedMilliseconds}ms");
        }
    }
}
