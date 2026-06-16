namespace Safe_Qr_Backend.DTO
{
    public record AllServiceResult(ServiceResult ServiceResult, string[] reasons );

    public enum ServiceResult
    {
        safe,
        suspicious,
        highRisk,
        malicious
    }
    
}
