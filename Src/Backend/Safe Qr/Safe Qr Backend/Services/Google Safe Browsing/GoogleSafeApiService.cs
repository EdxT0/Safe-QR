using Safe_Qr_Backend.DTO;
using Safe_Qr_Backend.DTO.GoogleSafeBrowsingDTO;
using Safe_Qr_Backend.DTO.GoogleSafeBrowsingDTO.RequestDTO;
using Safe_Qr_Backend.DTO.GoogleSafeBrowsingDTO.Response;
using System.Text.Json;

namespace Safe_Qr_Backend.Services.Google_Safe_Browsing
{
    public class GoogleSafeApiService: IGoogleSafeApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GoogleSafeApiService> _logger;
        private readonly string clientInfo = "safe-qr-fyp";
        private readonly string clientVersion = "1.0.0";


        public GoogleSafeApiService(HttpClient httpClient, IConfiguration config, Logger<GoogleSafeApiService> logger)
        {
            _httpClient = httpClient;
            _apiKey = config["ApiKeys:SafeBrowsing"] ?? throw new InvalidOperationException("Safe Browsing API key is not configured.");
            _logger = logger;
        }

        private static readonly string[] TargetThreats =
        [
                "MALWARE",
                "SOCIAL_ENGINEERING",
                "UNWANTED_SOFTWARE"
        ];

        public async Task<AllServiceResult> EvaluateUrl(string url)
        {
            var endpoint = $"https://safebrowsing.googleapis.com/v4/threatMatches:find?key={_apiKey}";

            var payload = new GoogleSafeBrowsingPayload(
                new ClientInfoDTO(clientInfo, clientVersion),
                new ThreatInfoDTO(
                    threatTypes: TargetThreats,
                    platformTypes: ["ANY_PLATFORM"],
                    threatEntryTypes: ["URL"],
                    threatEntries: [new ThreatEntryDTO(url)]
                    )
                );

            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, payload);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<GoogleSafeApiResponse>();

                if (result?.matches == null || result.matches.Length == 0)
                {
                    return new AllServiceResult(false, ["no threat detected"]);
                }

                var threats = result.matches
                              .Select(m => m.threatType)
                              .Distinct().ToArray();

                return new AllServiceResult(true, threats);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Safe Browsing API request failed for URL: {Url}", url);
                return new AllServiceResult(IsThreat: false, reasons: ["UNKNOWN"]);

            }catch( JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Safe Browsing API response for URL: {Url}", url);
                return new AllServiceResult(IsThreat: false, reasons: ["UNKNOWN"]);
            }
        }
    }
}
