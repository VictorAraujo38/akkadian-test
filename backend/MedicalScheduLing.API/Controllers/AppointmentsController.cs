using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedicalScheduling.API.Services;
using MedicalScheduling.API.DTOs;
using MedicalScheduling.API.Data;
using MedicalScheduling.API.Models;
using System.Security.Claims;

namespace MedicalScheduling.API.Controllers
{
    [ApiController]
    [Route("appointments")]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IAppointmentValidationService _validationService;
        private readonly IDoctorAssignmentService _doctorAssignmentService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AppointmentsController> _logger;

        public AppointmentsController(
            IAppointmentService appointmentService,
            IAppointmentValidationService validationService,
            IDoctorAssignmentService doctorAssignmentService,
            ApplicationDbContext context,
            ILogger<AppointmentsController> logger)
        {
            _appointmentService = appointmentService;
            _validationService = validationService;
            _doctorAssignmentService = doctorAssignmentService;
            _context = context;
            _logger = logger;
        }

        [HttpGet("available-slots")]
        [Authorize]
        public async Task<IActionResult> GetAvailableTimeSlots([FromQuery] DateTime date, [FromQuery] int? doctorId = null)
        {
            try
            {
                _logger.LogInformation("Buscando horários disponíveis para {Date:yyyy-MM-dd}, Médico: {DoctorId}",
                    date, doctorId);

                var availableSlots = await _validationService.GetAvailableTimeSlotsAsync(date, doctorId);

                return Ok(new
                {
                    date = date.ToString("yyyy-MM-dd"),
                    doctorId = doctorId,
                    availableSlots = availableSlots,
                    totalSlots = availableSlots.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar horários disponíveis");
                return BadRequest(new { message = "Erro ao buscar horários disponíveis", details = ex.Message });
            }
        }

        [HttpPost("validate")]
        [Authorize]
        public async Task<IActionResult> ValidateAppointment([FromBody] ValidateAppointmentDto dto)
        {
            try
            {
                _logger.LogInformation("Validando agendamento para paciente {PatientId} em {Date}",
                    dto.PatientId, dto.AppointmentDate);

                var validation = await _validationService.ValidateAppointmentAsync(
                    dto.PatientId,
                    dto.AppointmentDate,
                    dto.DoctorId
                );

                return Ok(new
                {
                    isValid = validation.IsValid,
                    errors = validation.Errors,
                    warnings = validation.Warnings,
                    metadata = validation.Metadata
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar agendamento");
                return BadRequest(new { message = "Erro ao validar agendamento", details = ex.Message });
            }
        }

        [HttpGet("doctors-by-specialty")]
        [Authorize]
        public async Task<IActionResult> GetDoctorsBySpecialty([FromQuery] string specialty, [FromQuery] DateTime? date = null)
        {
            try
            {
                var searchDate = date ?? DateTime.Today;
                _logger.LogInformation("Buscando médicos da especialidade {Specialty} para {Date:yyyy-MM-dd}",
                    specialty, searchDate);

                var doctors = await _doctorAssignmentService.GetAvailableDoctorsBySpecialtyAsync(specialty, searchDate);

                return Ok(new
                {
                    specialty = specialty,
                    date = searchDate.ToString("yyyy-MM-dd"),
                    doctors = doctors,
                    totalDoctors = doctors.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar médicos por especialidade");
                return BadRequest(new { message = "Erro ao buscar médicos", details = ex.Message });
            }
        }

        [HttpPost("check-availability")]
        [Authorize]
        public async Task<IActionResult> CheckDoctorAvailability([FromBody] CheckAvailabilityDto dto)
        {
            try
            {
                _logger.LogInformation("Verificando disponibilidade do médico {DoctorId} em {Date}",
                    dto.DoctorId, dto.AppointmentDate);

                var isAvailable = await _doctorAssignmentService.IsDoctorAvailableAsync(dto.DoctorId, dto.AppointmentDate);

                return Ok(new
                {
                    doctorId = dto.DoctorId,
                    appointmentDate = dto.AppointmentDate,
                    isAvailable = isAvailable
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar disponibilidade");
                return BadRequest(new { message = "Erro ao verificar disponibilidade", details = ex.Message });
            }
        }

        [HttpGet("{appointmentId}")]
        [Authorize]
        public async Task<IActionResult> GetAppointmentById(int appointmentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Buscando agendamento {AppointmentId} para usuário {UserId}",
                    appointmentId, userId);

                var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);

                // Verificar se o usuário tem permissão para ver este agendamento
                // (implementar verificação de permissão se necessário)

                return Ok(appointment);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Agendamento não encontrado: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar agendamento");
                return BadRequest(new { message = "Erro ao buscar agendamento", details = ex.Message });
            }
        }

        [HttpPatch("{appointmentId}/status")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> UpdateAppointmentStatus(int appointmentId, [FromBody] UpdateStatusDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Atualizando status do agendamento {AppointmentId} para {Status} por usuário {UserId}",
                    appointmentId, dto.Status, userId);

                var updatedAppointment = await _appointmentService.UpdateAppointmentStatusAsync(appointmentId, dto.Status);

                return Ok(new
                {
                    message = "Status atualizado com sucesso",
                    appointment = updatedAppointment
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Erro na atualização de status: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar status");
                return BadRequest(new { message = "Erro ao atualizar status", details = ex.Message });
            }
        }

        [HttpPatch("{appointmentId}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelAppointment(int appointmentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Cancelando agendamento {AppointmentId} por usuário {UserId}",
                    appointmentId, userId);

                // Validar cancelamento
                var validation = await _validationService.ValidateAppointmentCancellationAsync(appointmentId, userId);
                if (!validation.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Cancelamento não permitido",
                        errors = validation.Errors,
                        warnings = validation.Warnings
                    });
                }

                var success = await _appointmentService.CancelAppointmentAsync(appointmentId, userId);

                if (success)
                {
                    return Ok(new { message = "Agendamento cancelado com sucesso" });
                }
                else
                {
                    return BadRequest(new { message = "Não foi possível cancelar o agendamento" });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Acesso negado ao cancelar agendamento: {Message}", ex.Message);
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Operação inválida: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar agendamento");
                return BadRequest(new { message = "Erro ao cancelar agendamento", details = ex.Message });
            }
        }

        [HttpPatch("{appointmentId}/reschedule")]
        [Authorize]
        public async Task<IActionResult> RescheduleAppointment(int appointmentId, [FromBody] RescheduleDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("Reagendando consulta {AppointmentId} para {NewDate} por usuário {UserId}",
                    appointmentId, dto.NewAppointmentDate, userId);

                // Validar reagendamento
                var validation = await _validationService.ValidateAppointmentUpdateAsync(appointmentId, dto.NewAppointmentDate, userId);
                if (!validation.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Reagendamento não permitido",
                        errors = validation.Errors,
                        warnings = validation.Warnings
                    });
                }

                // Implementar lógica de reagendamento
                var appointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
                
                // Verificar se o usuário tem permissão (paciente ou médico do agendamento)
                var currentAppointment = await _context.Appointments.FindAsync(appointmentId);
                if (currentAppointment == null)
                {
                    return NotFound("Agendamento não encontrado");
                }
                
                var user = await _context.Users.FindAsync(userId);
                bool canReschedule = false;
                
                if (user.Role == UserRole.Patient && currentAppointment.PatientId == userId)
                {
                    canReschedule = true;
                }
                else if (user.Role == UserRole.Doctor && currentAppointment.DoctorId == userId)
                {
                    canReschedule = true;
                }
                
                if (!canReschedule)
                {
                    return Forbid("Usuário não tem permissão para reagendar este agendamento");
                }

                // Atualizar a data do agendamento
                currentAppointment.AppointmentDate = dto.NewAppointmentDate;
                currentAppointment.UpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(dto.Reason))
                {
                    currentAppointment.Notes = $"Reagendado: {dto.Reason}";
                }
                
                await _context.SaveChangesAsync();
                
                var updatedAppointment = await _appointmentService.GetAppointmentByIdAsync(appointmentId);
                
                return Ok(new
                {
                    message = "Agendamento reagendado com sucesso",
                    appointment = updatedAppointment,
                    warnings = validation.Warnings
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Erro no reagendamento: {Message}", ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Acesso negado ao reagendar: {Message}", ex.Message);
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reagendar agendamento");
                return BadRequest(new { message = "Erro ao reagendar agendamento", details = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("Usuário não autenticado");
        }
    }

    // DTOs para o controlador
    public class ValidateAppointmentDto
    {
        public int PatientId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public int? DoctorId { get; set; }
    }

    public class CheckAvailabilityDto
    {
        public int DoctorId { get; set; }
        public DateTime AppointmentDate { get; set; }
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; }
    }

    public class RescheduleDto
    {
        public DateTime NewAppointmentDate { get; set; }
        public string Reason { get; set; }
    }
}