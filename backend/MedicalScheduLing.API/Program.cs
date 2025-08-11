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

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<ITriageService, TriageService>();
builder.Services.AddHttpClient<IAIService, MockAIService>();

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

// Run migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        // Criar banco e tabelas automaticamente
        Console.WriteLine("Criando banco de dados...");
        dbContext.Database.EnsureCreated();
        Console.WriteLine("Banco de dados criado com sucesso!");

        // Criar usuários padrão se não existirem
        if (!dbContext.Users.Any())
        {
            Console.WriteLine("Criando usuários padrão...");

            string defaultPassword = HashPassword("Senha@123");

            var users = new[]
            {
                new User
                {
                    Email = "medico@example.com",
                    Password = defaultPassword,
                    Name = "Dr. João Silva",
                    Role = UserRole.Doctor,
                    CreatedAt = DateTime.UtcNow,
                    Appointments = new List<Appointment>()
                },
                new User
                {
                    Email = "paciente@example.com",
                    Password = defaultPassword,
                    Name = "Maria Santos",
                    Role = UserRole.Patient,
                    CreatedAt = DateTime.UtcNow,
                    Appointments = new List<Appointment>()
                }
            };

            dbContext.Users.AddRange(users);
            dbContext.SaveChanges();
            Console.WriteLine("Usuários padrão criados com sucesso!");
            Console.WriteLine($"Login: medico@example.com / Senha@123");
            Console.WriteLine($"Login: paciente@example.com / Senha@123");
        }
        else
        {
            Console.WriteLine("Usuários já existem no banco.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro durante inicialização do banco: {ex.Message}");
    }
}


app.Run();