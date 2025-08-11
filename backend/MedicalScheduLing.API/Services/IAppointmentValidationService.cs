namespace MedicalScheduling.API.Services
{
    public interface IAppointmentValidationService
    {
        Task<ValidationResult> ValidateAppointmentAsync(int patientId, DateTime appointmentDate, int? doctorId = null);
        Task<List<DateTime>> GetAvailableTimeSlotsAsync(DateTime date, int? doctorId = null);
        Task<bool> IsTimeSlotAvailableAsync(DateTime appointmentDate, int? doctorId = null);
        Task<ValidationResult> ValidateAppointmentUpdateAsync(int appointmentId, DateTime newDate, int userId);
        Task<ValidationResult> ValidateAppointmentCancellationAsync(int appointmentId, int userId);
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}