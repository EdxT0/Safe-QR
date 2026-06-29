using Safe_Qr_Backend.DTO;
using Safe_Qr_Backend.DTO.VirusTotalDTO.analysisDTO;
using Safe_Qr_Backend.DTO.VirusTotalDTO.CacheResponse;
using Safe_Qr_Backend.DTO.VirusTotalDTO.New_url_response;
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

        public VirusTotalApiService(HttpClient httpClient, IConfiguration config, Logger<VirusTotalApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = config["ApiKeys:VirusTotal"] ?? throw new InvalidOperationException("VirusTotal api key not set up");

            _httpClient.DefaultRequestHeaders.Add("x-apikey", _apiKey);
        }


        public async Task<AllServiceResult> EvaluateUrl(string url)
        {
            try
            {
                // check cache
                var cachedResult = await CheckUrlCached(url);
                if (cachedResult != null) {
                    var stats = cachedResult.data.attributes.last_analysis_stats;
                    int malicious = stats.malicious;
                    int suspicious = stats.suspicious;
                    int harmless = stats.harmless;
                    int undetected = stats.undetected;

                    return BuildResult(malicious, suspicious, harmless, undetected);
                }
                //analyse url
                string analyseId = await AnalyseUrl(url);
                //fetch analysed result
                var analysedResult = await GetAnalysisResult(analyseId);

                if (analysedResult != null)
                {
                    var stats = analysedResult.data.attributes.stats;
                    int malicious = stats.malicious;
                    int suspicious = stats.suspicious;
                    int harmless = stats.harmless;
                    int undetected = stats.undetected;

                    return BuildResult(malicious,suspicious,harmless,undetected);
                }
                else
                {
                    return new AllServiceResult(ServiceResult.suspicious, [$"Virus Total API didn't manage to get result"]);
                }

            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "VirusTotal request failed for URL: {Url}", url);
                return new AllServiceResult(ServiceResult.highRisk, ["UNKNOWN"]);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "VirusTotal deserialization failed for URL: {Url}", url);
                return new AllServiceResult(ServiceResult.highRisk, ["UNKNOWN"]);
            }
        }
        private AllServiceResult BuildResult(int malicious, int suspicious, int harmless, int undetected)
        {
            if (malicious > 0)
                return new AllServiceResult(ServiceResult.malicious,
                    [$"VirusTotal: {malicious} engine(s) flagged as malicious"]);

            if (suspicious > 0)
                return new AllServiceResult(ServiceResult.suspicious,
                    [$"VirusTotal: {suspicious} engine(s) flagged as suspicious"]);

            if (harmless > 0)
                return new AllServiceResult(ServiceResult.safe,
                    [$"VirusTotal: {harmless} engine(s) confirmed safe"]);

            return new AllServiceResult(ServiceResult.suspicious,
                [$"VirusTotal: {undetected} engine(s) undetected — no verdict"]);
        }

        private async Task<CacheResponseDTO?> CheckUrlCached(string url)
        {
            var urlId = ConvertUrlToBase64(url);
            var endpoint = $"{baseUrl}/urls/{urlId}";


            var response = await _httpClient.GetAsync(endpoint);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<CacheResponseDTO>();

            return result;



        }

        private static string ConvertUrlToBase64(string url)
        {
            var bytes = Encoding.UTF8.GetBytes(url);

            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private async Task<string> AnalyseUrl(string url)
        {
            var formData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("url", url)
            });
            var endpoint = $"{baseUrl}/urls";
            var response = await _httpClient.PostAsync(endpoint, formData);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AnalysisIdResponse>();
            string analyseId = result.data.id;
            return analyseId;
        }

        private async Task<AnalysisResponse?> GetAnalysisResult(string analysisId)
        {

            var endpoint = $"{baseUrl}/analyses/{analysisId}";



            int pollingAmount = 5;
            int attempt = 0;
            while(attempt < pollingAmount)
            {
                attempt++;
                await Task.Delay(3000);

                var response = await _httpClient.GetAsync(endpoint);
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
