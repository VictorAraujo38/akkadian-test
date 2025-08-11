using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MedicalScheduling.API.Data;
using MedicalScheduling.API.DTOs;
using System.Text.Json;
using System.Text;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace MedicalScheduling.API.Tests.Integration
{
    public class AuthControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly ApplicationDbContext _context;

        public AuthControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove o DbContext existente
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Adiciona DbContext em memória para testes
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid());
                    });

                    // Configuração JWT para testes
                    services.Configure<Dictionary<string, string>>(options =>
                    {
                        options["JwtSettings:SecretKey"] = "test-secret-key-minimum-32-characters-long-for-security!!";
                        options["JwtSettings:Issuer"] = "TestIssuer";
                        options["JwtSettings:Audience"] = "TestAudience";
                    });
                });
            });

            _client = _factory.CreateClient();

            // Configurar contexto para testes
            var scope = _factory.Services.CreateScope();
            _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task Register_ValidPatient_ShouldReturnSuccess()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Name = "João Silva",
                Email = "joao.test@example.com",
                Password = "Senha@123",
                Role = "patient",
                Phone = "(11) 99999-9999"
            };

            var json = JsonSerializer.Serialize(registerDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/auth/register", content);

            // Assert
            response.Should().BeSuccessful();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoginResponseDto>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            result.Should().NotBeNull();
            result.Email.Should().Be("joao.test@example.com");
            result.Name.Should().Be("João Silva");
            result.Role.Should().Be("Patient");
            result.Token.Should().NotBeNullOrEmpty();

            // Verificar se usuário foi criado no banco
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "joao.test@example.com");
            user.Should().NotBeNull();
            user.Role.Should().Be(Models.UserRole.Patient);
            user.IsActive.Should().BeTrue();

            // Verificar se a senha foi hasheada com BCrypt
            user.Password.Should().NotBe("Senha@123"); // Não deve ser texto plano
            user.Password.Should().StartWith("$2"); // BCrypt hash sempre começa com $2
        }

        [Fact]
        public async Task Register_ValidDoctor_ShouldReturnSuccess()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Name = "Dr. Maria Santos",
                Email = "maria.santos@example.com",
                Password = "MinhaSenha@456",
                Role = "doctor",
                CrmNumber = "CRM/SP 123456",
                Phone = "(11) 98888-8888"
            };

            var json = JsonSerializer.Serialize(registerDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/auth/register", content);

            // Assert
            response.Should().BeSuccessful();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoginResponseDto>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            result.Should().NotBeNull();
            result.Role.Should().Be("Doctor");

            // Verificar se médico foi criado corretamente
            var doctor = await _context.Users.FirstOrDefaultAsync(u => u.Email == "maria.santos@example.com");
            doctor.Should().NotBeNull();
            doctor.Role.Should().Be(Models.UserRole.Doctor);
            doctor.CrmNumber.Should().Be("CRM/SP 123456");
        }

        [Fact]
        public async Task Register_WeakPassword_ShouldReturnBadRequest()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Name = "Test User",
                Email = "test@example.com",
                Password = "123", // Senha fraca
                Role = "patient"
            };

            var json = JsonSerializer.Serialize(registerDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/auth/register", content);

            // Assert
            response.Should().HaveClientError();
        }

        [Fact]
        public async Task Register_DuplicateEmail_ShouldReturnBadRequest()
        {
            // Arrange - Primeiro usuário
            var firstUser = new RegisterDto
            {
                Name = "Primeiro Usuário",
                Email = "duplicate@example.com",
                Password = "Senha@123",
                Role = "patient"
            };

            var json1 = JsonSerializer.Serialize(firstUser);
            var content1 = new StringContent(json1, Encoding.UTF8, "application/json");

            await _client.PostAsync("/auth/register", content1);

            // Segundo usuário com mesmo email
            var secondUser = new RegisterDto
            {
                Name = "Segundo Usuário",
                Email = "duplicate@example.com",
                Password = "OutraSenha@456",
                Role = "patient"
            };

            var json2 = JsonSerializer.Serialize(secondUser);
            var content2 = new StringContent(json2, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/auth/register", content2);

            // Assert
            response.Should().HaveClientError();
        }

        [Fact]
        public async Task Login_ValidCredentials_ShouldReturnToken()
        {
            // Arrange - Registrar usuário primeiro
            var registerDto = new RegisterDto
            {
                Name = "Login Test",
                Email = "login@example.com",
                Password = "Senha@123",
                Role = "patient"
            };

            var registerJson = JsonSerializer.Serialize(registerDto);
            var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
            await _client.PostAsync("/auth/register", registerContent);

            // Login
            var loginDto = new LoginDto
            {
                Email = "login@example.com",
                Password = "Senha@123"
            };

            var loginJson = JsonSerializer.Serialize(loginDto);
            var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/auth/login", loginContent);

            // Assert
            response.Should().BeSuccessful();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoginResponseDto>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            result.Should().NotBeNull();
            result.Token.Should().NotBeNullOrEmpty();
            result.Email.Should().Be("login@example.com");
        }

        [Fact]
        public async Task Login_InvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "WrongPassword@123"
            };

            var json = JsonSerializer.Serialize(loginDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/auth/login", content);

            // Assert
            response.Should().HaveClientError();
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_CorrectEmailWrongPassword_ShouldReturnUnauthorized()
        {
            // Arrange - Registrar usuário primeiro
            var registerDto = new RegisterDto
            {
                Name = "Wrong Password Test",
                Email = "wrongpassword@example.com",
                Password = "CorrectPassword@123",
                Role = "patient"
            };

            var registerJson = JsonSerializer.Serialize(registerDto);
            var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
            await _client.PostAsync("/auth/register", registerContent);

            // Tentar login com senha errada
            var loginDto = new LoginDto
            {
                Email = "wrongpassword@example.com",
                Password = "WrongPassword@123"
            };

            var loginJson = JsonSerializer.Serialize(loginDto);
            var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/auth/login", loginContent);

            // Assert
            response.Should().HaveClientError();
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }

        public void Dispose()
        {
            _context?.Dispose();
            _client?.Dispose();
        }
    }
}