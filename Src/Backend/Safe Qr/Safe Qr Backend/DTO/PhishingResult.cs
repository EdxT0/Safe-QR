namespace Safe_Qr_Backend.DTO
{
    public record PhishingResult(string Url, float PhishingProbability, float LegitProbability, bool IsSuspicious );
    
}
