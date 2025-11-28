using Microsoft.EntityFrameworkCore;
using MyApp.Data;
using MyApp.Models;

namespace MyApp.Services
{
    public class TransactionService
    {
        private readonly PatientDbContext _context;

        public TransactionService(PatientDbContext context)
        {
            _context = context;
        }

        // Create - Add a new transaction
        public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
        {
            // Verify patient exists
            var patientExists = await _context.Patients.AnyAsync(p => p.PatientId == transaction.PatientId);
            if (!patientExists)
                throw new InvalidOperationException($"Patient with ID {transaction.PatientId} does not exist.");

            transaction.CreatedAt = DateTime.UtcNow;
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        // Read - Get all transactions
        public async Task<List<Transaction>> GetAllTransactionsAsync()
        {
            return await _context.Transactions
                .Include(t => t.Patient)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        // Read - Get transaction by ID
        public async Task<Transaction?> GetTransactionByIdAsync(int transactionId)
        {
            return await _context.Transactions
                .Include(t => t.Patient)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        // Read - Get transactions by Patient ID
        public async Task<List<Transaction>> GetTransactionsByPatientIdAsync(int patientId)
        {
            return await _context.Transactions
                .Include(t => t.Patient)
                .Where(t => t.PatientId == patientId)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        // Read - Get transactions by Status
        public async Task<List<Transaction>> GetTransactionsByStatusAsync(string status)
        {
            return await _context.Transactions
                .Include(t => t.Patient)
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();
        }

        // Update - Update transaction information
        public async Task<Transaction?> UpdateTransactionAsync(int transactionId, Transaction updatedTransaction)
        {
            var transaction = await _context.Transactions.FindAsync(transactionId);
            if (transaction == null)
                return null;

            // Verify patient exists if it's being changed
            if (transaction.PatientId != updatedTransaction.PatientId)
            {
                var patientExists = await _context.Patients.AnyAsync(p => p.PatientId == updatedTransaction.PatientId);
                if (!patientExists)
                    throw new InvalidOperationException($"Patient with ID {updatedTransaction.PatientId} does not exist.");
            }

            transaction.PatientId = updatedTransaction.PatientId;
            transaction.ServiceType = updatedTransaction.ServiceType;
            transaction.Amount = updatedTransaction.Amount;
            transaction.TransactionDate = updatedTransaction.TransactionDate;
            transaction.Status = updatedTransaction.Status;
            transaction.UpdatedAt = DateTime.UtcNow;

            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        // Delete - Delete a transaction
        public async Task<bool> DeleteTransactionAsync(int transactionId)
        {
            var transaction = await _context.Transactions.FindAsync(transactionId);
            if (transaction == null)
                return false;

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
            return true;
        }

        // Read - Get transaction summary by status
        public async Task<Dictionary<string, decimal>> GetTransactionSummaryByStatusAsync()
        {
            var summary = await _context.Transactions
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Total = g.Sum(t => t.Amount) })
                .ToListAsync();

            return summary.ToDictionary(s => s.Status, s => s.Total);
        }

        // Read - Get total amount for a patient
        public async Task<decimal> GetPatientTotalAsync(int patientId)
        {
            return await _context.Transactions
                .Where(t => t.PatientId == patientId)
                .SumAsync(t => t.Amount);
        }

        // Read - Get unpaid amount for a patient
        public async Task<decimal> GetPatientUnpaidAmountAsync(int patientId)
        {
            return await _context.Transactions
                .Where(t => t.PatientId == patientId && t.Status == "Unpaid")
                .SumAsync(t => t.Amount);
        }

        // Dashboard - Get total transactions count
        public async Task<int> GetTotalTransactionsCountAsync()
        {
            return await _context.Transactions.CountAsync();
        }

        // Dashboard - Get total revenue (all transactions)
        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.Transactions.SumAsync(t => t.Amount);
        }

        // Dashboard - Get paid revenue
        public async Task<decimal> GetPaidRevenueAsync()
        {
            return await _context.Transactions
                .Where(t => t.Status == "Paid")
                .SumAsync(t => t.Amount);
        }

        // Dashboard - Get unpaid revenue
        public async Task<decimal> GetUnpaidRevenueAsync()
        {
            return await _context.Transactions
                .Where(t => t.Status == "Unpaid")
                .SumAsync(t => t.Amount);
        }

        // Dashboard - Get recent transactions (last N days)
        public async Task<List<Transaction>> GetRecentTransactionsAsync(int days = 7)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            return await _context.Transactions
                .Include(t => t.Patient)
                .Where(t => t.TransactionDate >= startDate)
                .OrderByDescending(t => t.TransactionDate)
                .Take(10)
                .ToListAsync();
        }

        // Dashboard - Get monthly revenue trend for the last N months
        public async Task<Dictionary<string, decimal>> GetMonthlyRevenueAsync(int months = 12)
        {
            if (months <= 0) months = 12;

            // Determine the first month to include (start of month), include current month as the last
            var now = DateTime.UtcNow;
            var startMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-(months - 1));
            var endMonth = new DateTime(now.Year, now.Month, 1);

            // Fetch transactions that fall within the inclusive month range
            var transactions = await _context.Transactions
                .Where(t => t.TransactionDate >= startMonth && t.TransactionDate < endMonth.AddMonths(1))
                .ToListAsync();

            // Group on client side by Year+Month
            var monthlyData = transactions
                .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
                .Select(g => new
                {
                    YearMonth = new DateTime(g.Key.Year, g.Key.Month, 1),
                    Total = g.Sum(t => t.Amount)
                })
                .ToDictionary(x => x.YearMonth, x => x.Total);

            // Build result for exactly `months` months in order
            var result = new Dictionary<string, decimal>();
            for (var i = 0; i < months; i++)
            {
                var monthDate = startMonth.AddMonths(i);
                var key = monthDate.ToString("MMM yyyy");
                monthlyData.TryGetValue(monthDate, out var total);
                result[key] = total;
            }

            return result;
        }

        // Dashboard - Get transaction count by status
        public async Task<Dictionary<string, int>> GetTransactionCountByStatusAsync()
        {
            var counts = await _context.Transactions
                .GroupBy(t => t.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return counts.ToDictionary(c => c.Status, c => c.Count);
        }

        // Dashboard - Get revenue by service type
        public async Task<Dictionary<string, decimal>> GetRevenueByServiceTypeAsync()
        {
            var revenue = await _context.Transactions
                .GroupBy(t => t.ServiceType)
                .Select(g => new { ServiceType = g.Key, Total = g.Sum(t => t.Amount) })
                .OrderByDescending(x => x.Total)
                .ToListAsync();

            return revenue.ToDictionary(r => r.ServiceType, r => r.Total);
        }
    }
}
