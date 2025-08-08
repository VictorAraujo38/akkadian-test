using MedicalScheduling.API.DTOs;

namespace MedicalScheduling.API.Services
{
    public interface ITriageService
    {
        Task<TriageResponseDto> ProcessTriageAsync(string symptoms);
    }

    public interface IAIService
    {
        Task<TriageResponseDto> GetSpecialtyRecommendationAsync(string symptoms);
    }
}