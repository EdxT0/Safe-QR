using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.DTO.ThreatFeedbackDTO
{
    public record SubmitThreatFeedbackDTO(
        string Payload,
        string PayloadType,
        AggregatedFinalResult SystemClassification,
        ServiceResultEnum ReportedRiskLevel,
        string? Comment);
}
