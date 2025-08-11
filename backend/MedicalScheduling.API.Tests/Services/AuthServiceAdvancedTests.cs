using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MedicalScheduling.API.Data;
using MedicalScheduling.API.DTOs;
using MedicalScheduling.API.Models;
using MedicalScheduling.API.Services;
using Moq;
using Xunit;
using FluentAssertions;
using System.IdentityModel.Tokens.Jwt;

namespace MedicalScheduling.API.Tests.Services
{
    public class AuthServiceAdvancedTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly IConfiguration _configuration;

        public AuthServiceAdvancedTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            _mockLogger = new Mock<ILogger<AuthService>>();

            var configurationData = new Dictionary<string, string>
            {
                {"JwtSettings:SecretKey", "test-secret-key-minimum-32-characters-long-for-security!!"},
                {"JwtSettings:Issuer", "TestIssuer"},
                {"JwtSettings:Audience", "TestAudience"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();

            _authService = new AuthService(_context, _configuration, _mockLogger.Object);
        }

        [Theory]
        [InlineData("password", false)] // Muito simples
        [InlineData("Password1", false)] // Sem caractere especial
        [InlineData("password@", false)] // Sem maiúscula
        [InlineData("PASSWORD@123", false)] // Sem minúscula
        [InlineData("Password@", false)] // Sem número
        [InlineData("Pass@1", false)] // Muito curta
        [InlineData("Password@123", true)] // Válida
        [InlineData("MinhaSenh@Super123", true)] // Válida complexa
        public async Task Register_PasswordValidation_ShouldValidateCorrectly(string password, bool expectedValid)
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Name = "Test User",
                Email = $"test{Guid.NewGuid()}@example.com",
                Password = password,
                Role = "patient"
            };

