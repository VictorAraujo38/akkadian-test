using Microsoft.EntityFrameworkCore;
using MedicalScheduling.API.Data;
using MedicalScheduling.API.Models;

namespace MedicalScheduling.API.Services
{
    public class AppointmentValidationService : IAppointmentValidationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AppointmentValidationService> _logger;

        // Horários disponíveis para agendamento
        private readonly List<TimeSpan> _availableTimeSlots = new()
        {
            new TimeSpan(8, 0, 0),   // 08:00
            new TimeSpan(9, 0, 0),   // 09:00
            new TimeSpan(10, 0, 0),  // 10:00
            new TimeSpan(11, 0, 0),  // 11:00
            new TimeSpan(14, 0, 0),  // 14:00
            new TimeSpan(15, 0, 0),  // 15:00
            new TimeSpan(16, 0, 0),  // 16:00
            new TimeSpan(17, 0, 0)   // 17:00
        };

        // Configurações do sistema
        private const int MAX_APPOINTMENTS_PER_PATIENT_PER_MONTH = 5;
        private const int MAX_APPOINTMENTS_PER_DOCTOR_PER_DAY = 8;
        private const int MIN_ADVANCE_HOURS = 2;
        private const int MAX_ADVANCE_DAYS = 30;

        public AppointmentValidationService(ApplicationDbContext context, ILogger<AppointmentValidationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ValidationResult> ValidateAppointmentAsync(int patientId, DateTime appointmentDate, int? doctorId = null)
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                _logger.LogInformation("Validando agendamento - Paciente: {PatientId}, Data: {Date}, Médico: {DoctorId}", 
                    patientId, appointmentDate, doctorId);

                // 1. Validar se a data não é no passado
                if (appointmentDate <= DateTime.Now)
                {
                    result.Errors.Add("Não é possível agendar consultas no passado");
                    result.IsValid = false;
                }

                // 2. Validar antecedência mínima
                if (appointmentDate <= DateTime.Now.AddHours(MIN_ADVANCE_HOURS))
                {
                    result.Errors.Add($"Agendamentos devem ser feitos com pelo menos {MIN_ADVANCE_HOURS} horas de antecedência");
                    result.IsValid = false;
                }

                // 3. Validar antecedência máxima
                if (appointmentDate > DateTime.Now.AddDays(MAX_ADVANCE_DAYS))
                {
                    result.Errors.Add($"Não é possível agendar consultas com mais de {MAX_ADVANCE_DAYS} dias de antecedência");
                    result.IsValid = false;
                }

                // 4. Validar se é um dia útil
                if (appointmentDate.DayOfWeek == DayOfWeek.Saturday || appointmentDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    result.Errors.Add("Agendamentos só são permitidos em dias úteis (segunda a sexta-feira)");
                    result.IsValid = false;
                }

                // 5. Validar horário de funcionamento
                var timeSlot = appointmentDate.TimeOfDay;
                if (!_availableTimeSlots.Contains(timeSlot))
                {
                    var availableHours = string.Join(", ", _availableTimeSlots.Select(ts => ts.ToString(@"hh\:mm")));
                    result.Errors.Add($"Horário {appointmentDate:HH:mm} não está disponível. Horários válidos: {availableHours}");
                    result.IsValid = false;
                }

                // 6. Validar se paciente já tem consulta no mesmo dia
                var patientHasAppointmentSameDay = await _context.Appointments
                    .AnyAsync(a => a.PatientId == patientId && 
                                  a.AppointmentDate.Date == appointmentDate.Date &&
                                  a.Status != AppointmentStatus.Cancelled);

                if (patientHasAppointmentSameDay)
                {
                    result.Warnings.Add("Paciente já possui uma consulta agendada neste dia");
                }

                // 7. Validar conflito de horário
                var isTimeSlotAvailable = await IsTimeSlotAvailableAsync(appointmentDate, doctorId);
                if (!isTimeSlotAvailable)
                {
                    if (doctorId.HasValue)
                    {
                        result.Errors.Add("Este médico já possui uma consulta agendada neste horário");
                    }
                    else
                    {
                        result.Errors.Add("Este horário já está ocupado");
                    }
                    result.IsValid = false;
                }

                // 8. Validar limite de consultas por paciente por mês
                var monthlyLimit = await ValidateMonthlyPatientLimitAsync(patientId, appointmentDate);
                if (!monthlyLimit.isValid)
                {
                    if (monthlyLimit.appointmentCount >= MAX_APPOINTMENTS_PER_PATIENT_PER_MONTH)
                    {
                        result.Errors.Add($"Paciente já atingiu o limite de {MAX_APPOINTMENTS_PER_PATIENT_PER_MONTH} consultas por mês");
                        result.IsValid = false;
                    }
                    else
                    {
                        result.Warnings.Add($"Paciente possui {monthlyLimit.appointmentCount} consultas agendadas neste mês");
                    }
                }

                // 9. Validar limite de consultas por médico por dia (se médico especificado)
                if (doctorId.HasValue)
                {
                    var doctorDailyLimit = await ValidateDoctorDailyLimitAsync(doctorId.Value, appointmentDate);
                    if (!doctorDailyLimit.isValid)
                    {
                        result.Errors.Add($"Médico já atingiu o limite de {MAX_APPOINTMENTS_PER_DOCTOR_PER_DAY} consultas por dia");
                        result.IsValid = false;
                    }
                    else if (doctorDailyLimit.appointmentCount >= MAX_APPOINTMENTS_PER_DOCTOR_PER_DAY * 0.8) // 80% do limite
                    {
                        result.Warnings.Add($"Médico possui {doctorDailyLimit.appointmentCount} consultas agendadas neste dia");
                    }
                }

                // 10. Validar se o paciente existe e está ativo
                var patient = await _context.Users.FindAsync(patientId);
                if (patient == null || !patient.IsActive || patient.Role != UserRole.Patient)
                {
                    result.Errors.Add("Paciente não encontrado ou inativo");
                    result.IsValid = false;
                }

                // 11. Validar se o médico existe e está ativo (se especificado)
                if (doctorId.HasValue)
                {
                    var doctor = await _context.Users.FindAsync(doctorId.Value);
                    if (doctor == null || !doctor.IsActive || doctor.Role != UserRole.Doctor)
                    {
                        result.Errors.Add("Médico não encontrado ou inativo");
                        result.IsValid = false;
                    }
                }

                // 12. Adicionar metadados úteis
                result.Metadata.Add("appointmentDate", appointmentDate);
                result.Metadata.Add("isWeekend", appointmentDate.DayOfWeek == DayOfWeek.Saturday || appointmentDate.DayOfWeek == DayOfWeek.Sunday);
                result.Metadata.Add("hoursInAdvance", (appointmentDate - DateTime.Now).TotalHours);

                _logger.LogInformation("Validação concluída - Válido: {IsValid}, Erros: {ErrorCount}, Avisos: {WarningCount}", 
                    result.IsValid, result.Errors.Count, result.Warnings.Count);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante validação de agendamento");
                result.Errors.Add("Erro interno durante validação");
                result.IsValid = false;
            }

