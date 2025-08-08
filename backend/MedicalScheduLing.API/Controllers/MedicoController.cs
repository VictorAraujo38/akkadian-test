using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MedicalScheduling.API.Services;
using System.Security.Claims;

namespace MedicalScheduling.API.Controllers
{
    [ApiController]
    [Route("medico")]
    [Authorize(Roles = "Doctor")]
    public class MedicoController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public MedicoController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        [HttpGet("agendamentos")]
        public async Task<IActionResult> GetAppointmentsByDate([FromQuery] DateTime? data)
        {
            var doctorId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var date = data ?? DateTime.Today;
            var appointments = await _appointmentService.GetDoctorAppointmentsByDateAsync(doctorId, date);
            return Ok(appointments);
        }
    }
}