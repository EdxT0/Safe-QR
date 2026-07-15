using Safe_Qr_Backend.Repository.UrlReports;
using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Services.Google_Safe_Browsing;
using Safe_Qr_Backend.Services.VirusTotal;
using Safe_Qr_Backend.Entities;

namespace Safe_Qr_Backend.Services.Url
{
    public class UrlService : IUrlService
    {
        private readonly Phishing_Url_ONNX _phishingUrlONNXService;
        private readonly IGoogleSafeApiService _googleSafeApiService;
        private readonly IVirusTotalApiService _virusTotalApiService;
        private readonly IUrlReportRepository _urlReportRepository;

        private const int HighRiskConsensusCount = 2;

        public UrlService(Phishing_Url_ONNX phishingUrlONNXService, IGoogleSafeApiService googleSafeApiService, IVirusTotalApiService virusTotalApiService, IUrlReportRepository urlReportRepository)
        {
            _phishingUrlONNXService = phishingUrlONNXService;
            _googleSafeApiService = googleSafeApiService;
            _virusTotalApiService = virusTotalApiService;
            _urlReportRepository = urlReportRepository;
        }

        public async Task<AggregatedFinalResult> EvaluateUrlAsync(String url, CancellationToken ct)
        {

            var googleResult = await _googleSafeApiService.EvaluateUrlAsync(url, ct);
            var virusTotalResult = await _virusTotalApiService.EvaluateUrlAsync(url, ct);
            var ONNXResult = await _phishingUrlONNXService.Predict(url);

            
            var aggregatedResult = GetAggregatedResult(googleResult, virusTotalResult, ONNXResult);
            var addToRepoResult = await _urlReportRepository.AddUrlAsync(url, aggregatedResult, ct);
            if (addToRepoResult.IsSucceeded == false && addToRepoResult.Reasons == ResultEnum.Duplicate) { 
            
                var updateRepoResult = await _urlReportRepository.UpdateUrlAsync(url, aggregatedResult, ct);
            }
            return aggregatedResult;
        }


        public async Task<Result<UrlReport>> FlagUrlAsync(int id, CancellationToken ct)
        {
            var result = await _urlReportRepository.FlagOrUnflagUrlAsync(id, true, ct);
            return result;
        }
        public async Task<Result<UrlReport>> GetUrlReportByIdAsync(int id, CancellationToken ct)
        {
            return await _urlReportRepository.GetUrlReportByIdAsync(id, ct);
        }
        public async Task<Result<List<UrlReport>>> GetAllUrlReportAsync(CancellationToken ct)
        {
            var result = await _urlReportRepository.GetAllUrlReportAsync(ct);
            return result;
        }
        private AggregatedFinalResult GetAggregatedResult(
                    ServiceScanResult googleResult,
                    ServiceScanResult virusTotalResult,
                    ServiceScanResult ONNXResult
                    )
        {


            var results = new List<ServiceScanResult> { googleResult, virusTotalResult, ONNXResult };

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
