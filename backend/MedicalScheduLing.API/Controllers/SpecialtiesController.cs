using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedicalScheduling.API.Services;

namespace MedicalScheduling.API.Controllers
{
    [ApiController]
    [Route("specialties")]
    [Authorize]
    public class SpecialtiesController : ControllerBase
    {
        private readonly IDoctorAssignmentService _doctorAssignmentService;
        private readonly ILogger<SpecialtiesController> _logger;

        public SpecialtiesController(
            IDoctorAssignmentService doctorAssignmentService,
            ILogger<SpecialtiesController> logger)
        {
            _doctorAssignmentService = doctorAssignmentService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSpecialties()
        {
            try
            {
                var specialties = await _doctorAssignmentService.GetAllSpecialtiesAsync();
                return Ok(specialties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar especialidades");
                return BadRequest(new { message = "Erro ao buscar especialidades" });
            }
        }

        [HttpGet("with-doctors")]
        public async Task<IActionResult> GetSpecialtiesWithDoctors([FromQuery] DateTime? date = null)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;
                var specialties = await _doctorAssignmentService.GetAllSpecialtiesAsync();

                var result = new List<object>();

                foreach (var specialty in specialties)
                {
                    var doctors = await _doctorAssignmentService.GetAvailableDoctorsBySpecialtyAsync(
                        specialty.Name,
                        searchDate
                    );

                    result.Add(new
                    {
                        specialty.Id,
                        specialty.Name,
                        specialty.Description,
                        specialty.Department,
                        AvailableDoctors = doctors.Count,
                        Doctors = doctors
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar especialidades com médicos");
                return BadRequest(new { message = "Erro ao buscar especialidades com médicos" });
            }
        }

        [HttpGet("{specialtyName}/doctors")]
        public async Task<IActionResult> GetDoctorsBySpecialty(
            string specialtyName,
            [FromQuery] DateTime? date = null)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;
                var doctors = await _doctorAssignmentService.GetAvailableDoctorsBySpecialtyAsync(
                    specialtyName,
                    searchDate
                );

                return Ok(doctors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar médicos da especialidade {Specialty}", specialtyName);
                return BadRequest(new { message = "Erro ao buscar médicos da especialidade" });
            }
        }
    }
}