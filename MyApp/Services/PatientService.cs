using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Models;

namespace MyApp.Services
{
    public class PatientService
    {
        private readonly PatientDbContext _context;

        public PatientService(PatientDbContext context)
        {
            _context = context;
        }

        // Create - Add a new patient
        public async Task<Patient> CreatePatientAsync(Patient patient)
        {
            if (string.IsNullOrWhiteSpace(patient.MedicalRecordNumber))
            {
                patient.MedicalRecordNumber = GenerateMedicalRecordNumber();
            }

            patient.CreatedAt = DateTime.UtcNow;
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();
            return patient;
        }

        // Read - Get all patients
        public async Task<List<Patient>> GetAllPatientsAsync()
        {
            return await _context.Patients
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        // Read - Get patient by ID
        public async Task<Patient?> GetPatientByIdAsync(int patientId)
        {
            return await _context.Patients
                .FirstOrDefaultAsync(p => p.PatientId == patientId);
        }

        // Read - Get patient by Medical Record Number
        public async Task<Patient?> GetPatientByMrnAsync(string medicalRecordNumber)
        {
            return await _context.Patients
                .FirstOrDefaultAsync(p => p.MedicalRecordNumber == medicalRecordNumber);
        }

        // Update - Update patient information
        public async Task<Patient?> UpdatePatientAsync(int patientId, Patient updatedPatient)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
                return null;

            patient.Name = updatedPatient.Name;
            patient.DateOfBirth = updatedPatient.DateOfBirth;
            patient.ContactInfo = updatedPatient.ContactInfo;
            patient.UpdatedAt = DateTime.UtcNow;

            _context.Patients.Update(patient);
            await _context.SaveChangesAsync();
            return patient;
        }

        // Delete - Delete a patient
        public async Task<bool> DeletePatientAsync(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
                return false;

            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
            return true;
        }

        // Helper method to generate Medical Record Number
        private string GenerateMedicalRecordNumber()
        {
            return $"MRN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }
    }
}
