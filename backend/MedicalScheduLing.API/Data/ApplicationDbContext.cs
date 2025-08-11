using Microsoft.EntityFrameworkCore;
using MedicalScheduling.API.Models;

namespace MedicalScheduling.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Specialty> Specialties { get; set; }
        public DbSet<DoctorSpecialty> DoctorSpecialties { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User configuration
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<int>();

            // Campos opcionais para User
            modelBuilder.Entity<User>()
                .Property(u => u.CrmNumber)
                .HasMaxLength(20)
                .IsRequired(false); // NULLABLE

            modelBuilder.Entity<User>()
                .Property(u => u.Phone)
                .HasMaxLength(20)
                .IsRequired(false); // NULLABLE

            modelBuilder.Entity<User>()
                .Property(u => u.Address)
                .HasMaxLength(200)
                .IsRequired(false); // NULLABLE

            // Specialty configuration
            modelBuilder.Entity<Specialty>()
                .Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<Specialty>()
                .Property(s => s.Description)
                .HasMaxLength(500);

            modelBuilder.Entity<Specialty>()
                .Property(s => s.Department)
                .HasMaxLength(100);

            modelBuilder.Entity<Specialty>()
                .HasIndex(s => s.Name)
                .IsUnique();

            // DoctorSpecialty configuration (Many-to-Many)
            modelBuilder.Entity<DoctorSpecialty>()
                .HasKey(ds => ds.Id);

            modelBuilder.Entity<DoctorSpecialty>()
                .HasIndex(ds => new { ds.DoctorId, ds.SpecialtyId })
                .IsUnique();

            modelBuilder.Entity<DoctorSpecialty>()
                .HasOne(ds => ds.Doctor)
                .WithMany(d => d.DoctorSpecialties)
                .HasForeignKey(ds => ds.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DoctorSpecialty>()
                .HasOne(ds => ds.Specialty)
                .WithMany(s => s.DoctorSpecialties)
                .HasForeignKey(ds => ds.SpecialtyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DoctorSpecialty>()
                .Property(ds => ds.LicenseNumber)
                .HasMaxLength(50);

            // Appointment relationships
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(u => u.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Specialty)
                .WithMany(s => s.Appointments)
                .HasForeignKey(a => a.SpecialtyId)
                .OnDelete(DeleteBehavior.SetNull);

            // Appointment properties
            modelBuilder.Entity<Appointment>()
                .Property(a => a.Status)
                .HasConversion<int>()
                .HasDefaultValue(AppointmentStatus.Scheduled);

            modelBuilder.Entity<Appointment>()
                .Property(a => a.Symptoms)
                .IsRequired()
                .HasMaxLength(2000);

            modelBuilder.Entity<Appointment>()
                .Property(a => a.RecommendedSpecialty)
                .HasMaxLength(100);

            modelBuilder.Entity<Appointment>()
                .Property(a => a.TriageReasoning)
                .HasMaxLength(1000);

            modelBuilder.Entity<Appointment>()
                .Property(a => a.Notes)
                .HasMaxLength(1000);

            // Indexes for better performance
            modelBuilder.Entity<Appointment>()
                .HasIndex(a => a.AppointmentDate);

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => a.Status);

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => new { a.DoctorId, a.AppointmentDate });

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => new { a.PatientId, a.AppointmentDate });

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => a.SpecialtyId);

            // Auto-update timestamps
            modelBuilder.Entity<Appointment>()
                .Property(a => a.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<Specialty>()
                .Property(s => s.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            modelBuilder.Entity<DoctorSpecialty>()
                .Property(ds => ds.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Appointment && e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is Appointment appointment)
                {
                    appointment.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}