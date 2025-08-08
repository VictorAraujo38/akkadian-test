namespace MedicalScheduling.API.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public User Patient { get; set; }
        public int? DoctorId { get; set; }
        public User Doctor { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Symptoms { get; set; }
        public string RecommendedSpecialty { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}