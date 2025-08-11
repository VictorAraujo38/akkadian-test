using Microsoft.EntityFrameworkCore;
using MedicalScheduling.API.Data;
using MedicalScheduling.API.DTOs;
using MedicalScheduling.API.Models;

namespace MedicalScheduling.API.Services
{
    public interface IDoctorAssignmentService
    {
        Task<int?> AssignDoctorToAppointmentAsync(string specialtyName, DateTime appointmentDate);
        Task<List<DoctorAssignmentDto>> GetAvailableDoctorsBySpecialtyAsync(string specialtyName, DateTime date);
        Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime appointmentDate);
        Task<List<SpecialtyDto>> GetAllSpecialtiesAsync();
        Task<int?> GetSpecialtyIdByNameAsync(string specialtyName);
    }

    public class DoctorAssignmentService : IDoctorAssignmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DoctorAssignmentService> _logger;

        public DoctorAssignmentService(ApplicationDbContext context, ILogger<DoctorAssignmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int?> AssignDoctorToAppointmentAsync(string specialtyName, DateTime appointmentDate)
        {
            try
            {
                // 1. Buscar especialidade
                var specialty = await _context.Specialties
                    .FirstOrDefaultAsync(s => s.Name == specialtyName && s.IsActive);

                if (specialty == null)
                {
                    _logger.LogWarning($"Especialidade '{specialtyName}' não encontrada");
                    return null;
                }

                // 2. Buscar médicos da especialidade
                var availableDoctors = await GetAvailableDoctorsBySpecialtyAsync(specialtyName, appointmentDate);

                if (!availableDoctors.Any())
                {
                    _logger.LogWarning($"Nenhum médico disponível para {specialtyName} em {appointmentDate}");
                    return null;
                }

                // 3. Selecionar médico com menos agendamentos no dia (load balancing)
                var doctorWithLeastAppointments = await GetDoctorWithLeastAppointmentsAsync(
                    availableDoctors.Select(d => d.DoctorId).ToList(),
                    appointmentDate.Date
                );

                _logger.LogInformation($"Médico {doctorWithLeastAppointments} atribuído para {specialtyName}");
                return doctorWithLeastAppointments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao atribuir médico para {specialtyName}");
                return null;
            }
        }

        public async Task<List<DoctorAssignmentDto>> GetAvailableDoctorsBySpecialtyAsync(string specialtyName, DateTime date)
        {
            try
            {
                var availableDoctors = await _context.DoctorSpecialties
                    .Include(ds => ds.Doctor)
                    .Include(ds => ds.Specialty)
                    .Where(ds => ds.Specialty.Name == specialtyName &&
                                ds.IsActive &&
                                ds.Doctor.IsActive &&
                                ds.Doctor.Role == UserRole.Doctor)
                    .Select(ds => new DoctorAssignmentDto
                    {
                        DoctorId = ds.DoctorId,
                        DoctorName = ds.Doctor.Name,
                        Specialty = ds.Specialty.Name,
                        IsAvailable = true, // Será verificado individualmente
                        CrmNumber = ds.Doctor.CrmNumber,
                        IsPrimarySpecialty = ds.IsPrimary
                    })
                    .ToListAsync();

                // Verificar disponibilidade individual de cada médico
                var result = new List<DoctorAssignmentDto>();
                foreach (var doctor in availableDoctors)
                {
                    doctor.IsAvailable = await IsDoctorAvailableAsync(doctor.DoctorId, date);
                    if (doctor.IsAvailable)
                    {
                        result.Add(doctor);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao buscar médicos para {specialtyName}");
                return new List<DoctorAssignmentDto>();
            }
        }

        public async Task<bool> IsDoctorAvailableAsync(int doctorId, DateTime appointmentDate)
        {
            try
            {
                // Verificar se o médico já tem consulta no mesmo horário
                var hasConflict = await _context.Appointments
                    .AnyAsync(a => a.DoctorId == doctorId &&
                                  a.AppointmentDate == appointmentDate &&
                                  a.Status != AppointmentStatus.Cancelled);

                // Verificar se está dentro do horário de trabalho (8h às 18h)
                var hour = appointmentDate.Hour;
                var isWorkingHours = hour >= 8 && hour <= 17;

                // Verificar se não é final de semana
                var isWeekday = appointmentDate.DayOfWeek != DayOfWeek.Saturday &&
                               appointmentDate.DayOfWeek != DayOfWeek.Sunday;

                // Verificar limite de consultas por dia (máximo 8 consultas por médico)
                var startOfDay = appointmentDate.Date;
                var endOfDay = startOfDay.AddDays(1);

                var appointmentsToday = await _context.Appointments
                    .CountAsync(a => a.DoctorId == doctorId &&
                                    a.AppointmentDate >= startOfDay &&
                                    a.AppointmentDate < endOfDay &&
                                    a.Status != AppointmentStatus.Cancelled);

                var hasCapacity = appointmentsToday < 8;

                return !hasConflict && isWorkingHours && isWeekday && hasCapacity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao verificar disponibilidade do médico {doctorId}");
                return false;
            }
        }

        public async Task<List<SpecialtyDto>> GetAllSpecialtiesAsync()
        {
            try
            {
                return await _context.Specialties
                    .Where(s => s.IsActive)
                    .Select(s => new SpecialtyDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Description = s.Description,
                        Department = s.Department,
                        DoctorCount = s.DoctorSpecialties.Count(ds => ds.IsActive && ds.Doctor.IsActive)
                    })
                    .OrderBy(s => s.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar especialidades");
                return new List<SpecialtyDto>();
            }
        }

        public async Task<int?> GetSpecialtyIdByNameAsync(string specialtyName)
        {
            try
            {
                var specialty = await _context.Specialties
                    .FirstOrDefaultAsync(s => s.Name == specialtyName && s.IsActive);

                return specialty?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao buscar ID da especialidade {specialtyName}");
                return null;
            }
        }

        private async Task<int?> GetDoctorWithLeastAppointmentsAsync(List<int> doctorIds, DateTime date)
        {
            try
            {
                var startDate = date.Date;
                var endDate = startDate.AddDays(1);

                var doctorAppointmentCounts = await _context.Appointments
                    .Where(a => doctorIds.Contains(a.DoctorId.Value) &&
                               a.AppointmentDate >= startDate &&
                               a.AppointmentDate < endDate &&
                               a.Status != AppointmentStatus.Cancelled)
                    .GroupBy(a => a.DoctorId)
                    .Select(g => new { DoctorId = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Se nenhum médico tem agendamentos, pegar o primeiro
                if (!doctorAppointmentCounts.Any())
                {
                    return doctorIds.FirstOrDefault();
                }

                // Incluir médicos que não têm agendamentos no resultado
                var allDoctorCounts = doctorIds.Select(id => new
                {
                    DoctorId = id,
                    Count = doctorAppointmentCounts.FirstOrDefault(dac => dac.DoctorId == id)?.Count ?? 0
                }).ToList();

                // Retornar médico com menos agendamentos
                var doctorWithLeast = allDoctorCounts
                    .OrderBy(x => x.Count)
                    .FirstOrDefault();

                return doctorWithLeast?.DoctorId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar médico com menos agendamentos");
                return doctorIds.FirstOrDefault();
            }
        }
    }

    public class SpecialtyDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Department { get; set; }
        public int DoctorCount { get; set; }
    }
}