namespace MedicalScheduling.API.Models
{
    public class Specialty
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Department { get; set; } // Ex: Clínicas, Cirúrgicas, Diagnóstico
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        // Relacionamentos
        public List<DoctorSpecialty> DoctorSpecialties { get; set; } = new List<DoctorSpecialty>();
        public List<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}