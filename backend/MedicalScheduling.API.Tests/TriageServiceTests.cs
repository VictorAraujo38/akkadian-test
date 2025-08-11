using Xunit;
using MedicalScheduling.API.Services;
using System.Threading.Tasks;

namespace MedicalScheduling.API.Tests
{
    public class TriageServiceTests
    {
        [Theory]
        [InlineData("dor de cabeça", "Neurologia")]
        [InlineData("dor no peito", "Cardiologia")]
        [InlineData("tosse", "Pneumologia")]
        [InlineData("dor abdominal", "Gastroenterologia")]
        [InlineData("sintomas gerais", "Clínica Geral")]
        public async Task MockAIService_Should_Return_Expected_Specialty(string symptoms, string expectedSpecialty)
        {
            // Arrange
            var mockAIService = new MockAIService();
            
            // Act
            var result = await mockAIService.GetSpecialtyRecommendationAsync(symptoms);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedSpecialty, result.RecommendedSpecialty);
            Assert.NotNull(result.Confidence);
            Assert.NotNull(result.Reasoning);
        }
        
        [Fact]
        public async Task MockAIService_Should_Return_ClinicaGeral_For_Unknown_Symptoms()
        {
            // Arrange
            var mockAIService = new MockAIService();
            var unknownSymptoms = "sintomas muito específicos e desconhecidos";
            
            // Act
            var result = await mockAIService.GetSpecialtyRecommendationAsync(unknownSymptoms);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Clínica Geral", result.RecommendedSpecialty);
        }
    }
}