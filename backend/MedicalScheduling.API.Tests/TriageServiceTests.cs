using Xunit;
using MedicalScheduling.API.Services;
using System.Threading.Tasks;

namespace MedicalScheduling.API.Tests
{
    public class TriageServiceTests
    {
        [Theory]
        [InlineData("dor de cabeça intensa", "Neurologia")]
        [InlineData("febre alta e calafrios", "Clínica Geral")]
        [InlineData("tosse persistente", "Pneumologia")]
        [InlineData("dor no peito", "Cardiologia")]
        [InlineData("sintomas desconhecidos", "Clínica Geral")]
        public async Task ProcessTriageAsync_Should_Return_Expected_Specialty(string symptoms, string expectedSpecialty)
        {
            // Arrange
            var mockAIService = new MockAIService();
            var triageService = new TriageService(mockAIService);

            // Act
            var result = await triageService.ProcessTriageAsync(symptoms);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedSpecialty, result.RecommendedSpecialty);
            Assert.NotNull(result.Confidence);
        }
    }
}