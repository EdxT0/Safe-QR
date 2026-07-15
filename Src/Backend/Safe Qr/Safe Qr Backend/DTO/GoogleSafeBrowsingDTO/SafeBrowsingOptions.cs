namespace Safe_Qr_Backend.DTO.GoogleSafeBrowsingDTO
{
    public sealed class SafeBrowsingOptions
    {
        public const string SectionName = "SafeBrowsing";
        public required string ApiKey { get; init; }
        public string BaseUrl { get; init; } = "https://safebrowsing.googleapis.com";
    }
}
