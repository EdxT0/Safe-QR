using System.ComponentModel.DataAnnotations.Schema;

namespace Safe_Qr_Backend.Result
{
    public record ServiceResult(string vendor, ServiceResultEnum serviceResultVerdict, string[] reasons);
   

}
