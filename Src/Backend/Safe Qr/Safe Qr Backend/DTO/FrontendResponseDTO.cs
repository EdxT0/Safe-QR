using Safe_Qr_Backend.Result;

namespace Safe_Qr_Backend.DTO
{
    public record FrontendResponseDTO(string url, string classification, AllServiceResult allServiceResult);
    
}
