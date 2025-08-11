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
            // Get all appointments for the patient
            var appointments = await _context.Appointments
                .Where(a => a.PatientId == patientId)
                .ToListAsync();
            
            // Sort by appointment date
            appointments = appointments.OrderBy(a => a.AppointmentDate).ToList();
            
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
                    DoctorName = doctorName,
                    Status = appointment.Status.ToString()
                });
            }
            
            return result;
        }

        public async Task<List<AppointmentDto>> GetDoctorAppointmentsByDateAsync(int doctorId, DateTime date)
        {
            var startDate = date.Date;
            var endDate = startDate.AddDays(1);

            return await _context.Appointments
                .Where(a => a.DoctorId == doctorId && a.AppointmentDate >= startDate && a.AppointmentDate < endDate)
                .Include(a => a.Patient)
                .OrderBy(a => a.AppointmentDate)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    AppointmentDate = a.AppointmentDate,
                    Symptoms = a.Symptoms,
                    RecommendedSpecialty = a.RecommendedSpecialty,
                    PatientName = a.Patient.Name,
                    Status = a.Status.ToString()
                })
                .ToListAsync();
        }

        public async Task<List<DateTime>> GetAvailableTimeSlotsAsync(DateTime date)
        {
            var availableSlots = new List<DateTime>();
            var startTime = new TimeSpan(8, 0, 0); // 8:00 AM
            var endTime = new TimeSpan(18, 0, 0);  // 6:00 PM
            var slotDuration = TimeSpan.FromMinutes(30);

            var currentSlot = date.Date.Add(startTime);
            var endDateTime = date.Date.Add(endTime);

            // Get total number of active doctors
            var totalActiveDoctors = await _context.Users
                .CountAsync(u => u.Role == UserRole.Doctor && u.IsActive);

            while (currentSlot < endDateTime)
            {
                // Count how many appointments are booked for this time slot
                var bookedCount = await _context.Appointments
                    .CountAsync(a => a.AppointmentDate == currentSlot && a.Status != AppointmentStatus.Cancelled);

                // If there are available doctors for this time slot, add it
                if (bookedCount < totalActiveDoctors)
                {
                    availableSlots.Add(currentSlot);
                }

                currentSlot = currentSlot.Add(slotDuration);
            }

            return availableSlots;
        }

        public async Task<List<SpecialtyDto>> GetAllSpecialtiesAsync()
        {
            return await _context.Specialties
                .Select(s => new SpecialtyDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Department = s.Department
                })
                .ToListAsync();
        }

        public async Task<AppointmentDto> GetAppointmentByIdAsync(int appointmentId)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                throw new ArgumentException($"Agendamento com ID {appointmentId} não encontrado.");
            }

            return new AppointmentDto
            {
                Id = appointment.Id,
                AppointmentDate = appointment.AppointmentDate,
                Symptoms = appointment.Symptoms,
                RecommendedSpecialty = appointment.RecommendedSpecialty,
                PatientName = appointment.Patient?.Name,
                DoctorName = appointment.Doctor?.Name,
                DoctorCrm = appointment.Doctor?.CrmNumber,
                Status = appointment.Status.ToString()
            };
        }

        public async Task<AppointmentDto> UpdateAppointmentStatusAsync(int appointmentId, string status)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                throw new ArgumentException($"Agendamento com ID {appointmentId} não encontrado.");
            }

            // Validate status
            if (!Enum.TryParse<AppointmentStatus>(status, true, out var appointmentStatus))
            {
                throw new ArgumentException($"Status '{status}' inválido.");
            }

            appointment.Status = appointmentStatus;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new AppointmentDto
            {
                Id = appointment.Id,
                AppointmentDate = appointment.AppointmentDate,
                Symptoms = appointment.Symptoms,
                RecommendedSpecialty = appointment.RecommendedSpecialty,
                PatientName = appointment.Patient?.Name,
                DoctorName = appointment.Doctor?.Name,
                DoctorCrm = appointment.Doctor?.CrmNumber,
                Status = appointment.Status.ToString()
            };
        }

        public async Task<bool> CancelAppointmentAsync(int appointmentId, int userId)
        {
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                throw new ArgumentException($"Agendamento com ID {appointmentId} não encontrado.");
            }

            // Check if user has permission to cancel (patient or doctor)
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Usuário não encontrado.");
            }

            bool canCancel = false;
            if (user.Role == UserRole.Patient && appointment.PatientId == userId)
            {
                canCancel = true;
            }
            else if (user.Role == UserRole.Doctor && appointment.DoctorId == userId)
            {
                canCancel = true;
            }

            if (!canCancel)
            {
                throw new UnauthorizedAccessException("Usuário não tem permissão para cancelar este agendamento.");
            }

            // Check if appointment can be cancelled (not already cancelled or completed)
            if (appointment.Status == AppointmentStatus.Cancelled)
            {
                throw new InvalidOperationException("Agendamento já está cancelado.");
            }

            if (appointment.Status == AppointmentStatus.Completed)
            {
                throw new InvalidOperationException("Não é possível cancelar um agendamento já concluído.");
            }

            appointment.Status = AppointmentStatus.Cancelled;
            appointment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}