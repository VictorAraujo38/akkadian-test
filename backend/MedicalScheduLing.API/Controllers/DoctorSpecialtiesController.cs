using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalScheduling.API.Data;
using MedicalScheduling.API.Models;
using MedicalScheduling.API.DTOs;
using System.Security.Claims;

namespace MedicalScheduling.API.Controllers
{
    [ApiController]
    [Route("doctor-specialties")]
    [Authorize(Roles = "Doctor")]
    public class DoctorSpecialtiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DoctorSpecialtiesController> _logger;

        public DoctorSpecialtiesController(ApplicationDbContext context, ILogger<DoctorSpecialtiesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> AddDoctorSpecialty([FromBody] CreateDoctorSpecialtyDto dto)
        {
            try
            {
                var doctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                // Verificar se a especialidade existe
                var specialty = await _context.Specialties.FindAsync(dto.SpecialtyId);
                if (specialty == null)
                {
                    return BadRequest(new { message = "Especialidade não encontrada" });
                }

                // Verificar se já existe essa combinação
                var existingRelation = await _context.DoctorSpecialties
                    .AnyAsync(ds => ds.DoctorId == doctorId && ds.SpecialtyId == dto.SpecialtyId);

                if (existingRelation)
                {
                    return BadRequest(new { message = "Médico já possui essa especialidade" });
                }

                // Se é especialidade primária, remover outras primárias
                if (dto.IsPrimary)
                {
                    var existingPrimary = await _context.DoctorSpecialties
                        .Where(ds => ds.DoctorId == doctorId && ds.IsPrimary)
                        .ToListAsync();

                    foreach (var primary in existingPrimary)
                    {
                        primary.IsPrimary = false;
                    }
                }

                var doctorSpecialty = new DoctorSpecialty
                {
                    DoctorId = doctorId,
                    SpecialtyId = dto.SpecialtyId,
                    IsPrimary = dto.IsPrimary,
                    LicenseNumber = dto.LicenseNumber ?? "",
                    CertificationDate = dto.CertificationDate,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.DoctorSpecialties.Add(doctorSpecialty);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Especialidade adicionada com sucesso", id = doctorSpecialty.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar especialidade do médico");
                return BadRequest(new { message = "Erro ao adicionar especialidade" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDoctorSpecialties()
        {
            try
            {
                var doctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var specialties = await _context.DoctorSpecialties
                    .Include(ds => ds.Specialty)
                    .Where(ds => ds.DoctorId == doctorId && ds.IsActive)
                    .Select(ds => new
                    {
                        Id = ds.Id,
                        SpecialtyId = ds.SpecialtyId,
                        SpecialtyName = ds.Specialty.Name,
                        SpecialtyDepartment = ds.Specialty.Department,
                        IsPrimary = ds.IsPrimary,
                        LicenseNumber = ds.LicenseNumber,
                        CertificationDate = ds.CertificationDate
                    })
                    .ToListAsync();

                return Ok(specialties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar especialidades do médico");
                return BadRequest(new { message = "Erro ao buscar especialidades" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveDoctorSpecialty(int id)
        {
            try
            {
                var doctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var doctorSpecialty = await _context.DoctorSpecialties
                    .FirstOrDefaultAsync(ds => ds.Id == id && ds.DoctorId == doctorId);

                if (doctorSpecialty == null)
                {
                    return NotFound(new { message = "Especialidade não encontrada" });
                }

                _context.DoctorSpecialties.Remove(doctorSpecialty);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Especialidade removida com sucesso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover especialidade do médico");
                return BadRequest(new { message = "Erro ao remover especialidade" });
            }
        }
    }
}