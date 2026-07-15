using System.Diagnostics.CodeAnalysis;

namespace Safe_Qr_Backend.Result
{
    public class AggregatedFinalResult
    {
        public required ServiceResultEnum ServiceResultEnum { get; init; }
        public required List<ServiceScanResult> ServiceScanResult { get; init; } = new List<ServiceScanResult>();
        
        public AggregatedFinalResult()
        {

        }
        [SetsRequiredMembers]
        public AggregatedFinalResult(ServiceResultEnum serviceResultEnum, List<ServiceScanResult> serviceScanResult)
        {
            ServiceResultEnum = serviceResultEnum;
            ServiceScanResult = serviceScanResult;
        }
    }
   
}
