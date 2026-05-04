using Marketio_Shared.Entities;
using Marketio_WPF.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Marketio_WPF.Services
{
    internal class CustomerService
    {
        private readonly MarketioDbContext _context;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(MarketioDbContext context, ILogger<CustomerService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<dynamic>> GetAllCustomersAsync()
        {
            try
            {
                var customers = await _context.Customers
                    .OrderBy(c => c.LastName)
                    .ThenBy(c => c.FirstName)
                    .AsNoTracking()
                    .ToListAsync();

                return customers.Cast<dynamic>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all customers");
                throw new InvalidOperationException("Error retrieving customers.", ex);
            }
        }

        public async Task<dynamic?> GetCustomerByIdAsync(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId)) return null;

            try
            {
                return await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer {CustomerId}", customerId);
                throw new InvalidOperationException("Error retrieving customer.", ex);
            }
        }

        public async Task<bool> UpdateCustomerAsync(string customerId, dynamic customerData)
        {
            if (string.IsNullOrWhiteSpace(customerId) || customerData == null) return false;

            try
            {
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null) return false;

                customer.FirstName = (string)customerData.FirstName;
                customer.LastName = (string)customerData.LastName;
                customer.Email = (string)customerData.Email;
                customer.PhoneNumber = (string)customerData.PhoneNumber;
                customer.Address = (string)customerData.Address;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer {CustomerId} updated", customerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer {CustomerId}", customerId);
                throw new InvalidOperationException("Error updating customer.", ex);
            }
        }

        public async Task<bool> DeleteCustomerAsync(string customerId)
        {
            if (string.IsNullOrWhiteSpace(customerId)) return false;

            try
            {
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null) return false;

                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();

                _logger.LogWarning("Customer {CustomerId} deleted", customerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting customer {CustomerId}", customerId);
                throw new InvalidOperationException("Error deleting customer.", ex);
            }
        }

        public async Task<List<dynamic>> SearchCustomersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return new List<dynamic>();

            try
            {
                var term = searchTerm.Trim().ToLower();

                var customers = await _context.Customers
                    .Where(c => c.FirstName.ToLower().Contains(term) ||
                                c.LastName.ToLower().Contains(term) ||
                                c.Email.ToLower().Contains(term))
                    .OrderBy(c => c.LastName)
                    .AsNoTracking()
                    .ToListAsync();

                return customers.Cast<dynamic>().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching customers: {SearchTerm}", searchTerm);
                throw new InvalidOperationException("Error searching customers.", ex);
            }
        }
    }
}