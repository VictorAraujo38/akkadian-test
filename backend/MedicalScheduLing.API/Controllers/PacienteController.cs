using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedicalScheduling.API.DTOs;
using MedicalScheduling.API.Services;
using System.Security.Claims;

namespace MedicalScheduling.API.Controllers
{
    [ApiController]
    [Route("paciente")]
    [Authorize(Roles = "Patient")]
    public class PacienteController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public PacienteController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        [HttpPost("agendamentos")]
        public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _appointmentService.CreateAppointmentAsync(userId, dto);
            return Ok(result);
        }

        [HttpGet("agendamentos")]
        public async Task<IActionResult> GetMyAppointments()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var appointments = await _appointmentService.GetPatientAppointmentsAsync(userId);
            return Ok(appointments);
        }
    }
}