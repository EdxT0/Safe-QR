namespace Safe_Qr_Backend.Result
{
    public record ONNXPhishingResult(
        string Url, 
        float PhishingProbability, 
        float LegitProbability, 
        bool IsSuspicious );
    
}
