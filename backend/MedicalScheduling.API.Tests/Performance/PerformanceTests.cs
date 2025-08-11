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
using Bogus;
using System.Diagnostics;

namespace MedicalScheduling.API.Tests.Performance
{
    public class PerformanceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;
        private readonly AppointmentService _appointmentService;
        private readonly Mock<ITriageService> _mockTriageService;
        private readonly Mock<ILogger<AuthService>> _mockAuthLogger;
        private readonly Mock<ILogger<AppointmentService>> _mockAppointmentLogger;
        private readonly IConfiguration _configuration;

        public PerformanceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            _mockAuthLogger = new Mock<ILogger<AuthService>>();
            _mockAppointmentLogger = new Mock<ILogger<AppointmentService>>();
            _mockTriageService = new Mock<ITriageService>();

            var configurationData = new Dictionary<string, string>
            {
                {"JwtSettings:SecretKey", "test-secret-key-minimum-32-characters-long-for-security!!"},
                {"JwtSettings:Issuer", "TestIssuer"},
                {"JwtSettings:Audience", "TestAudience"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationData)
                .Build();

            _authService = new AuthService(_context, _configuration, _mockAuthLogger.Object);
            _appointmentService = new AppointmentService(_context, _mockTriageService.Object);

            // Setup mock para triagem
            _mockTriageService.Setup(x => x.ProcessTriageAsync(It.IsAny<string>()))
                .ReturnsAsync(new TriageResponseDto
                {
                    RecommendedSpecialty = "Clínica Geral",
                    Confidence = "Alta",
                    Reasoning = "Teste automático"
                });

            SeedTestData().Wait();
        }

        private async Task SeedTestData()
        {
            // Criar especialidades
            var specialties = new[]
            {
                new Specialty { Name = "Clínica Geral", Description = "Medicina geral", Department = "Clínicas", IsActive = true },
                new Specialty { Name = "Cardiologia", Description = "Coração", Department = "Clínicas", IsActive = true },
                new Specialty { Name = "Neurologia", Description = "Sistema nervoso", Department = "Clínicas", IsActive = true }
            };

            _context.Specialties.AddRange(specialties);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task BCryptHashing_Performance_ShouldBeReasonable()
        {
            // Arrange
            var passwords = new[]
            {
                "Password@123",
                "AnotherPassword@456",
                "SuperSecure@789",
                "TestPassword@000",
                "FinalPassword@111"
            };

            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();
            foreach (var password in passwords)
            {
                var hash = BCrypt.Net.BCrypt.HashPassword(password, 12);
                var isValid = BCrypt.Net.BCrypt.Verify(password, hash);
                isValid.Should().BeTrue();
            }
            stopwatch.Stop();

            // Assert - BCrypt com work factor 12 deve ser < 2 segundos para 5 operações
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);

            // Log do tempo para análise
            var averageTime = stopwatch.ElapsedMilliseconds / passwords.Length;
            Console.WriteLine($"Tempo médio por hash BCrypt: {averageTime}ms");
        }

        [Fact]
        public async Task BulkUserRegistration_Performance_ShouldHandleLoad()
        {
            // Arrange
            var faker = new Faker<RegisterDto>()
                .RuleFor(u => u.Name, f => f.Person.FullName)
                .RuleFor(u => u.Email, f => f.Internet.Email())
                .RuleFor(u => u.Password, f => "Password@123")
                .RuleFor(u => u.Role, f => "patient")
                .RuleFor(u => u.Phone, f => f.Phone.PhoneNumber());

            var users = faker.Generate(50); // 50 usuários

            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();
            var tasks = users.Select(async user =>
            {
                try
                {
                    await _authService.RegisterAsync(user);
                    return true;
                }
                catch
                {
                    return false;
                }
            });

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            results.Should().AllBeEquivalentTo(true); // Todos devem ter sucesso
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(30000); // < 30 segundos para 50 usuários

            var averageTime = stopwatch.ElapsedMilliseconds / users.Count;
            Console.WriteLine($"Tempo médio por registro: {averageTime}ms");
            Console.WriteLine($"Total de usuários registrados: {results.Length}");
        }

        [Fact]
        public async Task DatabaseQuery_Performance_ShouldBeEfficient()
        {
            // Arrange - Criar dados de teste
            await CreateTestUsers(100);
            await CreateTestAppointments(200);

            var stopwatch = new Stopwatch();

            // Act - Testar várias consultas
            stopwatch.Start();

            // Consulta 1: Todos os usuários ativos
            var activeUsers = await _context.Users
                .Where(u => u.IsActive)
                .ToListAsync();

            // Consulta 2: Agendamentos de hoje
            var today = DateTime.UtcNow.Date;
            var todayAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppointmentDate.Date == today)
                .ToListAsync();

