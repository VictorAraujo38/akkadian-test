using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MedicalScheduling.API.Data;
using MedicalScheduling.API.DTOs;
using MedicalScheduling.API.Models;
using System.Text.Json;
using System.Text;
using Xunit;
using FluentAssertions;
using System.Net.Http.Headers;
using BCrypt.Net;

namespace MedicalScheduling.API.Tests.Integration
{
    public class AppointmentControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private readonly ApplicationDbContext _context;

        public AppointmentControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("AppointmentTestDatabase_" + Guid.NewGuid());
                    });
                });
            });

            _client = _factory.CreateClient();

            var scope = _factory.Services.CreateScope();
            _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _context.Database.EnsureCreated();

            SeedTestData().Wait();
        }

        private async Task SeedTestData()
        {
            // Criar especialidades
            var specialties = new[]
            {
                new Specialty { Name = "Cardiologia", Description = "Coração", Department = "Clínicas", IsActive = true },
                new Specialty { Name = "Neurologia", Description = "Sistema nervoso", Department = "Clínicas", IsActive = true },
                new Specialty { Name = "Clínica Geral", Description = "Medicina geral", Department = "Clínicas", IsActive = true }
            };

            _context.Specialties.AddRange(specialties);
            await _context.SaveChangesAsync();

            // Criar usuários de teste
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("Senha@123", 12);

            var patient = new User
            {
                Email = "patient@test.com",
                Password = hashedPassword,
                Name = "Paciente Teste",
                Role = UserRole.Patient,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var doctor = new User
            {
                Email = "doctor@test.com",
                Password = hashedPassword,
                Name = "Dr. Teste",
                Role = UserRole.Doctor,
                CrmNumber = "CRM/SP 123456",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.AddRange(patient, doctor);
            await _context.SaveChangesAsync();

            // Associar médico com especialidade
            var doctorSpecialty = new DoctorSpecialty
            {
                DoctorId = doctor.Id,
                SpecialtyId = specialties[0].Id, // Cardiologia
                IsPrimary = true,
                LicenseNumber = "CRM/SP 123456",
                CertificationDate = DateTime.UtcNow.AddYears(-5),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.DoctorSpecialties.Add(doctorSpecialty);
            await _context.SaveChangesAsync();
        }

        private async Task<string> GetPatientTokenAsync()
        {
            var loginDto = new LoginDto
            {
                Email = "patient@test.com",
                Password = "Senha@123"
            };

            var json = JsonSerializer.Serialize(loginDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/auth/login", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoginResponseDto>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result.Token;
        }

        private async Task<string> GetDoctorTokenAsync()
        {
            var loginDto = new LoginDto
            {
                Email = "doctor@test.com",
                Password = "Senha@123"
            };

            var json = JsonSerializer.Serialize(loginDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/auth/login", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LoginResponseDto>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result.Token;
        }

        [Fact]
        public async Task CreateAppointment_ValidData_ShouldCreateSuccessfully()
        {
            // Arrange
            var token = await GetPatientTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var appointmentDto = new CreateAppointmentDto
            {
                AppointmentDate = DateTime.UtcNow.AddDays(1).Date.AddHours(10), // Amanhã às 10h
                Symptoms = "Dor no peito e falta de ar",
                PreferredSpecialty = "Cardiologia"
            };

            var json = JsonSerializer.Serialize(appointmentDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/paciente/agendamentos", content);

            // Assert
            response.Should().BeSuccessful();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AppointmentDto>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            result.Should().NotBeNull();
            result.Symptoms.Should().Be("Dor no peito e falta de ar");
            result.RecommendedSpecialty.Should().NotBeNullOrEmpty();

            // Verificar se foi criado no banco
            var appointment = await _context.Appointments.FirstOrDefaultAsync();
            appointment.Should().NotBeNull();
            appointment.Symptoms.Should().Be("Dor no peito e falta de ar");
        }

        [Fact]
        public async Task CreateAppointment_PastDate_ShouldReturnBadRequest()
        {
            // Arrange
            var token = await GetPatientTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var appointmentDto = new CreateAppointmentDto
            {
                AppointmentDate = DateTime.UtcNow.AddDays(-1), // Data no passado
                Symptoms = "Sintomas de teste",
                PreferredSpecialty = "Cardiologia"
            };

            var json = JsonSerializer.Serialize(appointmentDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/paciente/agendamentos", content);

            // Assert
            response.Should().HaveClientError();
        }

        [Fact]
        public async Task GetPatientAppointments_ValidToken_ShouldReturnAppointments()
        {
            // Arrange - Criar agendamento primeiro
            var token = await GetPatientTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var patient = await _context.Users.FirstAsync(u => u.Email == "patient@test.com");
            var specialty = await _context.Specialties.FirstAsync();

            var appointment = new Appointment
            {
                PatientId = patient.Id,
                SpecialtyId = specialty.Id,
                AppointmentDate = DateTime.UtcNow.AddDays(1),
                Symptoms = "Teste de sintomas",
                RecommendedSpecialty = "Cardiologia",
                TriageReasoning = "Teste de triagem",
                Status = AppointmentStatus.Scheduled,
                Notes = "",
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // Act
            var response = await _client.GetAsync("/paciente/agendamentos");

            // Assert
            response.Should().BeSuccessful();

            var responseContent = await response.Content.ReadAsStringAsync();
            var appointments = JsonSerializer.Deserialize<List<AppointmentDto>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            appointments.Should().NotBeNull();
            appointments.Should().HaveCount(1);
            appointments.First().Symptoms.Should().Be("Teste de sintomas");
        }

        [Fact]
        public async Task GetDoctorAppointments_ValidDate_ShouldReturnAppointments()
        {
            // Arrange
            var doctorToken = await GetDoctorTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", doctorToken);

            var doctor = await _context.Users.FirstAsync(u => u.Email == "doctor@test.com");
            var patient = await _context.Users.FirstAsync(u => u.Email == "patient@test.com");
            var specialty = await _context.Specialties.FirstAsync();

            var testDate = DateTime.UtcNow.AddDays(1).Date.AddHours(10);

            var appointment = new Appointment
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                SpecialtyId = specialty.Id,
                AppointmentDate = testDate,
                Symptoms = "Consulta de teste",
                RecommendedSpecialty = "Cardiologia",
                TriageReasoning = "Triagem automática",
                Status = AppointmentStatus.Scheduled,
                Notes = "",
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // Act
            var response = await _client.GetAsync($"/medico/agendamentos?data={testDate:yyyy-MM-dd}");

            // Assert
            response.Should().BeSuccessful();

            var responseContent = await response.Content.ReadAsStringAsync();
            var appointments = JsonSerializer.Deserialize<List<AppointmentDto>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            appointments.Should().NotBeNull();
            appointments.Should().HaveCount(1);
            appointments.First().PatientName.Should().Be("Paciente Teste");
        }

        [Fact]
        public async Task CreateAppointment_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var appointmentDto = new CreateAppointmentDto
            {
                AppointmentDate = DateTime.UtcNow.AddDays(1),
                Symptoms = "Sintomas sem auth",
                PreferredSpecialty = "Cardiologia"
            };

            var json = JsonSerializer.Serialize(appointmentDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/paciente/agendamentos", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetAvailableTimeSlots_ValidDate_ShouldReturnSlots()
        {
            // Arrange
            var token = await GetPatientTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var tomorrow = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

            // Act
            var response = await _client.GetAsync($"/appointments/available-slots?date={tomorrow}");

            // Assert
            response.Should().BeSuccessful();

            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().NotBeNullOrEmpty();

            // Verificar se retornou algum horário disponível
            var availableSlots = JsonSerializer.Deserialize<JsonElement>(responseContent);
            availableSlots.ValueKind.Should().NotBe(JsonValueKind.Null);
        }

        [Fact]
        public async Task TriageMock_ValidSymptoms_ShouldReturnSpecialty()
        {
            // Arrange
            var triageRequest = new TriageRequestDto
            {
                Symptoms = "dor no peito e palpitações"
            };

            var json = JsonSerializer.Serialize(triageRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/mock/triagem", content);

            // Assert
            response.Should().BeSuccessful();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TriageResponseDto>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            result.Should().NotBeNull();
            result.RecommendedSpecialty.Should().NotBeNullOrEmpty();
            result.Confidence.Should().NotBeNullOrEmpty();
            result.Reasoning.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetSpecialties_ShouldReturnAllSpecialties()
        {
            // Act
            var response = await _client.GetAsync("/specialties");

            // Assert
            response.Should().BeSuccessful();

            var responseContent = await response.Content.ReadAsStringAsync();
            var specialties = JsonSerializer.Deserialize<List<SpecialtyDto>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            specialties.Should().NotBeNull();
            specialties.Should().HaveCountGreaterThan(0);
            specialties.Should().Contain(s => s.Name == "Cardiologia");
            specialties.Should().Contain(s => s.Name == "Neurologia");
        }

        public void Dispose()
        {
            _context?.Dispose();
            _client?.Dispose();
        }
    }
}
