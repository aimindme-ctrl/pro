using Microsoft.AspNetCore.Mvc;
using MyApp.Models;
using MyApp.Services;

namespace MyApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly PatientService _patientService;
        private readonly ILogger<PatientsController> _logger;

        public PatientsController(PatientService patientService, ILogger<PatientsController> logger)
        {
            _patientService = patientService;
            _logger = logger;
        }

        /// <summary>
        /// Get all patients
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Patient>>> GetAllPatients()
        {
            try
            {
                var patients = await _patientService.GetAllPatientsAsync();
                return Ok(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patients");
                return StatusCode(500, new { message = "Error retrieving patients", error = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific patient by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Patient>> GetPatientById(int id)
        {
            try
            {
                var patient = await _patientService.GetPatientByIdAsync(id);
                if (patient == null)
                    return NotFound(new { message = $"Patient with ID {id} not found" });

                return Ok(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving patient {id}");
                return StatusCode(500, new { message = "Error retrieving patient", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new patient
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Patient>> CreatePatient([FromBody] Patient patient)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var createdPatient = await _patientService.CreatePatientAsync(patient);
                return CreatedAtAction(nameof(GetPatientById), new { id = createdPatient.PatientId }, createdPatient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient");
                return StatusCode(500, new { message = "Error creating patient", error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing patient
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<Patient>> UpdatePatient(int id, [FromBody] Patient patient)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != patient.PatientId)
                    return BadRequest(new { message = "ID mismatch" });

                var updatedPatient = await _patientService.UpdatePatientAsync(id, patient);
                if (updatedPatient == null)
                    return NotFound(new { message = $"Patient with ID {id} not found" });

                return Ok(updatedPatient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating patient {id}");
                return StatusCode(500, new { message = "Error updating patient", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a patient by ID
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePatient(int id)
        {
            try
            {
                var deleted = await _patientService.DeletePatientAsync(id);
                if (!deleted)
                    return NotFound(new { message = $"Patient with ID {id} not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting patient {id}");
                return StatusCode(500, new { message = "Error deleting patient", error = ex.Message });
            }
        }

        /// <summary>
        /// Search patients by name
        /// </summary>
        [HttpGet("search/by-name")]
        public async Task<ActionResult<IEnumerable<Patient>>> SearchByName([FromQuery] string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest(new { message = "Name parameter is required" });

                var patients = await _patientService.GetAllPatientsAsync();
                var results = patients.Where(p => p.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching patients");
                return StatusCode(500, new { message = "Error searching patients", error = ex.Message });
            }
        }
    }
}
