namespace MedicalScheduling.API.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? CrmNumber { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;

        // Relacionamentos
        public List<Appointment> Appointments { get; set; } = new List<Appointment>();
        public List<DoctorSpecialty> DoctorSpecialties { get; set; } = new List<DoctorSpecialty>();
    }

    public enum UserRole
    {
        Patient,
        Doctor
    }
}