using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MedicalScheduling.API.Data;
using MedicalScheduling.API.Services;
using MedicalScheduling.API.Models;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]))
        };
    });

// Business Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<ITriageService, TriageService>();
builder.Services.AddScoped<IDoctorAssignmentService, DoctorAssignmentService>();
builder.Services.AddScoped<IAppointmentValidationService, AppointmentValidationService>();

// AI Services
builder.Services.AddHttpClient<IAIService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IAIService>(serviceProvider =>
{
    var httpClient = serviceProvider.GetRequiredService<HttpClient>();
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var logger = serviceProvider.GetRequiredService<ILogger<OpenAIService>>();

    var openAIKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    if (!string.IsNullOrEmpty(openAIKey))
    {
        return new OpenAIService(httpClient, configuration, logger);
    }
    else
    {
        return new MockAIService(httpClient, serviceProvider.GetRequiredService<ILogger<MockAIService>>());
    }
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// Logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

static string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    return Convert.ToBase64String(hashedBytes);
}

// Database initialization and seeding
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Criando banco de dados...");
        dbContext.Database.EnsureCreated();
        logger.LogInformation("Banco de dados criado com sucesso!");

        // Seed Specialties
        if (!dbContext.Specialties.Any())
        {
            logger.LogInformation("Criando especialidades médicas...");

            var specialties = new[]
            {
                new Specialty { Name = "Cardiologia", Description = "Doenças do coração e sistema cardiovascular", Department = "Clínicas" },
                new Specialty { Name = "Neurologia", Description = "Doenças do sistema nervoso", Department = "Clínicas" },
                new Specialty { Name = "Pneumologia", Description = "Doenças do sistema respiratório", Department = "Clínicas" },
                new Specialty { Name = "Gastroenterologia", Description = "Doenças do sistema digestivo", Department = "Clínicas" },
                new Specialty { Name = "Ortopedia", Description = "Doenças dos ossos, músculos e articulações", Department = "Cirúrgicas" },
                new Specialty { Name = "Dermatologia", Description = "Doenças da pele, cabelo e unhas", Department = "Clínicas" },
                new Specialty { Name = "Psiquiatria", Description = "Transtornos mentais e comportamentais", Department = "Clínicas" },
                new Specialty { Name = "Otorrinolaringologia", Description = "Doenças do ouvido, nariz e garganta", Department = "Cirúrgicas" },
                new Specialty { Name = "Oftalmologia", Description = "Doenças dos olhos e sistema visual", Department = "Cirúrgicas" },
                new Specialty { Name = "Endocrinologia", Description = "Doenças hormonais e metabólicas", Department = "Clínicas" },
                new Specialty { Name = "Clínica Geral", Description = "Medicina geral e cuidados primários", Department = "Clínicas" },
                new Specialty { Name = "Pediatria", Description = "Cuidados médicos para crianças e adolescentes", Department = "Clínicas" },
                new Specialty { Name = "Ginecologia", Description = "Saúde da mulher e sistema reprodutivo feminino", Department = "Clínicas" },
                new Specialty { Name = "Urologia", Description = "Doenças do sistema urinário e reprodutor masculino", Department = "Cirúrgicas" },
                new Specialty { Name = "Oncologia", Description = "Diagnóstico e tratamento do câncer", Department = "Clínicas" },
                new Specialty { Name = "Reumatologia", Description = "Doenças articulares e do tecido conjuntivo", Department = "Clínicas" },
                new Specialty { Name = "Infectologia", Description = "Doenças infecciosas e parasitárias", Department = "Clínicas" },
                new Specialty { Name = "Nefrologia", Description = "Doenças dos rins", Department = "Clínicas" },
                new Specialty { Name = "Hematologia", Description = "Doenças do sangue", Department = "Clínicas" },
                new Specialty { Name = "Geriatria", Description = "Cuidados médicos para idosos", Department = "Clínicas" }
            };

            dbContext.Specialties.AddRange(specialties);
            dbContext.SaveChanges();
            logger.LogInformation("Especialidades criadas com sucesso!");
        }

        // Seed Users
        if (!dbContext.Users.Any())
        {
            logger.LogInformation("Criando usuários padrão...");

            string defaultPassword = HashPassword("Senha@123");

            var users = new[]
            {
                // Médicos
                new User
                {
                    Email = "medico@example.com",
                    Password = defaultPassword,
                    Name = "Dr. João Silva",
                    Role = UserRole.Doctor,
                    CrmNumber = "CRM/SP 123456",
                    Phone = "(11) 99999-1111",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Email = "medico2@example.com",
                    Password = defaultPassword,
                    Name = "Dra. Maria Santos",
                    Role = UserRole.Doctor,
                    CrmNumber = "CRM/SP 654321",
                    Phone = "(11) 99999-2222",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Email = "medico3@example.com",
                    Password = defaultPassword,
                    Name = "Dr. Carlos Oliveira",
                    Role = UserRole.Doctor,
                    CrmNumber = "CRM/SP 789012",
                    Phone = "(11) 99999-3333",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Email = "medico4@example.com",
                    Password = defaultPassword,
                    Name = "Dra. Ana Costa",
                    Role = UserRole.Doctor,
                    CrmNumber = "CRM/SP 345678",
                    Phone = "(11) 99999-4444",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Email = "medico5@example.com",
                    Password = defaultPassword,
                    Name = "Dr. Pedro Lima",
                    Role = UserRole.Doctor,
                    CrmNumber = "CRM/SP 567890",
                    Phone = "(11) 99999-5555",
                    CreatedAt = DateTime.UtcNow
                },
                // Pacientes
                new User
                {
                    Email = "paciente@example.com",
                    Password = defaultPassword,
                    Name = "Ana Silva",
                    Role = UserRole.Patient,
                    Phone = "(11) 88888-1111",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Email = "paciente2@example.com",
                    Password = defaultPassword,
                    Name = "Pedro Costa",
                    Role = UserRole.Patient,
                    Phone = "(11) 88888-2222",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Email = "paciente3@example.com",
                    Password = defaultPassword,
                    Name = "Luiza Ferreira",
                    Role = UserRole.Patient,
                    Phone = "(11) 88888-3333",
                    CreatedAt = DateTime.UtcNow
                }
            };

            dbContext.Users.AddRange(users);
            dbContext.SaveChanges();
            logger.LogInformation("Usuários criados com sucesso!");
        }

        // Seed Doctor Specialties
        if (!dbContext.DoctorSpecialties.Any())
        {
            logger.LogInformation("Atribuindo especialidades aos médicos...");

            var doctors = dbContext.Users.Where(u => u.Role == UserRole.Doctor).ToList();
            var specialties = dbContext.Specialties.ToList();

            var doctorSpecialties = new List<DoctorSpecialty>();

            // Dr. João Silva - Cardiologia e Clínica Geral
            var drJoao = doctors.First(d => d.Name == "Dr. João Silva");
            var cardiologia = specialties.First(s => s.Name == "Cardiologia");
            var clinicaGeral = specialties.First(s => s.Name == "Clínica Geral");

            doctorSpecialties.AddRange(new[]
            {
                new DoctorSpecialty
                {
                    DoctorId = drJoao.Id,
                    SpecialtyId = cardiologia.Id,
                    IsPrimary = true,
                    LicenseNumber = "CRM/SP 123456-CARDIO",
                    CertificationDate = DateTime.UtcNow.AddYears(-5),
                    CreatedAt = DateTime.UtcNow
                },
                new DoctorSpecialty
                {
                    DoctorId = drJoao.Id,
                    SpecialtyId = clinicaGeral.Id,
                    IsPrimary = false,
                    LicenseNumber = "CRM/SP 123456-CG",
                    CertificationDate = DateTime.UtcNow.AddYears(-8),
                    CreatedAt = DateTime.UtcNow
                }
            });

            // Dra. Maria Santos - Neurologia e Psiquiatria
            var draMaria = doctors.First(d => d.Name == "Dra. Maria Santos");
            var neurologia = specialties.First(s => s.Name == "Neurologia");
            var psiquiatria = specialties.First(s => s.Name == "Psiquiatria");

            doctorSpecialties.AddRange(new[]
            {
                new DoctorSpecialty
                {
                    DoctorId = draMaria.Id,
                    SpecialtyId = neurologia.Id,
                    IsPrimary = true,
                    LicenseNumber = "CRM/SP 654321-NEURO",
                    CertificationDate = DateTime.UtcNow.AddYears(-3),
                    CreatedAt = DateTime.UtcNow
                },
                new DoctorSpecialty
                {
                    DoctorId = draMaria.Id,
                    SpecialtyId = psiquiatria.Id,
                    IsPrimary = false,
                    LicenseNumber = "CRM/SP 654321-PSI",
                    CertificationDate = DateTime.UtcNow.AddYears(-2),
                    CreatedAt = DateTime.UtcNow
                }
            });

            // Dr. Carlos Oliveira - Ortopedia e Reumatologia
            var drCarlos = doctors.First(d => d.Name == "Dr. Carlos Oliveira");
            var ortopedia = specialties.First(s => s.Name == "Ortopedia");
            var reumatologia = specialties.First(s => s.Name == "Reumatologia");

            doctorSpecialties.AddRange(new[]
            {
                new DoctorSpecialty
                {
                    DoctorId = drCarlos.Id,
                    SpecialtyId = ortopedia.Id,
                    IsPrimary = true,
                    LicenseNumber = "CRM/SP 789012-ORTO",
                    CertificationDate = DateTime.UtcNow.AddYears(-6),
                    CreatedAt = DateTime.UtcNow
                },
                new DoctorSpecialty
                {
                    DoctorId = drCarlos.Id,
                    SpecialtyId = reumatologia.Id,
                    IsPrimary = false,
                    LicenseNumber = "CRM/SP 789012-REUMA",
                    CertificationDate = DateTime.UtcNow.AddYears(-4),
                    CreatedAt = DateTime.UtcNow
                }
            });

            // Dra. Ana Costa - Ginecologia e Pediatria
            var draAna = doctors.First(d => d.Name == "Dra. Ana Costa");
            var ginecologia = specialties.First(s => s.Name == "Ginecologia");
            var pediatria = specialties.First(s => s.Name == "Pediatria");

            doctorSpecialties.AddRange(new[]
            {
                new DoctorSpecialty
                {
                    DoctorId = draAna.Id,
                    SpecialtyId = ginecologia.Id,
                    IsPrimary = true,
                    LicenseNumber = "CRM/SP 345678-GINECO",
                    CertificationDate = DateTime.UtcNow.AddYears(-4),
                    CreatedAt = DateTime.UtcNow
                },
                new DoctorSpecialty
                {
                    DoctorId = draAna.Id,
                    SpecialtyId = pediatria.Id,
                    IsPrimary = false,
                    LicenseNumber = "CRM/SP 345678-PED",
                    CertificationDate = DateTime.UtcNow.AddYears(-7),
                    CreatedAt = DateTime.UtcNow
                }
            });

            // Dr. Pedro Lima - Gastroenterologia, Endocrinologia e Clínica Geral
            var drPedro = doctors.First(d => d.Name == "Dr. Pedro Lima");
            var gastroenterologia = specialties.First(s => s.Name == "Gastroenterologia");
            var endocrinologia = specialties.First(s => s.Name == "Endocrinologia");

            doctorSpecialties.AddRange(new[]
            {
                new DoctorSpecialty
                {
                    DoctorId = drPedro.Id,
                    SpecialtyId = gastroenterologia.Id,
                    IsPrimary = true,
                    LicenseNumber = "CRM/SP 567890-GASTRO",
                    CertificationDate = DateTime.UtcNow.AddYears(-5),
                    CreatedAt = DateTime.UtcNow
                },
                new DoctorSpecialty
                {
                    DoctorId = drPedro.Id,
                    SpecialtyId = endocrinologia.Id,
                    IsPrimary = false,
                    LicenseNumber = "CRM/SP 567890-ENDO",
                    CertificationDate = DateTime.UtcNow.AddYears(-3),
                    CreatedAt = DateTime.UtcNow
                },
                new DoctorSpecialty
                {
                    DoctorId = drPedro.Id,
                    SpecialtyId = clinicaGeral.Id,
                    IsPrimary = false,
                    LicenseNumber = "CRM/SP 567890-CG",
                    CertificationDate = DateTime.UtcNow.AddYears(-10),
                    CreatedAt = DateTime.UtcNow
                }
            });

            dbContext.DoctorSpecialties.AddRange(doctorSpecialties);
            dbContext.SaveChanges();
            logger.LogInformation("Especialidades atribuídas com sucesso!");
        }

        logger.LogInformation("=== SISTEMA INICIALIZADO COM SUCESSO ===");
        logger.LogInformation("CREDENCIAIS DE ACESSO:");
        logger.LogInformation("");
        logger.LogInformation("MÉDICOS:");
        logger.LogInformation("   medico@example.com / Senha@123 (Dr. João Silva - Cardiologia, Clínica Geral)");
        logger.LogInformation("   medico2@example.com / Senha@123 (Dra. Maria Santos - Neurologia, Psiquiatria)");
        logger.LogInformation("   medico3@example.com / Senha@123 (Dr. Carlos Oliveira - Ortopedia, Reumatologia)");
        logger.LogInformation("   medico4@example.com / Senha@123 (Dra. Ana Costa - Ginecologia, Pediatria)");
        logger.LogInformation("   medico5@example.com / Senha@123 (Dr. Pedro Lima - Gastro, Endocrinologia, Clínica Geral)");
        logger.LogInformation("");
        logger.LogInformation("PACIENTES:");
        logger.LogInformation("  👤 paciente@example.com / Senha@123 (Ana Silva)");
        logger.LogInformation("  👤 paciente2@example.com / Senha@123 (Pedro Costa)");
        logger.LogInformation("  👤 paciente3@example.com / Senha@123 (Luiza Ferreira)");
        logger.LogInformation("");
        logger.LogInformation("ESPECIALIDADES DISPONÍVEIS:");
        var allSpecialties = dbContext.Specialties.ToList();
        foreach (var specialty in allSpecialties)
        {
            var doctorCount = dbContext.DoctorSpecialties.Count(ds => ds.SpecialtyId == specialty.Id && ds.IsActive);
            logger.LogInformation($"  🏥 {specialty.Name} ({specialty.Department}) - {doctorCount} médico(s)");
        }
        logger.LogInformation("=========================================");

    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro durante inicialização do banco: {Message}", ex.Message);
    }
}

app.Run();