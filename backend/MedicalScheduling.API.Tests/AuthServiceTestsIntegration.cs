using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MedicalScheduling.API.Data;
using MedicalScheduling.API.DTOs;
using MedicalScheduling.API.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace MedicalScheduling.API.Tests.Integration
{
    public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
    {
        protected readonly WebApplicationFactory<Program> _factory;
        protected readonly HttpClient _client;
        private readonly IServiceScope _scope;
        protected readonly ApplicationDbContext _context;

        public IntegrationTestBase(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext registration
                    services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                    services.RemoveAll(typeof(ApplicationDbContext));

                    // Add in-memory database for testing
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });
                });
            });

            _client = _factory.CreateClient();
            _scope = _factory.Services.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Ensure database is created
            _context.Database.EnsureCreated();
        }

        protected async Task<string> GetAuthTokenAsync(string email = "test@example.com", string password = "Test@123", string role = "patient")
        {
            // Register user first
            var registerDto = new RegisterDto
            {
                Name = "Test User",
                Email = email,
                Password = password,
                Role = role,
                CrmNumber = role == "doctor" ? "CRM/SP 123456" : null,
                Phone = "(11) 99999-9999"
            };

            var registerResponse = await PostAsync("/auth/register", registerDto);
            var registerResult = await DeserializeAsync<LoginResponseDto>(registerResponse);

            return registerResult.Token;
        }

        protected async Task<HttpResponseMessage> PostAsync<T>(string url, T data)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await _client.PostAsync(url, content);
        }

        protected async Task<HttpResponseMessage> GetAsync(string url, string? token = null)
        {
            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return await _client.GetAsync(url);
        }

        protected async Task<T> DeserializeAsync<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public void Dispose()
        {
            _scope.Dispose();
            _client.Dispose();
        }
    }

    public class AuthIntegrationTests : IntegrationTestBase
    {
        public AuthIntegrationTests(WebApplicationFactory<Program> factory) : base(factory) { }

        [Fact]
        public async Task Register_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Name = "João Silva",
                Email = "joao@example.com",
                Password = "StrongPass@123",
                Role = "patient",
                Phone = "(11) 99999-9999"
            };

            // Act
            var response = await PostAsync("/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var result = await DeserializeAsync<LoginResponseDto>(response);
            result.Email.Should().Be(registerDto.Email);
            result.Name.Should().Be(registerDto.Name);
            result.Role.Should().Be("Patient");
            result.Token.Should().NotBeNullOrEmpty();

            // Verify user in database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
            user.Should().NotBeNull();
            user.Name.Should().Be(registerDto.Name);
            user.Role.Should().Be(UserRole.Patient);
        }

        [Fact]
        public async Task Register_Doctor_WithSpecialties_ShouldReturnSuccess()
        {
            // Arrange - Seed specialties first
            var cardiologySpecialty = new Specialty
            {
                Name = "Cardiologia",
                Description = "Doenças do coração",
                Department = "Clínicas",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Specialties.Add(cardiologySpecialty);
            await _context.SaveChangesAsync();

            var registerDto = new RegisterDto
            {
                Name = "Dr. Maria Santos",
                Email = "maria@example.com",
                Password = "DoctorPass@123",
                Role = "doctor",
                CrmNumber = "CRM/SP 654321",
                Phone = "(11) 99999-8888"
            };

            // Act
            var response = await PostAsync("/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var result = await DeserializeAsync<LoginResponseDto>(response);
            result.Role.Should().Be("Doctor");

            var doctor = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
            doctor.Should().NotBeNull();
            doctor.CrmNumber.Should().Be(registerDto.CrmNumber);
            doctor.Role.Should().Be(UserRole.Doctor);
        }

        [Fact]
        public async Task Register_WithWeakPassword_ShouldReturnBadRequest()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Name = "Test User",
                Email = "weak@example.com",
                Password = "123", // Weak password
                Role = "patient"
            };

            // Act
            var response = await PostAsync("/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ShouldReturnBadRequest()
        {
            // Arrange
            var email = "duplicate@example.com";
            await GetAuthTokenAsync(email, "FirstUser@123");

            var duplicateRegister = new RegisterDto
            {
                Name = "Second User",
                Email = email,
                Password = "SecondUser@123",
                Role = "patient"
            };

            // Act
            var response = await PostAsync("/auth/register", duplicateRegister);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange
            var email = "login@example.com";
            var password = "LoginTest@123";
            await GetAuthTokenAsync(email, password);

            var loginDto = new LoginDto
            {
                Email = email,
                Password = password
            };

            // Act
            var response = await PostAsync("/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var result = await DeserializeAsync<LoginResponseDto>(response);
            result.Token.Should().NotBeNullOrEmpty();
            result.Email.Should().Be(email);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "WrongPassword@123"
            };

            // Act
            var response = await PostAsync("/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }
    }

    public class AppointmentIntegrationTests : IntegrationTestBase
    {
        public AppointmentIntegrationTests(WebApplicationFactory<Program> factory) : base(factory) { }

        [Fact]
        public async Task CreateAppointment_AsPatient_ShouldReturnSuccess()
        {
            // Arrange
            await SeedSpecialtiesAndDoctors();
            var token = await GetAuthTokenAsync("patient@test.com", "Patient@123", "patient");

            var appointmentDto = new CreateAppointmentDto
            {
                AppointmentDate = DateTime.UtcNow.AddDays(1),
                Symptoms = "Dor de cabeça forte há 3 dias, acompanhada de náusea e sensibilidade à luz",
                PreferredSpecialty = "Neurologia"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await PostAsync("/paciente/agendamentos", appointmentDto);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var result = await DeserializeAsync<AppointmentDto>(response);
            result.Symptoms.Should().Be(appointmentDto.Symptoms);
            result.RecommendedSpecialty.Should().NotBeNullOrEmpty();

            // Verify in database
            var appointment = await _context.Appointments.FirstOrDefaultAsync();
            appointment.Should().NotBeNull();
            appointment.Symptoms.Should().Be(appointmentDto.Symptoms);
        }

        [Fact]
        public async Task GetPatientAppointments_ShouldReturnUserAppointments()
        {
            // Arrange
            await SeedSpecialtiesAndDoctors();
            var token = await GetAuthTokenAsync("patient2@test.com", "Patient@123", "patient");

            // Create appointment first
            var appointmentDto = new CreateAppointmentDto
            {
                AppointmentDate = DateTime.UtcNow.AddDays(2),
                Symptoms = "Tosse seca persistente há uma semana",
                PreferredSpecialty = "Pneumologia"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await PostAsync("/paciente/agendamentos", appointmentDto);

            // Act
            var response = await GetAsync("/paciente/agendamentos", token);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var appointments = await DeserializeAsync<List<AppointmentDto>>(response);
            appointments.Should().HaveCount(1);
            appointments[0].Symptoms.Should().Be(appointmentDto.Symptoms);
        }

        [Fact]
        public async Task GetDoctorAppointments_ShouldReturnDoctorAppointments()
        {
            // Arrange
            await SeedSpecialtiesAndDoctors();
            var doctorToken = await GetAuthTokenAsync("doctor@test.com", "Doctor@123", "doctor");
            var patientToken = await GetAuthTokenAsync("patient3@test.com", "Patient@123", "patient");

            // Create appointment as patient
            var appointmentDto = new CreateAppointmentDto
            {
                AppointmentDate = DateTime.UtcNow.AddDays(1),
                Symptoms = "Dor no peito ao fazer esforço físico",
                PreferredSpecialty = "Cardiologia"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", patientToken);
            await PostAsync("/paciente/agendamentos", appointmentDto);

            // Act - Get appointments as doctor
            var today = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");
            var response = await GetAsync($"/medico/agendamentos?data={today}", doctorToken);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var appointments = await DeserializeAsync<List<AppointmentDto>>(response);
            appointments.Should().NotBeEmpty();
        }

        [Fact]
        public async Task CreateAppointment_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var appointmentDto = new CreateAppointmentDto
            {
                AppointmentDate = DateTime.UtcNow.AddDays(1),
                Symptoms = "Test symptoms"
            };

            // Act
            var response = await PostAsync("/paciente/agendamentos", appointmentDto);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }

        private async Task SeedSpecialtiesAndDoctors()
        {
            // Seed specialties
            var specialties = new[]
            {
                new Specialty { Name = "Cardiologia", Description = "Doenças do coração", Department = "Clínicas", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Specialty { Name = "Neurologia", Description = "Doenças do sistema nervoso", Department = "Clínicas", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Specialty { Name = "Pneumologia", Description = "Doenças respiratórias", Department = "Clínicas", IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            _context.Specialties.AddRange(specialties);
            await _context.SaveChangesAsync();

            // Seed a doctor
            var doctor = new User
            {
                Email = "doctor@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("Doctor@123", 12),
                Name = "Dr. Test",
                Role = UserRole.Doctor,
                CrmNumber = "CRM/SP 999999",
                Phone = "(11) 99999-9999",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(doctor);
            await _context.SaveChangesAsync();

            // Assign specialties to doctor
            var doctorSpecialties = specialties.Select(s => new DoctorSpecialty
            {
                DoctorId = doctor.Id,
                SpecialtyId = s.Id,
                IsPrimary = s.Name == "Cardiologia",
                LicenseNumber = doctor.CrmNumber,
                CertificationDate = DateTime.UtcNow.AddYears(-5),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

            _context.DoctorSpecialties.AddRange(doctorSpecialties);
            await _context.SaveChangesAsync();
        }
    }

    public class TriageIntegrationTests : IntegrationTestBase
    {
        public TriageIntegrationTests(WebApplicationFactory<Program> factory) : base(factory) { }

        [Fact]
        public async Task MockTriage_WithHeadacheSymptoms_ShouldRecommendNeurology()
        {
            // Arrange
            var triageRequest = new TriageRequestDto
            {
                Symptoms = "Dor de cabeça intensa, sensibilidade à luz e náusea"
            };

            // Act
            var response = await PostAsync("/mock/triagem", triageRequest);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var result = await DeserializeAsync<TriageResponseDto>(response);
            result.RecommendedSpecialty.Should().Be("Neurologia");
            result.Confidence.Should().NotBeNullOrEmpty();
            result.Reasoning.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task MockTriage_WithChestPainSymptoms_ShouldRecommendCardiology()
        {
            // Arrange
            var triageRequest = new TriageRequestDto
            {
                Symptoms = "Dor no peito ao fazer esforço, palpitações e falta de ar"
            };

            // Act
            var response = await PostAsync("/mock/triagem", triageRequest);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var result = await DeserializeAsync<TriageResponseDto>(response);
            result.RecommendedSpecialty.Should().Be("Cardiologia");
        }

        [Fact]
        public async Task MockTriage_WithGeneralSymptoms_ShouldRecommendGeneralMedicine()
        {
            // Arrange
            var triageRequest = new TriageRequestDto
            {
                Symptoms = "Mal-estar geral, cansaço e fraqueza"
            };

            // Act
            var response = await PostAsync("/mock/triagem", triageRequest);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

            var result = await DeserializeAsync<TriageResponseDto>(response);
            result.RecommendedSpecialty.Should().Be("Clínica Geral");
        }
    }

    public class ValidationIntegrationTests : IntegrationTestBase
    {
        public ValidationIntegrationTests(WebApplicationFactory<Program> factory) : base(factory) { }

        [Fact]
        public async Task CreateAppointment_InPast_ShouldReturnBadRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync("validation@test.com", "Test@123", "patient");

            var appointmentDto = new CreateAppointmentDto
            {
                AppointmentDate = DateTime.UtcNow.AddDays(-1), // Past date
                Symptoms = "Test symptoms"
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await PostAsync("/paciente/agendamentos", appointmentDto);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateAppointment_WithEmptySymptoms_ShouldReturnBadRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync("validation2@test.com", "Test@123", "patient");

            var appointmentDto = new CreateAppointmentDto
            {
                AppointmentDate = DateTime.UtcNow.AddDays(1),
                Symptoms = "" // Empty symptoms
            };

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await PostAsync("/paciente/agendamentos", appointmentDto);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
        }
    }
}