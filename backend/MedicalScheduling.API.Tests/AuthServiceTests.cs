using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using MedicalScheduling.API.Services;
using MedicalScheduling.API.Data;
using MedicalScheduling.API.DTOs;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace MedicalScheduling.API.Tests
{
    public class AuthServiceTests : IDisposable
    {
        private ApplicationDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        private IConfiguration GetConfiguration()
        {
            var myConfiguration = new Dictionary<string, string>
            {
                {"JwtSettings:SecretKey", "this-is-a-test-secret-key-minimum-32-chars!!"},
                {"JwtSettings:Issuer", "TestIssuer"},
                {"JwtSettings:Audience", "TestAudience"}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(myConfiguration)
                .Build();
        }

        [Fact]
        public async Task RegisterAsync_Should_Create_New_User()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var configuration = GetConfiguration();
            var service = new AuthService(context, configuration);
            
            var registerDto = new RegisterDto
            {
                Email = "test@example.com",
                Password = "hashed",
                CreatedAt = DateTime.UtcNow
            };
            
            var doctor = new User
            {
                Id = 2,
                Email = "doctor@test.com",
                Name = "Test Doctor",
                Role = UserRole.Doctor,
                Password = "hashed",
                CreatedAt = DateTime.UtcNow
            };

            context.Users.AddRange(patient, doctor);

            var appointment1 = new Appointment
            {
                PatientId = 1,
                DoctorId = 2,
                AppointmentDate = DateTime.Now.AddDays(1),
                Symptoms = "Sintoma 1",
                RecommendedSpecialty = "Cardiologia",
                CreatedAt = DateTime.UtcNow
            };

            var appointment2 = new Appointment
            {
                PatientId = 1,
                AppointmentDate = DateTime.Now.AddDays(2),
                Symptoms = "Sintoma 2",
                RecommendedSpecialty = "Neurologia",
                CreatedAt = DateTime.UtcNow
            };

            context.Appointments.AddRange(appointment1, appointment2);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetPatientAppointmentsAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, a => a.RecommendedSpecialty == "Cardiologia");
            Assert.Contains(result, a => a.DoctorName == "Test Doctor");
        }
    }
}password123",
                Name = "Test User",
                Role = "patient"
            };

            // Act
            var result = await service.RegisterAsync(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.Email);
            Assert.Equal("Test User", result.Name);
            Assert.NotNull(result.Token);
            
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
            Assert.NotNull(user);
        }

        [Fact]
        public async Task RegisterAsync_Should_Throw_When_Email_Exists()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var configuration = GetConfiguration();
            var service = new AuthService(context, configuration);
            
            var registerDto = new RegisterDto
            {
                Email = "existing@example.com",
                Password = "password123",
                Name = "Test User",
                Role = "patient"
            };

            // First registration
            await service.RegisterAsync(registerDto);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () => 
                await service.RegisterAsync(registerDto));
        }

        [Fact]
        public async Task LoginAsync_Should_Return_Token_For_Valid_Credentials()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var configuration = GetConfiguration();
            var service = new AuthService(context, configuration);
            
            var registerDto = new RegisterDto
            {
                Email = "login@example.com",
                Password = "password123",
                Name = "Login User",
                Role = "doctor"
            };

            await service.RegisterAsync(registerDto);

            var loginDto = new LoginDto
            {
                Email = "login@example.com",
                Password = "password123"
            };

            // Act
            var result = await service.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("login@example.com", result.Email);
            Assert.Equal("Doctor", result.Role);
            Assert.NotNull(result.Token);
        }

        [Fact]
        public async Task LoginAsync_Should_Throw_For_Invalid_Credentials()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var configuration = GetConfiguration();
            var service = new AuthService(context, configuration);
            
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "wrongpassword"
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(async () => 
                await service.LoginAsync(loginDto));
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}