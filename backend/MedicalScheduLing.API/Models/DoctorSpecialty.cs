namespace MedicalScheduling.API.Models
{
    public class DoctorSpecialty
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public User Doctor { get; set; }
        public int SpecialtyId { get; set; }
        public Specialty Specialty { get; set; }

        public bool IsPrimary { get; set; } = false;
        public string LicenseNumber { get; set; } 
        public DateTime CertificationDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}