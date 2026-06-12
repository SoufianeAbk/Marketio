using Marketio_Shared.Entities;
using Marketio_Shared.Data;
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
                    .IgnoreQueryFilters()
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
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == customerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer {CustomerId}", customerId);
                throw new InvalidOperationException("Error retrieving customer.", ex);
            }
        }

        // Nieuwe methode toegevoegd die door CustomersViewModel.SubmitCreateCustomerAsync wordt aangeroepen
        public async Task<bool> CreateCustomerAsync(
            string firstName, string lastName,
            string email, string phone,
            string address, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var customer = new Customer
                {
                    Id = Guid.NewGuid().ToString(),
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim(),
                    Email = email.Trim(),
                    PhoneNumber = phone.Trim(),
                    Address = address.Trim(),
                    IsActive = isActive,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer '{Email}' created (Id={Id})", customer.Email, customer.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                throw new InvalidOperationException("Error creating customer.", ex);
            }
        }

        // Signatuur uitgebreid van (string, dynamic) naar 7 individuele parameters
        // Zodat CustomersViewModel.SubmitUpdateCustomerAsync correct compileert
        public async Task<bool> UpdateCustomerAsync(
            string customerId,
            string firstName, string lastName,
            string email, string phone,
            string address, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(customerId)) return false;

            try
            {
                var customer = await _context.Customers
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.Id == customerId);
                if (customer == null) return false;

                customer.FirstName = firstName.Trim();
                customer.LastName = lastName.Trim();
                customer.Email = email.Trim();
                customer.PhoneNumber = phone.Trim();
                customer.Address = address.Trim();
                customer.IsActive = isActive;

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
                var customer = await _context.Customers
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.Id == customerId);
                if (customer == null) return false;

                // Soft-delete: IsActive = false
                customer.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogWarning("Customer {CustomerId} soft-deleted", customerId);
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
                    .IgnoreQueryFilters()
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