using Safe_Qr_Backend.DTO.VirusTotalDTO.analysisDTO;
using Safe_Qr_Backend.DTO.VirusTotalDTO.CacheResponse;
using Safe_Qr_Backend.DTO.VirusTotalDTO.New_url_response;
using Safe_Qr_Backend.Result;
using System.Text;
using System.Text.Json;

namespace Safe_Qr_Backend.Services.VirusTotal
{
    public class VirusTotalApiService : IVirusTotalApiService
    {

        private readonly HttpClient _httpClient;
        private readonly Logger<VirusTotalApiService> _logger;
        private readonly string _apiKey;
        private readonly string baseUrl = "https://www.virustotal.com/api/v3/";
        private readonly VendorEnum vendor = VendorEnum.VirusTotal;

        public VirusTotalApiService(HttpClient httpClient, IConfiguration config, Logger<VirusTotalApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = config["ApiKeys:VirusTotal"] ?? throw new InvalidOperationException("VirusTotal api key not set up");

            _httpClient.DefaultRequestHeaders.Add("x-apikey", _apiKey);
        }


        public async Task<ServiceScanResult> EvaluateUrl(string url, CancellationToken ct = default)
        {
            try
            {
                // check cache
                var cachedResult = await CheckUrlCached(url, ct);
                if (cachedResult != null) {
                    var stats = cachedResult.data.attributes.last_analysis_stats;
                    int malicious = stats.malicious;
                    int suspicious = stats.suspicious;
                    int harmless = stats.harmless;
                    int undetected = stats.undetected;

                    return BuildResult(malicious, suspicious, harmless, undetected, vendor);
                }
                //analyse url
                string analyseId = await AnalyseUrl(url, ct);
                //fetch analysed result
                var analysedResult = await GetAnalysisResult(analyseId, ct);

                if (analysedResult != null)
                {
                    var stats = analysedResult.data.attributes.stats;
                    int malicious = stats.malicious;
                    int suspicious = stats.suspicious;
                    int harmless = stats.harmless;
                    int undetected = stats.undetected;

                    return BuildResult(malicious,suspicious,harmless, undetected, vendor);
                }
                else
                {
                    return new ServiceScanResult(vendor,ServiceResultEnum.suspicious, [$"Virus Total API didn't manage to get result"]);
                }

            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "VirusTotal request failed for URL: {Url}", url);
                return new ServiceScanResult(vendor,ServiceResultEnum.highRisk, ["UNKNOWN"]);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "VirusTotal deserialization failed for URL: {Url}", url);
                return new ServiceScanResult(vendor,ServiceResultEnum.highRisk, ["UNKNOWN"]);
            }
        }
        private ServiceScanResult BuildResult(int malicious, int suspicious, int harmless, int undetected, VendorEnum vendor)
        {
            if (malicious > 0)
                return new ServiceScanResult(vendor, ServiceResultEnum.malicious,
                    [$"VirusTotal: {malicious} engine(s) flagged as malicious"]);

            if (suspicious > 0)
                return new ServiceScanResult(vendor, ServiceResultEnum.suspicious,
                    [$"VirusTotal: {suspicious} engine(s) flagged as suspicious"]);

            if (harmless > 0)
                return new ServiceScanResult(vendor,ServiceResultEnum.safe,
                    [$"VirusTotal: {harmless} engine(s) confirmed safe"]);

            return new ServiceScanResult(vendor,ServiceResultEnum.suspicious,
                [$"VirusTotal: {undetected} engine(s) undetected — no verdict"]);
        }

        private async Task<CacheResponseDTO?> CheckUrlCached(string url, CancellationToken ct = default)
        {
            var urlId = ConvertUrlToBase64(url);
            var endpoint = $"{baseUrl}/urls/{urlId}";


            var response = await _httpClient.GetAsync(endpoint, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<CacheResponseDTO>(ct);

            return result;



        }

        private static string ConvertUrlToBase64(string url)
        {
            var bytes = Encoding.UTF8.GetBytes(url);

            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private async Task<string> AnalyseUrl(string url, CancellationToken ct = default)
        {
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("url", url)
            });
            var endpoint = $"{baseUrl}/urls";
            var response = await _httpClient.PostAsync(endpoint, formData, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AnalysisIdResponse>(ct) ?? throw new InvalidOperationException(
            "VirusTotal returned a 2xx response but the body could not be deserialized as expected.");
            string analyseId = result.data.id ?? throw new InvalidOperationException(
            "VirusTotal returned a successful response without an analysis ID — check the response shape against AnalysisIdResponse.");
            return analyseId;
        }

        private async Task<AnalysisResponse?> GetAnalysisResult(string analysisId, CancellationToken ct = default)
        {

            var endpoint = $"{baseUrl}/analyses/{analysisId}";



            int pollingAmount = 5;
            int attempt = 0;
            while(attempt < pollingAmount)
            {
                attempt++;
                await Task.Delay(3000);

                var response = await _httpClient.GetAsync(endpoint, ct);
                response.EnsureSuccessStatusCode();
                var result = await  response.Content.ReadFromJsonAsync<AnalysisResponse>();
                if(result == null)
                {
                    continue;
                }
                string status = result.data.attributes.status;
                if(status == "completed")
                {
                    return result;
                }
                _logger.LogInformation("VirusTotal analysis {Id} status: {Status}, attempt {Attempt}",
               analysisId, status, attempt);
               
            }

            _logger.LogWarning("VirusTotal analysis {Id} did not complete within polling window", analysisId);
            return null;
        }
    }
}
