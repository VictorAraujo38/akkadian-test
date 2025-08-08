namespace MedicalScheduling.API.DTOs
{
    public class CreateAppointmentDto
    {
        public DateTime AppointmentDate { get; set; }
        public string Symptoms { get; set; }
    }

    public class AppointmentDto
    {
        public int Id { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Symptoms { get; set; }
        public string RecommendedSpecialty { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
    }

    public class TriageRequestDto
    {
        public string Symptoms { get; set; }
    }

    public class TriageResponseDto
    {
        public string RecommendedSpecialty { get; set; }
        public string Confidence { get; set; }
    }
}