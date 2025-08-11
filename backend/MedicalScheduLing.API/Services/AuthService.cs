using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MedicalScheduling.API.Data;
using MedicalScheduling.API.DTOs;
using MedicalScheduling.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace MedicalScheduling.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<LoginResponseDto> RegisterAsync(RegisterDto dto)
        {
            try
            {
                _logger.LogInformation("Tentativa de registro para email: {Email}", dto.Email);

                // Check if user exists
                if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                {
                    _logger.LogWarning("Tentativa de registro com email já existente: {Email}", dto.Email);
                    throw new InvalidOperationException("Email já está em uso");
                }

                // Validate password strength
                if (!IsPasswordValid(dto.Password))
                {
                    throw new ArgumentException("Senha deve ter pelo menos 8 caracteres, incluindo maiúscula, minúscula, número e caractere especial");
                }

                // Hash password with bcrypt
                var hashedPassword = HashPassword(dto.Password);

                // Create user
                var user = new User
                {
                    Email = dto.Email.ToLowerInvariant().Trim(),
                    Password = hashedPassword,
                    Name = dto.Name.Trim(),
                    Role = dto.Role.ToLower() == "doctor" ? UserRole.Doctor : UserRole.Patient,
                    CrmNumber = dto.Role.ToLower() == "doctor" ? dto.CrmNumber?.Trim() : null,
                    Phone = dto.Phone?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Usuário registrado com sucesso: {Email}, ID: {UserId}", dto.Email, user.Id);

                // Generate token
                var token = GenerateJwtToken(user);

                return new LoginResponseDto
                {
                    Token = token,
                    Email = user.Email,
                    Name = user.Name,
                    Role = user.Role.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante registro do usuário: {Email}", dto.Email);
                throw;
            }
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
        {
            try
            {
                _logger.LogInformation("Tentativa de login para email: {Email}", dto.Email);

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLowerInvariant().Trim());

                if (user == null)
                {
                    _logger.LogWarning("Tentativa de login com email não encontrado: {Email}", dto.Email);
                    // Delay para dificultar ataques de força bruta
                    await Task.Delay(1000);
                    throw new UnauthorizedAccessException("Email ou senha inválidos");
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Tentativa de login com usuário inativo: {Email}", dto.Email);
                    throw new UnauthorizedAccessException("Conta desativada");
                }

                if (!VerifyPassword(dto.Password, user.Password))
                {
                    _logger.LogWarning("Tentativa de login com senha incorreta: {Email}", dto.Email);
                    // Delay para dificultar ataques de força bruta
                    await Task.Delay(1000);
                    throw new UnauthorizedAccessException("Email ou senha inválidos");
                }

                _logger.LogInformation("Login realizado com sucesso: {Email}, ID: {UserId}", dto.Email, user.Id);

                var token = GenerateJwtToken(user);

                return new LoginResponseDto
                {
                    Token = token,
                    Email = user.Email,
                    Name = user.Name,
                    Role = user.Role.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante login do usuário: {Email}", dto.Email);
                throw;
            }
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, 12);
        }

        private bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar senha");
                return false;
            }
        }

        private bool IsPasswordValid(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            var hasUpper = password.Any(char.IsUpper);
            var hasLower = password.Any(char.IsLower);
            var hasDigit = password.Any(char.IsDigit);
            var hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];

            if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
            {
                throw new InvalidOperationException("JWT SecretKey deve ter pelo menos 32 caracteres");
            }

            var key = Encoding.UTF8.GetBytes(secretKey);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim("isActive", user.IsActive.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            _logger.LogInformation("JWT Token gerado para usuário: {UserId}", user.Id);

            return tokenHandler.WriteToken(token);
        }
    }
}