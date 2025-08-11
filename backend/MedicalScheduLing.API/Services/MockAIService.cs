using MedicalScheduling.API.DTOs;

namespace MedicalScheduling.API.Services
{
    public class MockAIService : IAIService
    {

        private readonly HttpClient _httpClient;

        public MockAIService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        private readonly Dictionary<string, string> _symptomSpecialtyMap = new()
        {
            { "dor de cabeça", "Neurologia" },
            { "febre", "Clínica Geral" },
            { "tosse", "Pneumologia" },
            { "dor no peito", "Cardiologia" },
            { "ansiedade", "Psiquiatria" },
            { "dor nas costas", "Ortopedia" },
            { "alergia", "Alergologia" },
            { "dor de estômago", "Gastroenterologia" },
            { "visão embaçada", "Oftalmologia" },
            { "dor de garganta", "Otorrinolaringologia" }
        };

        public MockAIService()
        {
        }

        public Task<TriageResponseDto> GetSpecialtyRecommendationAsync(string symptoms)
        {
            var symptomsLower = symptoms.ToLower();

            foreach (var kvp in _symptomSpecialtyMap)
            {
                if (symptomsLower.Contains(kvp.Key))
                {
                    return Task.FromResult(new TriageResponseDto
                    {
                        RecommendedSpecialty = kvp.Value,
                        Confidence = "Alta"
                    });
                }
            }

            return Task.FromResult(new TriageResponseDto
            {
                RecommendedSpecialty = "Clínica Geral",
                Confidence = "Média"
            });
        }
    }
}