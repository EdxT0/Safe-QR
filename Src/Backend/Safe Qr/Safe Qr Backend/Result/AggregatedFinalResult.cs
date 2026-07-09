namespace Safe_Qr_Backend.Result
{
    public record AggregatedFinalResult( ServiceResultEnum serviceResultEnum, List<ServiceScanResult> serviceScanResult);
   
}
