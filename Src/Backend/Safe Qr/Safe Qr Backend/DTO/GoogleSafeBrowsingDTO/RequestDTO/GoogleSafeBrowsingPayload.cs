namespace Safe_Qr_Backend.DTO.GoogleSafeBrowsingDTO.RequestDTO
{
    public record GoogleSafeBrowsingPayload(ClientInfoDTO clientInfo, ThreatInfoDTO threatInfo);

}
