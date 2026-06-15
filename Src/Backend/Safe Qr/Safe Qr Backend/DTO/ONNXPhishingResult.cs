namespace Safe_Qr_Backend.DTO
{
    public record ONNXPhishingResult(
        string Url, 
        float PhishingProbability, 
        float LegitProbability, 
        bool IsSuspicious );
    
}
