using Google.Protobuf;
using Microsoft.Extensions.Options;
using Safe_Qr_Backend.DTO.GoogleSafeBrowsingDTO;
using Safe_Qr_Backend.Protos.SafeBrowsing;
using Safe_Qr_Backend.Result;



namespace Safe_Qr_Backend.Services.Google_Safe_Browsing
{
    public class GoogleSafeApiService: IGoogleSafeApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<GoogleSafeApiService> _logger;
        private readonly VendorEnum vendor = VendorEnum.Google;
        private readonly IOptions<SafeBrowsingOptions> _options;

        public GoogleSafeApiService(HttpClient httpClient, IConfiguration config, ILogger<GoogleSafeApiService> logger, IOptions<SafeBrowsingOptions> options)
        {
            _httpClient = httpClient;
            _apiKey = config["ApiKeys:SafeBrowsing"] ?? throw new InvalidOperationException("Safe Browsing API key is not configured.");
            _logger = logger;
            _options = options;
        }

        private static readonly string[] KnownThreatTypes = ["MALWARE", "SOCIAL_ENGINEERING", "UNWANTED_SOFTWARE", "POTENTIALLY_HARMFUL_APPLICATION"];


        public async Task<ServiceScanResult> EvaluateUrlAsync(string url, CancellationToken cancellationToken)
        {
            var requestUri = $"/v5/urls:search?key={_options.Value.ApiKey}&urls={Uri.EscapeDataString(url)}";

            HttpResponseMessage httpResponse;
            try
            {
                httpResponse = await _httpClient.GetAsync(requestUri, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Safe Browsing urls:search request failed.");
                return new ServiceScanResult(vendor, ServiceResultEnum.highRisk, ["Safe Browsing request failed."]);
            }

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Safe Browsing returned {StatusCode}: {Body}", httpResponse.StatusCode, errorBody);
                return new ServiceScanResult(vendor, ServiceResultEnum.highRisk, ["Safe Browsing returned an error."]);
            }

            var rawBytes = await httpResponse.Content.ReadAsByteArrayAsync(cancellationToken);

            SearchUrlsResponse response;
            try
            {
                response = SearchUrlsResponse.Parser.ParseFrom(rawBytes);
            }
            catch (InvalidProtocolBufferException ex)
            {
                _logger.LogError(ex, "Safe Browsing response failed protobuf parsing. Bytes: {ByteCount}", rawBytes.Length);
                return new ServiceScanResult(vendor, ServiceResultEnum.suspicious, ["Unexpected response from Safe Browsing."]);
            }

            var recognizedThreatTypes = response.Threats
                .SelectMany(t => t.ThreatTypes)
                .Where(t => t != ThreatType.Unspecified)
                .Select(t => t.ToString())
                .Distinct()
                .ToArray();

            return recognizedThreatTypes.Length == 0
                ? new ServiceScanResult(vendor, ServiceResultEnum.safe, ["no threats found"])
                : new ServiceScanResult(vendor, ServiceResultEnum.malicious, recognizedThreatTypes);
        }
    }
}
