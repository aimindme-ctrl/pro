using Microsoft.AspNetCore.Mvc;
using MyApp.Models;
using MyApp.Services;

namespace MyApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionsController : ControllerBase
    {
        private readonly TransactionService _transactionService;
        private readonly PatientService _patientService;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(TransactionService transactionService, PatientService patientService, ILogger<TransactionsController> logger)
        {
            _transactionService = transactionService;
            _patientService = patientService;
            _logger = logger;
        }

        /// <summary>
        /// Get all transactions
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetAllTransactions()
        {
            try
            {
                var transactions = await _transactionService.GetAllTransactionsAsync();
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transactions");
                return StatusCode(500, new { message = "Error retrieving transactions", error = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific transaction by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Transaction>> GetTransactionById(int id)
        {
            try
            {
                var transaction = await _transactionService.GetTransactionByIdAsync(id);
                if (transaction == null)
                    return NotFound(new { message = $"Transaction with ID {id} not found" });

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving transaction {id}");
                return StatusCode(500, new { message = "Error retrieving transaction", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new transaction
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Transaction>> CreateTransaction([FromBody] Transaction transaction)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Verify patient exists
                var patient = await _patientService.GetPatientByIdAsync(transaction.PatientId);
                if (patient == null)
                    return BadRequest(new { message = $"Patient with ID {transaction.PatientId} does not exist" });

                var createdTransaction = await _transactionService.CreateTransactionAsync(transaction);
                return CreatedAtAction(nameof(GetTransactionById), new { id = createdTransaction.TransactionId }, createdTransaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating transaction");
                return StatusCode(500, new { message = "Error creating transaction", error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing transaction
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<Transaction>> UpdateTransaction(int id, [FromBody] Transaction transaction)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != transaction.TransactionId)
                    return BadRequest(new { message = "ID mismatch" });

                var updatedTransaction = await _transactionService.UpdateTransactionAsync(id, transaction);
                if (updatedTransaction == null)
                    return NotFound(new { message = $"Transaction with ID {id} not found" });

                return Ok(updatedTransaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating transaction {id}");
                return StatusCode(500, new { message = "Error updating transaction", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a transaction by ID
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTransaction(int id)
        {
            try
            {
                var deleted = await _transactionService.DeleteTransactionAsync(id);
                if (!deleted)
                    return NotFound(new { message = $"Transaction with ID {id} not found" });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting transaction {id}");
                return StatusCode(500, new { message = "Error deleting transaction", error = ex.Message });
            }
        }

        /// <summary>
        /// Get transactions by patient ID
        /// </summary>
        [HttpGet("by-patient/{patientId}")]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactionsByPatient(int patientId)
        {
            try
            {
                var transactions = await _transactionService.GetTransactionsByPatientIdAsync(patientId);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving transactions for patient {patientId}");
                return StatusCode(500, new { message = "Error retrieving transactions", error = ex.Message });
            }
        }

        /// <summary>
        /// Get transactions by status (Paid/Unpaid)
        /// </summary>
        [HttpGet("by-status/{status}")]
        public async Task<ActionResult<IEnumerable<Transaction>>> GetTransactionsByStatus(string status)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(status))
                    return BadRequest(new { message = "Status parameter is required" });

                var transactions = await _transactionService.GetTransactionsByStatusAsync(status);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving transactions by status {status}");
                return StatusCode(500, new { message = "Error retrieving transactions", error = ex.Message });
            }
        }

        /// <summary>
        /// Get dashboard data (analytics)
        /// </summary>
        [HttpGet("analytics/dashboard")]
        public async Task<ActionResult<object>> GetDashboardAnalytics()
        {
            try
            {
                var totalCount = await _transactionService.GetTotalTransactionsCountAsync();
                var totalRevenue = await _transactionService.GetTotalRevenueAsync();
                var paidRevenue = await _transactionService.GetPaidRevenueAsync();
                var unpaidRevenue = await _transactionService.GetUnpaidRevenueAsync();
                var recentTransactions = await _transactionService.GetRecentTransactionsAsync(7);
                var monthlyRevenue = await _transactionService.GetMonthlyRevenueAsync(12);
                var statusCounts = await _transactionService.GetTransactionCountByStatusAsync();
                var revenueByService = await _transactionService.GetRevenueByServiceTypeAsync();

                return Ok(new
                {
                    TotalTransactionsCount = totalCount,
                    TotalRevenue = totalRevenue,
                    PaidRevenue = paidRevenue,
                    UnpaidRevenue = unpaidRevenue,
                    RecentTransactions = recentTransactions,
                    MonthlyRevenue = monthlyRevenue,
                    TransactionCountByStatus = statusCounts,
                    RevenueByServiceType = revenueByService
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard analytics");
                return StatusCode(500, new { message = "Error retrieving analytics", error = ex.Message });
            }
        }

        /// <summary>
        /// Get monthly revenue trend for the last N months
        /// </summary>
        [HttpGet("analytics/monthly-revenue")]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetMonthlyRevenue([FromQuery] int months = 12)
        {
            try
            {
                var monthlyRevenue = await _transactionService.GetMonthlyRevenueAsync(months);
                return Ok(monthlyRevenue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving monthly revenue");
                return StatusCode(500, new { message = "Error retrieving monthly revenue", error = ex.Message });
            }
        }

        /// <summary>
        /// Get transaction count by status
        /// </summary>
        [HttpGet("analytics/status-summary")]
        public async Task<ActionResult<Dictionary<string, int>>> GetStatusSummary()
        {
            try
            {
                var statusCounts = await _transactionService.GetTransactionCountByStatusAsync();
                return Ok(statusCounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving status summary");
                return StatusCode(500, new { message = "Error retrieving status summary", error = ex.Message });
            }
        }

        /// <summary>
        /// Get revenue by service type
        /// </summary>
        [HttpGet("analytics/revenue-by-service")]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetRevenueByService()
        {
            try
            {
                var revenue = await _transactionService.GetRevenueByServiceTypeAsync();
                return Ok(revenue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving revenue by service type");
                return StatusCode(500, new { message = "Error retrieving revenue by service type", error = ex.Message });
            }
        }

        /// <summary>
        /// Get patient's financial summary
        /// </summary>
        [HttpGet("patient/{patientId}/summary")]
        public async Task<ActionResult<object>> GetPatientFinancialSummary(int patientId)
        {
            try
            {
                var patient = await _patientService.GetPatientByIdAsync(patientId);
                if (patient == null)
                    return NotFound(new { message = $"Patient with ID {patientId} not found" });

                var total = await _transactionService.GetPatientTotalAsync(patientId);
                var unpaid = await _transactionService.GetPatientUnpaidAmountAsync(patientId);
                var paid = total - unpaid;

                return Ok(new
                {
                    PatientId = patientId,
                    PatientName = patient.Name,
                    TotalAmount = total,
                    PaidAmount = paid,
                    UnpaidAmount = unpaid
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving financial summary for patient {patientId}");
                return StatusCode(500, new { message = "Error retrieving financial summary", error = ex.Message });
            }
        }
    }
}
