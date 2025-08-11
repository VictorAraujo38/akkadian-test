using Microsoft.EntityFrameworkCore;
using MedicalScheduling.API.Data;
using MedicalScheduling.API.DTOs;

namespace MedicalScheduling.API.Services
{
    public class TriageService : ITriageService
    {
        private readonly IAIService _aiService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TriageService> _logger;

        public TriageService(IAIService aiService, ApplicationDbContext context, ILogger<TriageService> logger)
        {
            _aiService = aiService;
            _context = context;
            _logger = logger;
        }

        public async Task<TriageResponseDto> ProcessTriageAsync(string symptoms)
        {
            try
            {
                // 1. Processar com AI Service
                var aiResult = await _aiService.GetSpecialtyRecommendationAsync(symptoms);

                // 2. Buscar especialidade no banco de dados
                var specialty = await _context.Specialties
                    .FirstOrDefaultAsync(s => s.Name == aiResult.RecommendedSpecialty && s.IsActive);

                // 3. Se não encontrar a especialidade exata, tentar variações
                if (specialty == null)
                {
                    specialty = await FindSpecialtyByVariations(aiResult.RecommendedSpecialty);
                }

                // 4. Se ainda não encontrar, usar Clínica Geral como fallback
                if (specialty == null)
                {
                    _logger.LogWarning($"Especialidade '{aiResult.RecommendedSpecialty}' não encontrada no banco, usando Clínica Geral");

                    specialty = await _context.Specialties
                        .FirstOrDefaultAsync(s => s.Name == "Clínica Geral" && s.IsActive);

                    if (specialty != null)
                    {
                        aiResult.RecommendedSpecialty = "Clínica Geral";
                        aiResult.Reasoning += " (Especialidade original não encontrada, direcionado para avaliação geral)";
                    }
                }

                // 5. Retornar resultado com SpecialtyId
                return new TriageResponseDto
                {
                    RecommendedSpecialty = aiResult.RecommendedSpecialty,
                    Confidence = aiResult.Confidence,
                    Reasoning = aiResult.Reasoning,
                    SpecialtyId = specialty?.Id
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante processamento de triagem");

                // Fallback em caso de erro
                var fallbackSpecialty = await _context.Specialties
                    .FirstOrDefaultAsync(s => s.Name == "Clínica Geral" && s.IsActive);

                return new TriageResponseDto
                {
                    RecommendedSpecialty = "Clínica Geral",
                    Confidence = "Baixa",
                    Reasoning = "Erro no processamento da triagem, recomendação padrão para avaliação inicial",
                    SpecialtyId = fallbackSpecialty?.Id
                };
            }
        }

        private async Task<Models.Specialty> FindSpecialtyByVariations(string originalSpecialty)
        {
            // Mapeamento de variações comuns de nomes de especialidades
            var specialtyVariations = new Dictionary<string, string[]>
            {
                { "Otorrinolaringologia", new[] { "Otorrino", "ORL", "Ouvido Nariz Garganta" } },
                { "Ginecologia", new[] { "Gineco", "GO", "Ginecologia e Obstetrícia" } },
                { "Pneumologia", new[] { "Pneumo", "Pulmonologia", "Pulmão" } },
                { "Gastroenterologia", new[] { "Gastro", "GE", "Digestivo" } },
                { "Oftalmologia", new[] { "Oftalmo", "Olhos", "Visão" } },
                { "Endocrinologia", new[] { "Endócrino", "Endocrino", "Hormônios" } },
                { "Psiquiatria", new[] { "Psiquiatra", "Mental", "Psiquiátrico" } },
                { "Neurologia", new[] { "Neuro", "Neurológico", "Sistema Nervoso" } },
                { "Cardiologia", new[] { "Cardio", "Coração", "Cardiovascular" } },
                { "Ortopedia", new[] { "Orto", "Ossos", "Traumatologia" } },
                { "Dermatologia", new[] { "Dermato", "Pele", "Dermatológico" } },
                { "Urologia", new[] { "Uro", "Urinário", "Urológico" } },
                { "Oncologia", new[] { "Onco", "Câncer", "Oncológico" } },
                { "Pediatria", new[] { "Pediátrico", "Criança", "Infantil" } },
                { "Geriatria", new[] { "Geriátrico", "Idoso", "Terceira Idade" } },
                { "Reumatologia", new[] { "Reuma", "Articular", "Reumatológico" } },
                { "Nefrologia", new[] { "Nefro", "Rim", "Renal" } },
                { "Hematologia", new[] { "Hemato", "Sangue", "Hematológico" } },
                { "Infectologia", new[] { "Infecto", "Infecciosa", "Doenças Infecciosas" } }
            };

            // Procurar por variações
            foreach (var kvp in specialtyVariations)
            {
                var specialtyName = kvp.Key;
                var variations = kvp.Value;

                // Verificar se a especialidade original corresponde a alguma variação
                if (variations.Any(v => originalSpecialty.Contains(v, StringComparison.OrdinalIgnoreCase)) ||
                    originalSpecialty.Contains(specialtyName, StringComparison.OrdinalIgnoreCase))
                {
                    var specialty = await _context.Specialties
                        .FirstOrDefaultAsync(s => s.Name == specialtyName && s.IsActive);

                    if (specialty != null)
                    {
                        _logger.LogInformation($"Especialidade '{originalSpecialty}' mapeada para '{specialtyName}'");
                        return specialty;
                    }
                }
            }

            return null;
        }
    }
}