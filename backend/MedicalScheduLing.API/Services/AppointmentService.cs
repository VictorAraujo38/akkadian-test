using Microsoft.EntityFrameworkCore;
using MedicalScheduling.API.Data;
using MedicalScheduling.API.DTOs;
using MedicalScheduling.API.Models;

namespace MedicalScheduling.API.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITriageService _triageService;

        public AppointmentService(ApplicationDbContext context, ITriageService triageService)
        {
            _context = context;
            _triageService = triageService;
        }

        public async Task<AppointmentDto> CreateAppointmentAsync(int patientId, CreateAppointmentDto dto)
        {
            // Process triage
            var triageResult = await _triageService.ProcessTriageAsync(dto.Symptoms);

            // Create appointment
            var appointment = new Appointment
            {
                PatientId = patientId,
                AppointmentDate = dto.AppointmentDate,
                Symptoms = dto.Symptoms,
                RecommendedSpecialty = triageResult.RecommendedSpecialty,
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // Load patient data
            await _context.Entry(appointment)
                .Reference(a => a.Patient)
                .LoadAsync();

            return new AppointmentDto
            {
                Id = appointment.Id,
                AppointmentDate = appointment.AppointmentDate,
                Symptoms = appointment.Symptoms,
                RecommendedSpecialty = appointment.RecommendedSpecialty,
                PatientName = appointment.Patient?.Name
            };
        }

        public async Task<List<AppointmentDto>> GetPatientAppointmentsAsync(int patientId)
        {
            // Get all appointments
            var allAppointments = await _context.Appointments.ToListAsync();
            
            // Filter and sort in memory
            var appointments = allAppointments
                .Where(a => a.PatientId == patientId)
                .OrderBy(a => a.AppointmentDate)
                .ToList();
            
            // Get patient information
            var patient = await _context.Users.FindAsync(patientId);
            
            // Create DTOs
            var result = new List<AppointmentDto>();
            
            foreach (var appointment in appointments)
            {
                var doctorName = "";
                if (appointment.DoctorId.HasValue)
                {
                    var doctor = await _context.Users.FindAsync(appointment.DoctorId.Value);
                    doctorName = doctor?.Name;
                }
                
                result.Add(new AppointmentDto
                {
                    Id = appointment.Id,
                    AppointmentDate = appointment.AppointmentDate,
                    Symptoms = appointment.Symptoms,
                    RecommendedSpecialty = appointment.RecommendedSpecialty,
                    PatientName = patient?.Name,
                    DoctorName = doctorName
                });
            }
            
            return result;
        }

        public async Task<List<AppointmentDto>> GetDoctorAppointmentsByDateAsync(int doctorId, DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            return await _context.Appointments
                .Where(a => a.AppointmentDate >= startDate && a.AppointmentDate < endDate)
                .Include(a => a.Patient)
                .OrderBy(a => a.AppointmentDate)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    AppointmentDate = a.AppointmentDate,
                    Symptoms = a.Symptoms,
                    RecommendedSpecialty = a.RecommendedSpecialty,
                    PatientName = a.Patient.Name
                })
                .ToListAsync();
        }
    }
}