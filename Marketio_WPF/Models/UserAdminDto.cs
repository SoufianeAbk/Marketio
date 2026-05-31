namespace Marketio_WPF.Models
{
    /// <summary>
    /// Data Transfer Object voor de Admin-gebruikerslijst.
    /// Vervangt het anonieme type uit UserManagementService zodat
    /// WPF-bindings (ook TwoWay op DataGridCheckBoxColumn) correct werken.
    /// </summary>
    public class UserAdminDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public string Roles { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
    }
}
