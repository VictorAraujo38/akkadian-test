using MedicalScheduling.API.DTOs;
using System.Text.Json;

namespace MedicalScheduling.API.Services
{
    public class OpenAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public OpenAIService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"] ?? 
                     Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        }

        public async Task<TriageResponseDto> GetSpecialtyRecommendationAsync(string symptoms)
        {
            // If no API key, fallback to mock
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new MockAIService().GetSpecialtyRecommendationAsync(symptoms).Result;
            }

            // Implement OpenAI API call here
            // This is a simplified example
            var prompt = $"Based on these symptoms: {symptoms}, recommend a medical specialty. Return only the specialty name.";
            
            // Add your OpenAI implementation here
            
            return new TriageResponseDto
            {
                RecommendedSpecialty = "Clínica Geral",
                Confidence = "Alta"
            };
        }
    }
}