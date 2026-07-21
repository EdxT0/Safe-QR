using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Services.UrlThreatEngine
{
    public interface IUrlThreatEngineService
    {
        ServiceScanResult EvaluateUrl(string url);
    }
}
