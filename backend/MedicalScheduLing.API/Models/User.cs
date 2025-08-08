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
        public List<Appointment> Appointments { get; set; }
    }

    public enum UserRole
    {
        Patient,
        Doctor
    }
}