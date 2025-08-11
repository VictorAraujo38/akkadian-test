using MedicalScheduling.API.DTOs;
using System.Text.Json;
using System.Text;

namespace MedicalScheduling.API.Services
{
    public class OpenAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<OpenAIService> _logger;

        public OpenAIService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAIService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["OpenAI:ApiKey"] ??
                     Environment.GetEnvironmentVariable("OPENAI_API_KEY");

            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            }
        }

        public async Task<TriageResponseDto> GetSpecialtyRecommendationAsync(string symptoms)
        {
            // Se não há API key, usa fallback mock
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("OpenAI API Key não configurada, usando serviço mock");
                return await new MockAIService().GetSpecialtyRecommendationAsync(symptoms);
            }

            try
            {
                var prompt = CreateTriagePrompt(symptoms);
                var response = await CallOpenAIAPI(prompt);

                return ParseOpenAIResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao chamar OpenAI API, usando fallback mock");
                // Fallback para mock em caso de erro
                return await new MockAIService().GetSpecialtyRecommendationAsync(symptoms);
            }
        }

        private string CreateTriagePrompt(string symptoms)
        {
            return $@"Você é um assistente médico especializado em triagem. 
Analise os sintomas descritos e recomende a especialidade médica mais adequada.

Sintomas: {symptoms}

Especialidades disponíveis:
- Cardiologia (problemas cardíacos, dor no peito, palpitações)
- Neurologia (dor de cabeça, enxaqueca, tonturas, problemas neurológicos)
- Pneumologia (tosse, falta de ar, problemas respiratórios)
- Gastroenterologia (dor abdominal, náusea, problemas digestivos)
- Ortopedia (dor nas articulações, ossos, músculos)
- Dermatologia (problemas de pele, alergias cutâneas)
- Psiquiatria (ansiedade, depressão, problemas mentais)
- Otorrinolaringologia (dor de garganta, ouvido, nariz)
- Oftalmologia (problemas de visão, olhos)
- Endocrinologia (diabetes, problemas hormonais)
- Clínica Geral (sintomas gerais, febre, mal-estar)

Responda APENAS com um JSON no formato:
{{
  ""specialty"": ""Nome da Especialidade"",
  ""confidence"": ""Alta|Média|Baixa"",
  ""reasoning"": ""Breve explicação da recomendação""
}}";
        }

        private async Task<string> CallOpenAIAPI(string prompt)
        {
            var requestBody = new
            {
                model = "gpt-5",
                messages = new[]
                {
                    new { role = "system", content = "Você é um assistente médico especializado em triagem. Sempre responda em JSON válido." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 200,
                temperature = 0.3
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"OpenAI API error: {response.StatusCode} - {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return responseContent;
        }

        private TriageResponseDto ParseOpenAIResponse(string openAIResponse)
        {
            try
            {
                using var document = JsonDocument.Parse(openAIResponse);
                var choices = document.RootElement.GetProperty("choices");
                var firstChoice = choices[0];
                var message = firstChoice.GetProperty("message");
                var content = message.GetProperty("content").GetString();

                // Parse do JSON retornado pela IA
                using var aiDocument = JsonDocument.Parse(content);
                var specialty = aiDocument.RootElement.GetProperty("specialty").GetString();
                var confidence = aiDocument.RootElement.GetProperty("confidence").GetString();
                var reasoning = aiDocument.RootElement.TryGetProperty("reasoning", out var reasoningElement)
                    ? reasoningElement.GetString()
                    : "";

                return new TriageResponseDto
                {
                    RecommendedSpecialty = specialty,
                    Confidence = confidence,
                    Reasoning = reasoning
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar resposta da OpenAI");
                // Fallback simples
                return new TriageResponseDto
                {
                    RecommendedSpecialty = "Clínica Geral",
                    Confidence = "Média",
                    Reasoning = "Erro no processamento da IA, recomendação padrão"
                };
            }
        }
    }
}