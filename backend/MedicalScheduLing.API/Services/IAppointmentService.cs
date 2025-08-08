using MedicalScheduling.API.DTOs;

namespace MedicalScheduling.API.Services
{
    public interface IAppointmentService
    {
        Task<AppointmentDto> CreateAppointmentAsync(int patientId, CreateAppointmentDto dto);
        Task<List<AppointmentDto>> GetPatientAppointmentsAsync(int patientId);
        Task<List<AppointmentDto>> GetDoctorAppointmentsByDateAsync(int doctorId, DateTime date);
    }
}
