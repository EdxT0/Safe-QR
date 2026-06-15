namespace Safe_Qr_Backend.DTO.GoogleSafeBrowsingDTO.RequestDTO
{
    public record ThreatInfoDTO(string[] threatTypes,
                                string[] platformTypes,
                                string[] threatEntryTypes,
                                ThreatEntryDTO[] threatEntries);

}