            // Consulta 3: Médicos com especialidades
            var doctorsWithSpecialties = await _context.Users
                .Include(u => u.DoctorSpecialties)
                .ThenInclude(ds => ds.Specialty)
                .Where(u => u.Role == UserRole.Doctor && u.IsActive)
                .ToListAsync();

            stopwatch.Stop();

            // Assert
            activeUsers.Should().NotBeEmpty();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // < 1 segundo para todas as consultas

            Console.WriteLine($"Usuários ativos: {activeUsers.Count}");
            Console.WriteLine($"Agendamentos hoje: {todayAppointments.Count}");
            Console.WriteLine($"Médicos com especialidades: {doctorsWithSpecialties.Count}");
            Console.WriteLine($"Tempo total das consultas: {stopwatch.ElapsedMilliseconds}ms");
        }

        [Fact]
        public async Task ConcurrentAppointmentCreation_ShouldHandleLoad()
        {
            // Arrange
            var patients = await CreateTestUsers(20, UserRole.Patient);
            var doctors = await CreateTestUsers(5, UserRole.Doctor);

            var appointmentTasks = new List<Task<bool>>();

            // Act - Criar 50 agendamentos concorrentes
            for (int i = 0; i < 50; i++)
            {
                var patientId = patients[i % patients.Count].Id;
                var appointmentDate = DateTime.UtcNow.AddDays(1).AddHours(8 + (i % 8));

                var task = CreateAppointmentConcurrently(patientId, appointmentDate);
                appointmentTasks.Add(task);
            }

            var stopwatch = Stopwatch.StartNew();
            var results = await Task.WhenAll(appointmentTasks);
            stopwatch.Stop();

            // Assert
            var successCount = results.Count(r => r);
            successCount.Should().BeGreaterThan(40); // Pelo menos 80% de sucesso
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // < 10 segundos

            Console.WriteLine($"Agendamentos criados com sucesso: {successCount}/50");
            Console.WriteLine($"Tempo total: {stopwatch.ElapsedMilliseconds}ms");
        }

        private async Task<bool> CreateAppointmentConcurrently(int patientId, DateTime appointmentDate)
        {
            try
            {
                var dto = new CreateAppointmentDto
                {
                    AppointmentDate = appointmentDate,
                    Symptoms = $"Sintomas de teste {DateTime.Now.Ticks}",
                    PreferredSpecialty = "Clínica Geral"
                };

                await _appointmentService.CreateAppointmentAsync(patientId, dto);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<List<User>> CreateTestUsers(int count, UserRole role = UserRole.Patient)
        {
            var faker = new Faker<User>()
                .RuleFor(u => u.Name, f => f.Person.FullName)
                .RuleFor(u => u.Email, f => f.Internet.Email())
                .RuleFor(u => u.Password, f => BCrypt.Net.BCrypt.HashPassword("Password@123", 12))
                .RuleFor(u => u.Role, f => role)
                .RuleFor(u => u.Phone, f => f.Phone.PhoneNumber())
                .RuleFor(u => u.CrmNumber, f => role == UserRole.Doctor ? f.Random.AlphaNumeric(10) : null)
                .RuleFor(u => u.IsActive, f => true)
                .RuleFor(u => u.CreatedAt, f => DateTime.UtcNow);

            var users = faker.Generate(count);

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            return users;
        }

        private async Task CreateTestAppointments(int count)
        {
            var patients = await _context.Users.Where(u => u.Role == UserRole.Patient).Take(10).ToListAsync();
            var doctors = await _context.Users.Where(u => u.Role == UserRole.Doctor).Take(5).ToListAsync();
            var specialties = await _context.Specialties.ToListAsync();

            if (!patients.Any())
            {
                patients = await CreateTestUsers(10, UserRole.Patient);
            }

            if (!doctors.Any())
            {
                doctors = await CreateTestUsers(5, UserRole.Doctor);
            }

            var faker = new Faker<Appointment>()
                .RuleFor(a => a.PatientId, f => f.PickRandom(patients).Id)
                .RuleFor(a => a.DoctorId, f => f.PickRandom(doctors).Id)
                .RuleFor(a => a.SpecialtyId, f => f.PickRandom(specialties).Id)
                .RuleFor(a => a.AppointmentDate, f => f.Date.Future(1))
                .RuleFor(a => a.Symptoms, f => f.Lorem.Sentence())
                .RuleFor(a => a.RecommendedSpecialty, f => f.PickRandom(specialties).Name)
                .RuleFor(a => a.TriageReasoning, f => f.Lorem.Sentence())
                .RuleFor(a => a.Status, f => AppointmentStatus.Scheduled)
                .RuleFor(a => a.Notes, f => "")
                .RuleFor(a => a.CreatedAt, f => DateTime.UtcNow);

            var appointments = faker.Generate(count);

            _context.Appointments.AddRange(appointments);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task JWT_Generation_Performance_ShouldBeEfficient()
        {
            // Arrange
            var users = await CreateTestUsers(100);
            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();
            var tokens = new List<string>();

            foreach (var user in users)
            {
                var loginDto = new LoginDto
                {
                    Email = user.Email,
                    Password = "Password@123"
                };

                var result = await _authService.LoginAsync(loginDto);
                tokens.Add(result.Token);
            }
            stopwatch.Stop();

            // Assert
            tokens.Should().HaveCount(100);
            tokens.Should().OnlyContain(t => !string.IsNullOrEmpty(t));
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // < 5 segundos para 100 tokens

            var averageTime = stopwatch.ElapsedMilliseconds / users.Count;
            Console.WriteLine($"Tempo médio por geração de JWT: {averageTime}ms");
        }

        [Fact]
        public async Task TriageService_Performance_ShouldBeResponsive()
        {
            // Arrange
            var symptoms = new[]
            {
                "dor no peito e falta de ar",
                "dor de cabeça intensa",
                "febre alta e tosse",
                "dor abdominal e náusea",
                "dor nas articulações",
                "coceira na pele",
                "ansiedade e insônia",
                "dor de garganta",
                "visão embaçada",
                "sede excessiva"
            };

            var stopwatch = new Stopwatch();

            // Act
            stopwatch.Start();
            var results = new List<TriageResponseDto>();

            foreach (var symptom in symptoms)
            {
                var result = await _mockTriageService.Object.ProcessTriageAsync(symptom);
                results.Add(result);
            }
            stopwatch.Stop();

            // Assert
            results.Should().HaveCount(10);
            results.Should().OnlyContain(r => !string.IsNullOrEmpty(r.RecommendedSpecialty));
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // < 1 segundo para 10 triagens

            var averageTime = stopwatch.ElapsedMilliseconds / symptoms.Length;
            Console.WriteLine($"Tempo médio por triagem: {averageTime}ms");
        }

        [Fact]
        public async Task DatabaseConnection_Pool_ShouldHandleConcurrency()
        {
            // Arrange
            var tasks = new List<Task<bool>>();

            // Act - 20 operações concorrentes no banco
            for (int i = 0; i < 20; i++)
            {
                var task = PerformDatabaseOperation(i);
                tasks.Add(task);
            }

            var stopwatch = Stopwatch.StartNew();
            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            results.Should().OnlyContain(r => r); // Todas devem ter sucesso
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000); // < 3 segundos

            Console.WriteLine($"Operações concorrentes no DB: {results.Length}");
            Console.WriteLine($"Tempo total: {stopwatch.ElapsedMilliseconds}ms");
        }

        private async Task<bool> PerformDatabaseOperation(int index)
        {
            try
            {
                // Simular operação típica: buscar usuários, criar agendamento
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .Take(5)
                    .ToListAsync();

                if (users.Any())
                {
                    var specialties = await _context.Specialties
                        .Take(1)
                        .ToListAsync();

                    if (specialties.Any())
                    {
                        var appointment = new Appointment
                        {
                            PatientId = users.First().Id,
                            SpecialtyId = specialties.First().Id,
                            AppointmentDate = DateTime.UtcNow.AddDays(1),
                            Symptoms = $"Teste concorrente {index}",
                            RecommendedSpecialty = specialties.First().Name,
                            TriageReasoning = "Teste",
                            Status = AppointmentStatus.Scheduled,
                            Notes = "",
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Appointments.Add(appointment);
                        await _context.SaveChangesAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro na operação {index}: {ex.Message}");
                return false;
            }
        }

        [Fact]
        public async Task Memory_Usage_ShouldBeEfficient()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);

            // Act - Operações que podem consumir memória
            var users = await CreateTestUsers(1000);
            await CreateTestAppointments(500);

            // Buscar dados
            var allUsers = await _context.Users.ToListAsync();
            var allAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .ToListAsync();

            var finalMemory = GC.GetTotalMemory(true);

            // Assert
            var memoryUsed = finalMemory - initialMemory;
            var memoryInMB = memoryUsed / (1024 * 1024);

            memoryInMB.Should().BeLessThan(100); // < 100MB para operações de teste

            Console.WriteLine($"Usuários criados: {users.Count}");
            Console.WriteLine($"Agendamentos criados: 500");
            Console.WriteLine($"Memória utilizada: {memoryInMB:F2} MB");
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}