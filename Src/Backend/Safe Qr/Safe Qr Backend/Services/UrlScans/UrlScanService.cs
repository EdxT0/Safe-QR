using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Timeout;
using Safe_Qr_Backend.Entities;
using Safe_Qr_Backend.Repository.UrlReports;
using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Services.Google_Safe_Browsing;
using Safe_Qr_Backend.Services.UrlThreatEngine;
using Safe_Qr_Backend.Services.VirusTotal;

namespace Safe_Qr_Backend.Services.UrlScans
{
    public class UrlScanService : IUrlScanService
    {
        private readonly Phishing_Url_ONNX _phishingUrlONNXService;
        private readonly IUrlThreatEngineService _urlThreatEngineService;
        private readonly IGoogleSafeApiService _googleSafeApiService;
        private readonly IVirusTotalApiService _virusTotalApiService;
        private readonly IUrlReportRepository _urlReportRepository;
        private readonly ResiliencePipeline<AggregatedFinalResult> _pipeline;

        private const int HighRiskConsensusCount = 2;

        public UrlScanService(Phishing_Url_ONNX phishingUrlONNXService, IGoogleSafeApiService googleSafeApiService, IVirusTotalApiService virusTotalApiService, IUrlReportRepository urlReportRepository, IUrlThreatEngineService urlThreatEngineService)
        {
            _phishingUrlONNXService = phishingUrlONNXService;
            _googleSafeApiService = googleSafeApiService;
            _virusTotalApiService = virusTotalApiService;
            _urlReportRepository = urlReportRepository;
            _urlThreatEngineService = urlThreatEngineService;
            _pipeline = new ResiliencePipelineBuilder<AggregatedFinalResult>()
                        .AddTimeout(TimeSpan.FromSeconds(5))
                        .AddRetry(new Polly.Retry.RetryStrategyOptions<AggregatedFinalResult>
                        {
                            MaxRetryAttempts = 2,
                            BackoffType = DelayBackoffType.Exponential,
                            UseJitter = true,
                            ShouldHandle = new PredicateBuilder<AggregatedFinalResult>()
                                        .Handle<HttpRequestException>()
                                        .Handle<TimeoutRejectedException>()
                        })
                        .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions<AggregatedFinalResult>
                        {
                            FailureRatio = 0.5,
                            MinimumThroughput = 5,
                            SamplingDuration = TimeSpan.FromSeconds(30),
                            BreakDuration = TimeSpan.FromSeconds(30),
                            ShouldHandle = new PredicateBuilder<AggregatedFinalResult>()
                                            .Handle<HttpRequestException>()
                                            .Handle<TimeoutRejectedException>()
                                            }).AddFallback(new FallbackStrategyOptions<AggregatedFinalResult>
                        {
                            ShouldHandle = new PredicateBuilder<AggregatedFinalResult>()
                                        .Handle<HttpRequestException>()
                                        .Handle<TimeoutRejectedException>()
                                        .Handle<BrokenCircuitException>(),

                            FallbackAction = async args =>
                            {
                                CancellationToken ct = args.Context.CancellationToken;
                                var url = args.Context.Properties.GetValue(ResilienceKeys.Url, string.Empty);
                                var result = await RunLocalFallbackAsync(url, ct);

                                return Outcome.FromResult(result);
                            }
                        })
                        .Build();
        }

        private static class ResilienceKeys
        {
            public static readonly ResiliencePropertyKey<string> Url = new("Url");
        }
        public async Task<AggregatedFinalResult> PipelineEvaluate(string url, CancellationToken ct = default)
        {
            var context = ResilienceContextPool.Shared.Get(ct);
            context.Properties.Set(ResilienceKeys.Url, url);
            AggregatedFinalResult result;
            try
            {
                result=  await _pipeline.ExecuteAsync(
                    async ctx =>
                    {
                        var u = ctx.Properties.GetValue(ResilienceKeys.Url, string.Empty);
                        return await EvaluateUrlAsync(u, ctx.CancellationToken);
                    },
                    context);
            }
            finally
            {
                ResilienceContextPool.Shared.Return(context);
            }
            var addToRepoResult = await _urlReportRepository.AddUrlAsync(url, result, ct);
            if (addToRepoResult.IsSucceeded == false && addToRepoResult.Reasons == ResultEnum.Duplicate)
            {

                var updateRepoResult = await _urlReportRepository.UpdateUrlAsync(url, result, ct);
            }
            return result;
        }

