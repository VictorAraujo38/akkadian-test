namespace MedicalScheduling.API.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public User Patient { get; set; }
        public int? DoctorId { get; set; }
        public User Doctor { get; set; }
        public int? SpecialtyId { get; set; } 
        public Specialty Specialty { get; set; } 
        public DateTime AppointmentDate { get; set; }
        public string Symptoms { get; set; }
        public string RecommendedSpecialty { get; set; } 
        public string TriageReasoning { get; set; }
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string Notes { get; set; }
    }

    public enum AppointmentStatus
    {
        Scheduled,   // Agendado
        Confirmed,   // Confirmado
        InProgress,  // Em andamento
        Completed,   // Concluído
        Cancelled,   // Cancelado
        NoShow       // Paciente não compareceu
    }
}