            return result;
        }

        public async Task<List<DateTime>> GetAvailableTimeSlotsAsync(DateTime date, int? doctorId = null)
        {
            var availableSlots = new List<DateTime>();

            try
            {
                _logger.LogInformation("Buscando horários disponíveis para {Date:yyyy-MM-dd}, Médico: {DoctorId}", 
                    date, doctorId);

                // Validar se é dia útil
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    _logger.LogInformation("Data {Date:yyyy-MM-dd} é fim de semana, sem horários disponíveis", date);
                    return availableSlots;
                }

                // Validar se não é uma data muito no futuro
                if (date > DateTime.Now.AddDays(MAX_ADVANCE_DAYS))
                {
                    _logger.LogInformation("Data {Date:yyyy-MM-dd} excede limite de {Days} dias", date, MAX_ADVANCE_DAYS);
                    return availableSlots;
                }

                foreach (var timeSlot in _availableTimeSlots)
                {
                    var appointmentDateTime = date.Date.Add(timeSlot);
                    
                    // Verificar se o horário não é no passado (com margem de antecedência)
                    if (appointmentDateTime <= DateTime.Now.AddHours(MIN_ADVANCE_HOURS))
                    {
                        continue;
                    }

                    // Verificar se o horário está disponível
                    var isAvailable = await IsTimeSlotAvailableAsync(appointmentDateTime, doctorId);
                    if (isAvailable)
                    {
                        availableSlots.Add(appointmentDateTime);
                    }
                }

                _logger.LogInformation("Encontrados {Count} horários disponíveis para {Date:yyyy-MM-dd}", 
                    availableSlots.Count, date);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar horários disponíveis para {Date:yyyy-MM-dd}", date);
            }

            return availableSlots;
        }

        public async Task<bool> IsTimeSlotAvailableAsync(DateTime appointmentDate, int? doctorId = null)
        {
            try
            {
                _logger.LogInformation("Verificando disponibilidade do horário {Date} para médico {DoctorId}", 
                    appointmentDate, doctorId);

                // Se um médico específico foi informado, verificar apenas para ele
                if (doctorId.HasValue)
                {
                    var doctorHasAppointment = await _context.Appointments
                        .AnyAsync(a => a.DoctorId == doctorId.Value &&
                                      a.AppointmentDate == appointmentDate &&
                                      a.Status != AppointmentStatus.Cancelled);
                    
                    return !doctorHasAppointment;
                }
                
                // Se nenhum médico foi especificado, verificar se há algum médico disponível
                // Primeiro, buscar todos os médicos ativos
                var activeDoctors = await _context.Users
                    .Where(u => u.Role == UserRole.Doctor && u.IsActive)
                    .Select(u => u.Id)
                    .ToListAsync();

                // Verificar se pelo menos um médico está disponível neste horário
                foreach (var doctorIdToCheck in activeDoctors)
                {
                    var doctorIsAvailable = await _context.Appointments
                        .AnyAsync(a => a.DoctorId == doctorIdToCheck &&
                                      a.AppointmentDate == appointmentDate &&
                                      a.Status != AppointmentStatus.Cancelled);
                    
                    if (!doctorIsAvailable)
                    {
                        return true; // Pelo menos um médico está disponível
                    }
                }
                
                return false; // Nenhum médico disponível
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar disponibilidade do horário {Date}", appointmentDate);
                return false;
            }
        }


        public async Task<ValidationResult> ValidateAppointmentUpdateAsync(int appointmentId, DateTime newDate, int userId)
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                _logger.LogInformation("Validando reagendamento - ID: {AppointmentId}, Nova data: {NewDate}, Usuário: {UserId}", 
                    appointmentId, newDate, userId);

                // 1. Verificar se o agendamento existe
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    result.Errors.Add("Agendamento não encontrado");
                    result.IsValid = false;
                    return result;
                }

                // 2. Verificar permissões
                if (appointment.PatientId != userId && appointment.DoctorId != userId)
                {
                    result.Errors.Add("Usuário não tem permissão para reagendar este agendamento");
                    result.IsValid = false;
                    return result;
                }

                // 3. Verificar se o agendamento pode ser reagendado
                if (appointment.Status == AppointmentStatus.Completed || 
                    appointment.Status == AppointmentStatus.Cancelled)
                {
                    result.Errors.Add($"Agendamento não pode ser reagendado - Status atual: {appointment.Status}");
                    result.IsValid = false;
                    return result;
                }

                // 4. Verificar antecedência para reagendamento (mínimo 4 horas)
                if (appointment.AppointmentDate <= DateTime.Now.AddHours(4))
                {
                    result.Errors.Add("Agendamentos só podem ser reagendados com pelo menos 4 horas de antecedência");
                    result.IsValid = false;
                }

                // 5. Validar a nova data usando as mesmas regras de criação
                var newDateValidation = await ValidateAppointmentAsync(appointment.PatientId, newDate, appointment.DoctorId);
                
                // Copiar erros e warnings da validação da nova data
                result.Errors.AddRange(newDateValidation.Errors);
                result.Warnings.AddRange(newDateValidation.Warnings);
                
                if (!newDateValidation.IsValid)
                {
                    result.IsValid = false;
                }

                // 6. Adicionar metadados específicos do reagendamento
                result.Metadata.Add("originalDate", appointment.AppointmentDate);
                result.Metadata.Add("newDate", newDate);
                result.Metadata.Add("appointmentStatus", appointment.Status.ToString());

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante validação de reagendamento");
                result.Errors.Add("Erro interno durante validação de reagendamento");
                result.IsValid = false;
            }

            return result;
        }

        public async Task<ValidationResult> ValidateAppointmentCancellationAsync(int appointmentId, int userId)
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                _logger.LogInformation("Validando cancelamento - ID: {AppointmentId}, Usuário: {UserId}", 
                    appointmentId, userId);

                // 1. Verificar se o agendamento existe
                var appointment = await _context.Appointments
                    .Include(a => a.Patient)
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.Id == appointmentId);

                if (appointment == null)
                {
                    result.Errors.Add("Agendamento não encontrado");
                    result.IsValid = false;
                    return result;
                }

                // 2. Verificar permissões
                if (appointment.PatientId != userId && appointment.DoctorId != userId)
                {
                    result.Errors.Add("Usuário não tem permissão para cancelar este agendamento");
                    result.IsValid = false;
                    return result;
                }

                // 3. Verificar se o agendamento pode ser cancelado
                if (appointment.Status == AppointmentStatus.Completed)
                {
                    result.Errors.Add("Agendamentos já concluídos não podem ser cancelados");
                    result.IsValid = false;
                }

                if (appointment.Status == AppointmentStatus.Cancelled)
                {
                    result.Warnings.Add("Agendamento já foi cancelado anteriormente");
                }

                // 4. Verificar antecedência para cancelamento (mínimo 2 horas)
                if (appointment.AppointmentDate <= DateTime.Now.AddHours(2))
                {
                    result.Errors.Add("Agendamentos só podem ser cancelados com pelo menos 2 horas de antecedência");
                    result.IsValid = false;
                }

                // 5. Adicionar metadados do cancelamento
                result.Metadata.Add("appointmentDate", appointment.AppointmentDate);
                result.Metadata.Add("currentStatus", appointment.Status.ToString());
                result.Metadata.Add("hoursUntilAppointment", (appointment.AppointmentDate - DateTime.Now).TotalHours);

                // 6. Verificar política de cancelamento (warnings)
                var hoursUntilAppointment = (appointment.AppointmentDate - DateTime.Now).TotalHours;
                if (hoursUntilAppointment < 24)
                {
                    result.Warnings.Add("Cancelamento com menos de 24 horas pode gerar custos adicionais");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante validação de cancelamento");
                result.Errors.Add("Erro interno durante validação de cancelamento");
                result.IsValid = false;
            }

            return result;
        }

        private async Task<(bool isValid, int appointmentCount)> ValidateMonthlyPatientLimitAsync(int patientId, DateTime appointmentDate)
        {
            try
            {
                var startOfMonth = new DateTime(appointmentDate.Year, appointmentDate.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1);
                
                var appointmentCount = await _context.Appointments
                    .CountAsync(a => a.PatientId == patientId && 
                                    a.AppointmentDate >= startOfMonth && 
                                    a.AppointmentDate < endOfMonth &&
                                    a.Status != AppointmentStatus.Cancelled);

                return (appointmentCount < MAX_APPOINTMENTS_PER_PATIENT_PER_MONTH, appointmentCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar limite mensal do paciente {PatientId}", patientId);
                return (false, 0);
            }
        }

        private async Task<(bool isValid, int appointmentCount)> ValidateDoctorDailyLimitAsync(int doctorId, DateTime appointmentDate)
        {
            try
            {
                var startOfDay = appointmentDate.Date;
                var endOfDay = startOfDay.AddDays(1);
                
                var appointmentCount = await _context.Appointments
                    .CountAsync(a => a.DoctorId == doctorId && 
                                    a.AppointmentDate >= startOfDay && 
                                    a.AppointmentDate < endOfDay &&
                                    a.Status != AppointmentStatus.Cancelled);

                return (appointmentCount < MAX_APPOINTMENTS_PER_DOCTOR_PER_DAY, appointmentCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar limite diário do médico {DoctorId}", doctorId);
                return (false, 0);
            }
        }
    }
}