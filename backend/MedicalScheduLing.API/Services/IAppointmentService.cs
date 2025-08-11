using MedicalScheduling.API.DTOs;

namespace MedicalScheduling.API.Services
{
    public interface IAppointmentService
    {
        Task<AppointmentDto> CreateAppointmentAsync(int patientId, CreateAppointmentDto dto);
        Task<List<AppointmentDto>> GetPatientAppointmentsAsync(int patientId);
        Task<List<AppointmentDto>> GetDoctorAppointmentsByDateAsync(int doctorId, DateTime date);
        Task<List<DateTime>> GetAvailableTimeSlotsAsync(DateTime date);
        Task<List<SpecialtyDto>> GetAllSpecialtiesAsync();
        Task<AppointmentDto> UpdateAppointmentStatusAsync(int appointmentId, string status);
        Task<bool> CancelAppointmentAsync(int appointmentId, int userId);
        Task<AppointmentDto> GetAppointmentByIdAsync(int appointmentId);
    }
}