using MedicalScheduling.API.DTOs;

namespace MedicalScheduling.API.Services
{
    public class MockAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MockAIService> _logger;

        // Mapeamento mais detalhado de sintomas para especialidades
        private readonly Dictionary<string, (string specialty, string reasoning)> _symptomSpecialtyMap = new()
        {
            // Cardiologia
            { "dor no peito", ("Cardiologia", "Dor no peito pode indicar problemas cardíacos que requerem avaliação especializada") },
            { "palpitações", ("Cardiologia", "Palpitações cardíacas necessitam de investigação cardiológica") },
            { "falta de ar cardíaca", ("Cardiologia", "Dispneia pode estar relacionada a problemas cardíacos") },
            { "pressão alta", ("Cardiologia", "Hipertensão arterial requer acompanhamento cardiológico") },
            
            // Neurologia
            { "dor de cabeça", ("Neurologia", "Cefaleia persistente pode indicar necessidade de avaliação neurológica") },
            { "enxaqueca", ("Neurologia", "Enxaquecas requerem tratamento neurológico especializado") },
            { "tontura", ("Neurologia", "Tonturas podem ter origem neurológica e necessitam investigação") },
            { "convulsão", ("Neurologia", "Convulsões requerem avaliação neurológica urgente") },
            { "formigamento", ("Neurologia", "Parestesias podem indicar problemas neurológicos") },
            
            // Pneumologia
            { "tosse", ("Pneumologia", "Tosse persistente requer avaliação pulmonar especializada") },
            { "falta de ar", ("Pneumologia", "Dispneia necessita de investigação respiratória") },
            { "chiado no peito", ("Pneumologia", "Sibilos podem indicar problemas respiratórios") },
            { "escarro", ("Pneumologia", "Produção de escarro anormal requer avaliação pneumológica") },
            
            // Gastroenterologia
            { "dor no estômago", ("Gastroenterologia", "Dor abdominal pode necessitar de avaliação gastroenterológica") },
            { "dor abdominal", ("Gastroenterologia", "Dor abdominal persistente requer investigação especializada") },
            { "náusea", ("Gastroenterologia", "Náuseas recorrentes podem indicar problemas gastrointestinais") },
            { "vômito", ("Gastroenterologia", "Vômitos persistentes necessitam de avaliação gastroenterológica") },
            { "diarreia", ("Gastroenterologia", "Diarreia crônica requer investigação gastrointestinal") },
            { "constipação", ("Gastroenterologia", "Constipação persistente pode necessitar de avaliação especializada") },
            { "azia", ("Gastroenterologia", "Sintomas de refluxo requerem acompanhamento gastroenterológico") },
            
            // Ortopedia
            { "dor nas costas", ("Ortopedia", "Dor nas costas pode necessitar de avaliação ortopédica") },
            { "dor nas articulações", ("Ortopedia", "Artralgia requer avaliação ortopédica especializada") },
            { "dor muscular", ("Ortopedia", "Dor muscular persistente pode necessitar de avaliação ortopédica") },
            { "dor no joelho", ("Ortopedia", "Dor articular requer avaliação ortopédica") },
            { "dor no ombro", ("Ortopedia", "Dor no ombro pode necessitar de investigação ortopédica") },
            
            // Dermatologia
            { "coceira", ("Dermatologia", "Prurido persistente requer avaliação dermatológica") },
            { "mancha na pele", ("Dermatologia", "Lesões cutâneas necessitam de avaliação dermatológica") },
            { "alergia", ("Dermatologia", "Reações alérgicas cutâneas requerem acompanhamento dermatológico") },
            { "vermelhidão", ("Dermatologia", "Eritema cutâneo pode necessitar de avaliação especializada") },
            
            // Psiquiatria
            { "ansiedade", ("Psiquiatria", "Sintomas de ansiedade podem necessitar de acompanhamento psiquiátrico") },
            { "depressão", ("Psiquiatria", "Sintomas depressivos requerem avaliação psiquiátrica") },
            { "insônia", ("Psiquiatria", "Distúrbios do sono podem necessitar de acompanhamento especializado") },
            { "estresse", ("Psiquiatria", "Estresse excessivo pode beneficiar de acompanhamento psiquiátrico") },
            
            // Otorrinolaringologia
            { "dor de garganta", ("Otorrinolaringologia", "Sintomas de garganta podem necessitar de avaliação ORL") },
            { "dor de ouvido", ("Otorrinolaringologia", "Otalgia requer avaliação otorrinolaringológica") },
            { "congestão nasal", ("Otorrinolaringologia", "Sintomas nasais persistentes necessitam de avaliação ORL") },
            { "rouquidão", ("Otorrinolaringologia", "Alterações da voz requerem avaliação especializada") },
            
            // Oftalmologia
            { "visão embaçada", ("Oftalmologia", "Alterações visuais requerem avaliação oftalmológica") },
            { "dor nos olhos", ("Oftalmologia", "Dor ocular necessita de investigação oftalmológica") },
            { "vermelhidão nos olhos", ("Oftalmologia", "Hiperemia ocular pode necessitar de avaliação especializada") },
            
            // Endocrinologia
            { "sede excessiva", ("Endocrinologia", "Polidipsia pode indicar distúrbios endócrinos") },
            { "perda de peso", ("Endocrinologia", "Perda de peso inexplicada pode necessitar de avaliação endócrina") },
            { "ganho de peso", ("Endocrinologia", "Alterações ponderais podem indicar distúrbios hormonais") },
            { "fadiga", ("Endocrinologia", "Fadiga persistente pode ter origem endócrina") },
            
            // Sintomas gerais
            { "febre", ("Clínica Geral", "Febre pode ter múltiplas causas e requer avaliação inicial") },
            { "mal-estar", ("Clínica Geral", "Sintomas inespecíficos necessitam de avaliação clínica geral") },
            { "fraqueza", ("Clínica Geral", "Astenia pode ter múltiplas causas e requer investigação inicial") }
        };

