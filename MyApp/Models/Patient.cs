using System.ComponentModel.DataAnnotations;

namespace MyApp.Models
{
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [StringLength(20)]
        public string MedicalRecordNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string ContactInfo { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation property
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
