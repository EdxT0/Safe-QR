using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.DTO.ScanHistoryDTO
{
    public record SaveScanHistoryDTO(string Payload, string PayloadType, AggregatedFinalResult Result);
}
