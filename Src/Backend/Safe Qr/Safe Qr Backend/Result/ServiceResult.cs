using System.ComponentModel.DataAnnotations.Schema;

namespace Safe_Qr_Backend.Result
{
    public record ServiceResult(VendorEnum vendor, ServiceResultEnum serviceResultVerdict, string[] reasons);
   

}
