using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MedicalScheduling.API.Data;
using MedicalScheduling.API.Services;
using MedicalScheduling.API.Models;

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

// Run migrations
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        // Aplicar migrations automaticamente
        dbContext.Database.Migrate();

        // Criar usuários padrão se não existirem
        if (!dbContext.Users.Any())
        {
            var users = new[]
            {
                new User
                {
                    Email = "medico@example.com",
                    Password = "jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=", // Senha@123
                    Name = "Dr. João Silva",
                    Role = UserRole.Doctor,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Email = "paciente@example.com",
                    Password = "jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=", // Senha@123
                    Name = "Maria Santos",
                    Role = UserRole.Patient,
                    CreatedAt = DateTime.UtcNow
                }
            };

            dbContext.Users.AddRange(users);
            dbContext.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        // Log do erro mas não quebra a aplicação
        Console.WriteLine($"Migration error: {ex.Message}");
    }
}


app.Run();