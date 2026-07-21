using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.Services.UrlThreatEngine
{
    public class UrlThreatEngineService : IUrlThreatEngineService
    {

        private const VendorEnum vendor = VendorEnum.InHouse;

        public ServiceScanResult EvaluateUrl(string url)
        {


            // parse as uri check
            if(!Uri.TryCreate(url, UriKind.Absolute ,out var uri)){
                return new ServiceScanResult(vendor, ServiceResultEnum.malicious, ["malformed Url"]);
            }
            // url scheme check
            if(uri.Scheme is not ("http" or "https"))
            {
                return new ServiceScanResult(vendor, ServiceResultEnum.highRisk, ["not http or https"]);
            }
            //check userinfo obsfucation
            if (!String.IsNullOrEmpty(uri.UserInfo))
            {
                return new ServiceScanResult(vendor, ServiceResultEnum.malicious, ["user info redirect"]);
            }

            //host type classification
            var hostType = Uri.CheckHostName(uri.Host);
            // Returns: Dns, IPv4, IPv6, Basic, or Unknown

            if (hostType is UriHostNameType.IPv4 or UriHostNameType.IPv6)
            {
                return new ServiceScanResult(vendor, ServiceResultEnum.malicious, ["Host type is raw ip address"]);
            }

            //query path inspection
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            string[] redirectKeys = { "redirect", "url", "next", "continue", "returnUrl", "dest" };

            foreach (var key in redirectKeys)
            {
                var value = query[key];
                if (value != null && (value.StartsWith("http") || value.Contains('.')))
                {
                    return new ServiceScanResult(vendor, ServiceResultEnum.highRisk, [$"Query param '{key}' contains a URL-like value: {value}"]);
                }
            }


            return new ServiceScanResult(vendor, ServiceResultEnum.safe, [$"none of the engine got flagged"]);

        }
    }
}
