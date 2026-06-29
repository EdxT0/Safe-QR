using System.ComponentModel.DataAnnotations.Schema;

namespace Safe_Qr_Backend.Entities
{
    public record ServiceResult(ServiceResultVerdict serviceResultVerdict, string[] reasons);
   
    public enum ServiceResultVerdict
    {
        safe,
        suspicious,
        highRisk,
        malicious
    }
}
