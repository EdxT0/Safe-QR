using Safe_Qr_Backend.Result;
using Safe_Qr_Backend.Services.Google_Safe_Browsing;
using Safe_Qr_Backend.Services.VirusTotal;

namespace Safe_Qr_Backend.Services.UrlService
{
    public class EvaluateUrlService : IEvaluateUrlService
    {
        private readonly Phishing_Url_ONNX _phishingUrlONNXService;
        private readonly IGoogleSafeApiService _googleSafeApiService;
        private readonly IVirusTotalApiService _virusTotalApiService;

        public EvaluateUrlService(Phishing_Url_ONNX phishingUrlONNXService, IGoogleSafeApiService googleSafeApiService, IVirusTotalApiService virusTotalApiService)
        {
            _phishingUrlONNXService = phishingUrlONNXService;
            _googleSafeApiService = googleSafeApiService;
            _virusTotalApiService = virusTotalApiService;
        }

        //public async Task<AggregatedFinalResult> EvaluateUrl( String url)
        //{

        //    var googleResult = await _googleSafeApiService.EvaluateUrl(url);
        //    var virusTotalResult = await _virusTotalApiService.EvaluateUrl(url);


        //    var ONNXResult = await _phishingUrlONNXService.Predict(url);

        //    return ONNXResult;
        //}
    }
}
