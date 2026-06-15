namespace Safe_Qr_Backend.DTO.GoogleSafeBrowsingDTO.Response
{
    public record ThreatMatch(string threatType, string platformType, string threatEntryType, ThreatEntryDTO threat, string cacheDuration);

}
