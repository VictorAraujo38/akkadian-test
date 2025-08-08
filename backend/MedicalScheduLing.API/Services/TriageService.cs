namespace MedicalScheduling.API.Services
{
    public class TriageService : ITriageService
    {
        private readonly IAIService _aiService;

        public TriageService(IAIService aiService)
        {
            _aiService = aiService;
        }

        public async Task<TriageResponseDto> ProcessTriageAsync(string symptoms)
        {
            return await _aiService.GetSpecialtyRecommendationAsync(symptoms);
        }
    }
}