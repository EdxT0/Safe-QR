using Safe_Qr_Backend.DTO.GoogleSafeBrowsingDTO;
using Safe_Qr_Backend.DTO.GoogleSafeBrowsingDTO.RequestDTO;
using Safe_Qr_Backend.DTO.GoogleSafeBrowsingDTO.Response;
using Safe_Qr_Backend.Result;
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
        private readonly VendorEnum vendor = VendorEnum.Google;


        public GoogleSafeApiService(HttpClient httpClient, IConfiguration config, ILogger<GoogleSafeApiService> logger)
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

        public async Task<ServiceScanResult> EvaluateUrl(string url, CancellationToken ct = default)
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
                var response = await _httpClient.PostAsJsonAsync(endpoint, payload, ct);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<GoogleSafeApiResponse>(ct);

                if (result?.matches == null || result.matches.Length == 0)
                {
                    return new ServiceScanResult(vendor, ServiceResultEnum.safe, ["no threat detected"]);
                }

                var threats = result.matches
                              .Select(m => m.threatType)
                              .Distinct().ToArray();

                return new ServiceScanResult(vendor, ServiceResultEnum.malicious, threats);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Safe Browsing API request failed for URL: {Url}", url);
                return new ServiceScanResult(vendor, ServiceResultEnum.highRisk, reasons: ["UNKNOWN"]);

            }catch( JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Safe Browsing API response for URL: {Url}", url);
                return new ServiceScanResult(vendor, ServiceResultEnum.highRisk, reasons: ["UNKNOWN"]);
            }
        }
    }
}