            // Act & Assert
            if (expectedValid)
            {
                var result = await _authService.RegisterAsync(registerDto);
                result.Should().NotBeNull();
                result.Token.Should().NotBeNullOrEmpty();
            }
            else
            {
                var act = () => _authService.RegisterAsync(registerDto);
                await act.Should().ThrowAsync<ArgumentException>()
                    .WithMessage("*Senha deve ter pelo menos 8 caracteres*");
            }
        }

        [Fact]
        public async Task Register_BCryptHashing_ShouldHashPasswordCorrectly()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Name = "Hash Test User",
                Email = "hashtest@example.com",
                Password = "TestPassword@123",
                Role = "patient"
            };

            // Act
            await _authService.RegisterAsync(registerDto);

            // Assert
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "hashtest@example.com");
            user.Should().NotBeNull();

            // Verificar se é hash BCrypt
            user.Password.Should().StartWith("$2");
            user.Password.Should().NotBe("TestPassword@123");

            // Verificar se pode verificar a senha
            var isValid = BCrypt.Net.BCrypt.Verify("TestPassword@123", user.Password);
            isValid.Should().BeTrue();

            // Verificar se senha errada não passa
            var isInvalid = BCrypt.Net.BCrypt.Verify("WrongPassword", user.Password);
            isInvalid.Should().BeFalse();
        }

        [Fact]
        public async Task Login_BCryptVerification_ShouldVerifyCorrectly()
        {
            // Arrange - Criar usuário com BCrypt hash
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("MySecurePassword@123", 12);

            var user = new User
            {
                Email = "bcrypttest@example.com",
                Password = hashedPassword,
                Name = "BCrypt Test",
                Role = UserRole.Patient,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = "bcrypttest@example.com",
                Password = "MySecurePassword@123"
            };

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
            result.Email.Should().Be("bcrypttest@example.com");
        }

        [Fact]
        public async Task Login_WrongPassword_ShouldThrowUnauthorized()
        {
            // Arrange
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("CorrectPassword@123", 12);

            var user = new User
            {
                Email = "wrongpasstest@example.com",
                Password = hashedPassword,
                Name = "Wrong Pass Test",
                Role = UserRole.Patient,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = "wrongpasstest@example.com",
                Password = "WrongPassword@123"
            };

            // Act & Assert
            var act = () => _authService.LoginAsync(loginDto);
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Email ou senha inválidos");
        }

        [Fact]
        public async Task Login_InactiveUser_ShouldThrowUnauthorized()
        {
            // Arrange
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("Password@123", 12);

            var user = new User
            {
                Email = "inactive@example.com",
                Password = hashedPassword,
                Name = "Inactive User",
                Role = UserRole.Patient,
                IsActive = false, // Usuário inativo
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = "inactive@example.com",
                Password = "Password@123"
            };

            // Act & Assert
            var act = () => _authService.LoginAsync(loginDto);
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Conta desativada");
        }

        [Fact]
        public async Task Register_EmailNormalization_ShouldNormalizeEmail()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Name = "Email Test",
                Email = "  TEST@EXAMPLE.COM  ", // Email com espaços e maiúsculas
                Password = "Password@123",
                Role = "patient"
            };

            // Act
            await _authService.RegisterAsync(registerDto);

            // Assert
            var user = await _context.Users.FirstOrDefaultAsync();
            user.Should().NotBeNull();
            user.Email.Should().Be("test@example.com"); // Deve estar normalizado
        }

        [Fact]
        public async Task GenerateJwtToken_ShouldContainCorrectClaims()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Name = "JWT Test User",
                Email = "jwttest@example.com",
                Password = "Password@123",
                Role = "doctor",
                CrmNumber = "CRM/SP 123456"
            };

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            result.Token.Should().NotBeNullOrEmpty();

            // Decodificar e verificar claims do JWT
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(result.Token);

            token.Claims.Should().Contain(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
            token.Claims.Should().Contain(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress" && c.Value == "jwttest@example.com");
            token.Claims.Should().Contain(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name" && c.Value == "JWT Test User");
            token.Claims.Should().Contain(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" && c.Value == "Doctor");
            token.Claims.Should().Contain(c => c.Type == "userId");
            token.Claims.Should().Contain(c => c.Type == "isActive" && c.Value == "True");
            token.Claims.Should().Contain(c => c.Type == "jti"); // JWT ID
            token.Claims.Should().Contain(c => c.Type == "iat"); // Issued At

            // Verificar expiração (7 dias)
            token.ValidTo.Should().BeAfter(DateTime.UtcNow.AddDays(6));
            token.ValidTo.Should().BeBefore(DateTime.UtcNow.AddDays(8));
        }

        [Fact]
        public async Task Register_DuplicateEmail_ShouldThrowInvalidOperation()
        {
            // Arrange
            var firstUser = new RegisterDto
            {
                Name = "First User",
                Email = "duplicate@example.com",
                Password = "Password@123",
                Role = "patient"
            };

            var secondUser = new RegisterDto
            {
                Name = "Second User",
                Email = "duplicate@example.com",
                Password = "AnotherPassword@456",
                Role = "doctor"
            };

            // Act
            await _authService.RegisterAsync(firstUser);

            var act = () => _authService.RegisterAsync(secondUser);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Email já está em uso");
        }

        [Fact]
        public async Task Register_DoctorWithCrm_ShouldStoreCrmCorrectly()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Name = "Dr. Test",
                Email = "doctor@test.com",
                Password = "Password@123",
                Role = "doctor",
                CrmNumber = "  CRM/SP 123456  ", // Com espaços
                Phone = "  (11) 99999-9999  "
            };

            // Act
            await _authService.RegisterAsync(registerDto);

            // Assert
            var doctor = await _context.Users.FirstOrDefaultAsync(u => u.Email == "doctor@test.com");
            doctor.Should().NotBeNull();
            doctor.Role.Should().Be(UserRole.Doctor);
            doctor.CrmNumber.Should().Be("CRM/SP 123456"); // Espaços removidos
            doctor.Phone.Should().Be("(11) 99999-9999");
        }

        [Fact]
        public async Task Login_NonExistentUser_ShouldHaveDelay()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "Password@123"
            };

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var act = () => _authService.LoginAsync(loginDto);
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            stopwatch.Stop();

            // Assert - Deve ter delay de pelo menos 1 segundo (para prevenir timing attacks)
            stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(900);
        }

        [Fact]
        public void Dispose_ShouldCleanupResources()
        {
            // Act & Assert - Não deve lançar exceção
            var act = () => Dispose();
            act.Should().NotThrow();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}