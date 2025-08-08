using Microsoft.AspNetCore.Mvc;
using MedicalScheduling.API.DTOs;
using MedicalScheduling.API.Services;

namespace MedicalScheduling.API.Controllers
{
    [ApiController]
    [Route("mock")]
    public class MockController : ControllerBase
    {
        private readonly ITriageService _triageService;

        public MockController(ITriageService triageService)
        {
            _triageService = triageService;
        }

        [HttpPost("triagem")]
        public async Task<IActionResult> Triage([FromBody] TriageRequestDto dto)
        {
            var result = await _triageService.ProcessTriageAsync(dto.Symptoms);
            return Ok(result);
        }
    }
}