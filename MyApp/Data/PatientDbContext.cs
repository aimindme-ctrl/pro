using Microsoft.EntityFrameworkCore;
using MyApp.Models;

namespace MyApp.Data
{
    public class PatientDbContext : DbContext
    {
        public PatientDbContext(DbContextOptions<PatientDbContext> options)
            : base(options)
        {
        }

        public DbSet<Patient> Patients { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the Patient entity
            modelBuilder.Entity<Patient>(entity =>
            {
                entity.HasKey(e => e.PatientId);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.MedicalRecordNumber)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false);

                entity.Property(e => e.ContactInfo)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.DateOfBirth)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // One-to-Many: Patient has many Transactions
                entity.HasMany(p => p.Transactions)
                    .WithOne(t => t.Patient)
                    .HasForeignKey(t => t.PatientId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure the Transaction entity
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.TransactionId);

                entity.Property(e => e.ServiceType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Amount)
                    .HasColumnType("decimal(10, 2)")
                    .IsRequired();

                entity.Property(e => e.TransactionDate)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(t => t.Patient)
                    .WithMany(p => p.Transactions)
                    .HasForeignKey(t => t.PatientId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
