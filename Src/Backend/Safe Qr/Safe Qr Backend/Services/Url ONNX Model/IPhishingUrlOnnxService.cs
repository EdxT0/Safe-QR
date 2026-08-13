using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Services
{
    public interface IPhishingUrlOnnxService
    {
        Task<ServiceScanResult> Predict(string url);
    }
}