        public async Task<AggregatedFinalResult> RunLocalFallbackAsync(string url, CancellationToken ct = default)
        {

            var onnxResult = await _phishingUrlONNXService.Predict(url);
            var localEngineResult = _urlThreatEngineService.EvaluateUrl(url);
            var aggregatedFallbackResult =  GetFallbackResult(onnxResult, localEngineResult);

            return aggregatedFallbackResult;
        }

        public async Task<AggregatedFinalResult> EvaluateUrlAsync(string url, CancellationToken ct)
        {

            var googleTask = _googleSafeApiService.EvaluateUrlAsync(url, ct);
            var virusTotalTask = _virusTotalApiService.EvaluateUrlAsync(url, ct);
            var onnxTask = _phishingUrlONNXService.Predict(url);

            await Task.WhenAll(googleTask, virusTotalTask, onnxTask);

            var googleResult = await googleTask;      
            var virusTotalResult = await virusTotalTask;
            var onnxResult = await onnxTask;
            var inHouseResult = _urlThreatEngineService.EvaluateUrl(url);


            var aggregatedResult = GetAggregatedResult(googleResult, virusTotalResult, onnxResult, inHouseResult);

            return aggregatedResult;
        }



        private AggregatedFinalResult GetFallbackResult(ServiceScanResult onnxResult, ServiceScanResult inHouseResult)
        {
            var result = new List<ServiceScanResult>() { onnxResult, inHouseResult };

            if (onnxResult.ServiceResult == ServiceResultEnum.malicious || inHouseResult.ServiceResult == ServiceResultEnum.malicious)
            {
                return new AggregatedFinalResult(ServiceResultEnum.malicious, result);
            }

            if (onnxResult.ServiceResult == ServiceResultEnum.highRisk || inHouseResult.ServiceResult == ServiceResultEnum.highRisk)
            {
                return new AggregatedFinalResult(ServiceResultEnum.highRisk, result);
            }
            if (onnxResult.ServiceResult == ServiceResultEnum.suspicious || inHouseResult.ServiceResult == ServiceResultEnum.suspicious)
            {
                return new AggregatedFinalResult(ServiceResultEnum.suspicious, result);
            }
            else
            {
                return new AggregatedFinalResult(ServiceResultEnum.safe, result);
            }

        }

        //doesnt include inhouse engine
        private AggregatedFinalResult GetAggregatedResult(
                    ServiceScanResult googleResult,
                    ServiceScanResult virusTotalResult,
                    ServiceScanResult ONNXResult,
                    ServiceScanResult inHouseResult
                    )
        {


            var results = new List<ServiceScanResult> { googleResult, virusTotalResult, ONNXResult, inHouseResult };

            if (googleResult.ServiceResult == ServiceResultEnum.malicious)
                return new AggregatedFinalResult(ServiceResultEnum.malicious, results);

            if (virusTotalResult.ServiceResult == ServiceResultEnum.malicious)
                return new AggregatedFinalResult(ServiceResultEnum.malicious, results);

            if (ONNXResult.ServiceResult >= ServiceResultEnum.malicious)
                return new AggregatedFinalResult(ServiceResultEnum.malicious, results);

            int highRiskCount = results.Count(r =>
                r.ServiceResult == ServiceResultEnum.highRisk ||
                r.ServiceResult == ServiceResultEnum.malicious);

            if (highRiskCount >= HighRiskConsensusCount)
                return new AggregatedFinalResult(ServiceResultEnum.malicious, results);

            if (results.Any(r => r.ServiceResult == ServiceResultEnum.highRisk))
                return new AggregatedFinalResult(ServiceResultEnum.highRisk, results);


            if (results.Any(r => r.ServiceResult == ServiceResultEnum.suspicious))
                return new AggregatedFinalResult(ServiceResultEnum.suspicious, results);

            return new AggregatedFinalResult(ServiceResultEnum.safe, results);
        }
    }
}