        public MockAIService(HttpClient httpClient = null, ILogger<MockAIService> logger = null)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public Task<TriageResponseDto> GetSpecialtyRecommendationAsync(string symptoms)
        {
            try
            {
                _logger?.LogInformation($"Analisando sintomas: {symptoms}");

                var symptomsLower = symptoms.ToLower();

                // Analisar sintomas com pontuação de confiança
                var matches = new List<(string specialty, string reasoning, double score)>();

                foreach (var kvp in _symptomSpecialtyMap)
                {
                    if (symptomsLower.Contains(kvp.Key))
                    {
                        _logger?.LogInformation($"Match encontrado: {kvp.Key} -> {kvp.Value.specialty}");

                        // Calcular score baseado na especificidade do sintoma
                        double score = CalculateSymptomScore(kvp.Key, symptomsLower);
                        matches.Add((kvp.Value.specialty, kvp.Value.reasoning, score));
                    }
                }

                if (matches.Any())
                {
                    // Pegar a especialidade com maior score
                    var bestMatch = matches.OrderByDescending(m => m.score).First();

                    _logger?.LogInformation($"Melhor match: {bestMatch.specialty} (score: {bestMatch.score})");

                    var confidence = bestMatch.score > 0.8 ? "Alta" :
                                   bestMatch.score > 0.5 ? "Média" : "Baixa";

                    return Task.FromResult(new TriageResponseDto
                    {
                        RecommendedSpecialty = bestMatch.specialty,
                        Confidence = confidence,
                        Reasoning = bestMatch.reasoning
                    });
                }

                // Se não encontrou correspondências específicas, usar análise por palavras-chave
                _logger?.LogInformation("Nenhum match específico encontrado, usando análise geral");
                var generalAnalysis = AnalyzeGeneralSymptoms(symptomsLower);

                return Task.FromResult(generalAnalysis);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erro no serviço de triagem mock");

                return Task.FromResult(new TriageResponseDto
                {
                    RecommendedSpecialty = "Clínica Geral",
                    Confidence = "Baixa",
                    Reasoning = "Erro na análise, recomendação de avaliação geral"
                });
            }
        }

        private double CalculateSymptomScore(string keyword, string symptoms)
        {
            // Score baseado na especificidade e presença do sintoma
            double baseScore = 0.6;

            // Bonus se o sintoma aparece múltiplas vezes
            int occurrences = CountOccurrences(symptoms, keyword);
            if (occurrences > 1) baseScore += 0.1;

            // Bonus para sintomas mais específicos (palavras maiores)
            if (keyword.Length > 10) baseScore += 0.2;

            // Bonus se há palavras relacionadas próximas
            if (HasRelatedWords(symptoms, keyword)) baseScore += 0.1;

            return Math.Min(baseScore, 1.0);
        }

        private int CountOccurrences(string text, string keyword)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(keyword, index)) != -1)
            {
                count++;
                index += keyword.Length;
            }
            return count;
        }

        private bool HasRelatedWords(string symptoms, string keyword)
        {
            var relatedWords = new Dictionary<string, string[]>
            {
                { "dor no peito", new[] { "aperto", "pressão", "queimação", "irradiação" } },
                { "dor de cabeça", new[] { "latejante", "pressão", "enxaqueca", "cefaleia" } },
                { "tosse", new[] { "seca", "produtiva", "catarro", "escarro" } },
                { "febre", new[] { "temperatura", "calafrio", "sudorese" } }
            };

            if (relatedWords.ContainsKey(keyword))
            {
                return relatedWords[keyword].Any(word => symptoms.Contains(word));
            }

            return false;
        }

        private TriageResponseDto AnalyzeGeneralSymptoms(string symptoms)
        {
            // Análise de emergência
            var emergencyKeywords = new[] { "dor intensa", "sangramento", "dificuldade para respirar", "desmaio", "convulsão" };
            if (emergencyKeywords.Any(keyword => symptoms.Contains(keyword)))
            {
                return new TriageResponseDto
                {
                    RecommendedSpecialty = "Pronto Socorro",
                    Confidence = "Alta",
                    Reasoning = "Sintomas sugerem necessidade de atendimento de emergência"
                };
            }

            // Análise por idade (se mencionada)
            if (symptoms.Contains("criança") || symptoms.Contains("bebê") || symptoms.Contains("pediatria"))
            {
                return new TriageResponseDto
                {
                    RecommendedSpecialty = "Pediatria",
                    Confidence = "Alta",
                    Reasoning = "Sintomas em paciente pediátrico requerem avaliação especializada"
                };
            }

            // Análise por gênero específico
            if (symptoms.Contains("menstruação") || symptoms.Contains("gravidez") || symptoms.Contains("ginecologia"))
            {
                return new TriageResponseDto
                {
                    RecommendedSpecialty = "Ginecologia",
                    Confidence = "Alta",
                    Reasoning = "Sintomas relacionados à saúde feminina"
                };
            }

            // Default para sintomas inespecíficos
            return new TriageResponseDto
            {
                RecommendedSpecialty = "Clínica Geral",
                Confidence = "Média",
                Reasoning = "Sintomas inespecíficos necessitam de avaliação clínica inicial para direcionamento adequado"
            };
        }
    }
}