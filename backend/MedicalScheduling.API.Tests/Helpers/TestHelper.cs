using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MedicalScheduling.API.Data;
using MedicalScheduling.API.Models;
using BCrypt.Net;
using Bogus;

namespace MedicalScheduling.API.Tests.Helpers
{
    public static class TestHelper
    {
        public static ApplicationDbContext GetInMemoryContext(string databaseName = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: databaseName ?? Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        public static IConfiguration GetTestConfiguration()
        {
            var configurationData = new Dictionary<string, string>
            {
                {"JwtSettings:SecretKey", "test-secret-key-minimum-32-characters-long-for-security!!"},
                {"JwtSettings:Issuer", "TestIssuer"},
                {"JwtSettings:Audience", "TestAudience"},
                {"TestSettings:UseInMemoryDatabase", "true"},
                {"TestSettings:EnablePerformanceTests", "true"}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();
        }

        public static async Task<User> CreateTestUserAsync(ApplicationDbContext context, UserRole role = UserRole.Patient, string email = null)
        {
            var faker = new Faker();

            var user = new User
            {
                Email = email ?? faker.Internet.Email(),
                Password = BCrypt.Net.BCrypt.HashPassword("Password@123", 12),
                Name = faker.Person.FullName,
                Role = role,
                CrmNumber = role == UserRole.Doctor ? $"CRM/SP {faker.Random.Number(100000, 999999)}" : null,
                Phone = faker.Phone.PhoneNumber(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        public static async Task<List<User>> CreateTestUsersAsync(ApplicationDbContext context, int count, UserRole role = UserRole.Patient)
        {
            var users = new List<User>();

            for (int i = 0; i < count; i++)
            {
                var user = await CreateTestUserAsync(context, role);
                users.Add(user);
            }

            return users;
        }

        public static async Task<Specialty> CreateTestSpecialtyAsync(ApplicationDbContext context, string name = null)
        {
            var faker = new Faker();

            var specialty = new Specialty
            {
                Name = name ?? faker.Lorem.Word(),
                Description = faker.Lorem.Sentence(),
                Department = faker.PickRandom("Clínicas", "Cirúrgicas", "Diagnóstico"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Specialties.Add(specialty);
            await context.SaveChangesAsync();
            return specialty;
        }

        public static async Task<List<Specialty>> CreateTestSpecialtiesAsync(ApplicationDbContext context)
        {
            var specialties = new[]
            {
                new Specialty { Name = "Cardiologia", Description = "Doenças do coração", Department = "Clínicas", IsActive = true },
                new Specialty { Name = "Neurologia", Description = "Doenças do sistema nervoso", Department = "Clínicas", IsActive = true },
                new Specialty { Name = "Ortopedia", Description = "Doenças dos ossos", Department = "Cirúrgicas", IsActive = true },
                new Specialty { Name = "Dermatologia", Description = "Doenças da pele", Department = "Clínicas", IsActive = true },
                new Specialty { Name = "Clínica Geral", Description = "Medicina geral", Department = "Clínicas", IsActive = true }
            };

            foreach (var specialty in specialties)
            {
                specialty.CreatedAt = DateTime.UtcNow;
            }

            context.Specialties.AddRange(specialties);
            await context.SaveChangesAsync();
            return specialties.ToList();
        }

        public static async Task<Appointment> CreateTestAppointmentAsync(
            ApplicationDbContext context,
            int? patientId = null,
            int? doctorId = null,
            int? specialtyId = null,
            DateTime? appointmentDate = null)
        {
            var faker = new Faker();

            // Se não foram fornecidos IDs, criar usuários/especialidade
            if (!patientId.HasValue)
            {
                var patient = await CreateTestUserAsync(context, UserRole.Patient);
                patientId = patient.Id;
            }

            if (!specialtyId.HasValue)
            {
                var specialty = await CreateTestSpecialtyAsync(context);
                specialtyId = specialty.Id;
            }

            var appointment = new Appointment
            {
                PatientId = patientId.Value,
                DoctorId = doctorId,
                SpecialtyId = specialtyId,
                AppointmentDate = appointmentDate ?? DateTime.UtcNow.AddDays(1),
                Symptoms = faker.Lorem.Sentence(),
                RecommendedSpecialty = "Clínica Geral",
                TriageReasoning = faker.Lorem.Sentence(),
                Status = AppointmentStatus.Scheduled,
                Notes = "",
                CreatedAt = DateTime.UtcNow
            };

            context.Appointments.Add(appointment);
            await context.SaveChangesAsync();
            return appointment;
        }

        public static async Task<DoctorSpecialty> CreateDoctorSpecialtyAsync(
            ApplicationDbContext context,
            int doctorId,
            int specialtyId,
            bool isPrimary = true)
        {
            var faker = new Faker();

            var doctorSpecialty = new DoctorSpecialty
            {
                DoctorId = doctorId,
                SpecialtyId = specialtyId,
                IsPrimary = isPrimary,
                LicenseNumber = $"CRM/SP {faker.Random.Number(100000, 999999)}",
                CertificationDate = DateTime.UtcNow.AddYears(-faker.Random.Number(1, 10)),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.DoctorSpecialties.Add(doctorSpecialty);
            await context.SaveChangesAsync();
            return doctorSpecialty;
        }

        public static string GenerateValidPassword()
        {
            var faker = new Faker();
            return $"{faker.Lorem.Letter().ToUpper()}{faker.Lorem.Word().ToLower()}{faker.Random.Number(100, 999)}@";
        }

        public static string GenerateInvalidPassword()
        {
            return "123"; // Muito simples
        }

        public static DateTime GetValidAppointmentDate(int daysFromNow = 1, int hour = 10)
        {
            return DateTime.UtcNow.AddDays(daysFromNow).Date.AddHours(hour);
        }

        public static DateTime GetInvalidAppointmentDate()
        {
            return DateTime.UtcNow.AddDays(-1); // Data no passado
        }

        public static void AssertExecutionTime(Action action, int maxMilliseconds, string operationName = "Operation")
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            action();
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > maxMilliseconds)
            {
                throw new InvalidOperationException(
                    $"{operationName} took {stopwatch.ElapsedMilliseconds}ms, expected < {maxMilliseconds}ms");
            }
        }

        public static async Task AssertExecutionTimeAsync(Func<Task> action, int maxMilliseconds, string operationName = "Operation")
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await action();
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > maxMilliseconds)
            {
                throw new InvalidOperationException(
                    $"{operationName} took {stopwatch.ElapsedMilliseconds}ms, expected < {maxMilliseconds}ms");
            }
        }

        public static void CleanupDatabase(ApplicationDbContext context)
        {
            context.Appointments.RemoveRange(context.Appointments);
            context.DoctorSpecialties.RemoveRange(context.DoctorSpecialties);
            context.Users.RemoveRange(context.Users);
            context.Specialties.RemoveRange(context.Specialties);
            context.SaveChanges();
        }
    }
}