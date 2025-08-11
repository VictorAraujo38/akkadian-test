namespace MedicalScheduling.API.DTOs
{
    public class CreateAppointmentDto
    {
        public DateTime AppointmentDate { get; set; }
        public string Symptoms { get; set; }
        public string PreferredSpecialty { get; set; } // Opcional
    }

    public class AppointmentDto
    {
        public int Id { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Symptoms { get; set; }
        public string RecommendedSpecialty { get; set; }
        public string PatientName { get; set; }
        public string DoctorName { get; set; }
        public string DoctorCrm { get; set; }
        public string Status { get; set; } = "Agendado";
        public string TriageReasoning { get; set; }
        public int? SpecialtyId { get; set; }
        public string SpecialtyDepartment { get; set; }
    }

    public class TriageRequestDto
    {
        public string Symptoms { get; set; }
    }

    public class TriageResponseDto
    {
        public string RecommendedSpecialty { get; set; }
        public string Confidence { get; set; }
        public string Reasoning { get; set; }
        public int? SpecialtyId { get; set; } 
    }

    public class DoctorAssignmentDto
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public string Specialty { get; set; }
        public bool IsAvailable { get; set; }
        public string CrmNumber { get; set; }
        public bool IsPrimarySpecialty { get; set; }
    }

    public class SpecialtyDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Department { get; set; }
        public int DoctorCount { get; set; }
    }

    public class DoctorDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CrmNumber { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public List<SpecialtyDto> Specialties { get; set; } = new List<SpecialtyDto>();
        public bool IsActive { get; set; }
    }

    public class CreateDoctorSpecialtyDto
    {
        public int DoctorId { get; set; }
        public int SpecialtyId { get; set; }
        public bool IsPrimary { get; set; }
        public string LicenseNumber { get; set; }
        public DateTime CertificationDate { get; set; }
    }
}