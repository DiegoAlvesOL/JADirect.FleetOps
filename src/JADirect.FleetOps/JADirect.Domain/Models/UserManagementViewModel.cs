using JADirect.Domain.Enums;

namespace JADirect.Domain.Models;


/// <summary>
/// Modelo para a tela de gerenciamento de usuário.
/// </summary>
public class UserManagementViewModel
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRoles Role { get; set; }
    public UserStatus Status { get; set; }
    
    
    // Helpers para a View (Mantendo a lógica fora do HTML)
    public string DisplayName => $"{FirstName} {Surname}";
    public bool IsActive => Status == UserStatus.Active;
    public string StatusLabel => IsActive ? "Active" : "Deactiveted";
    public string StatusClasses => IsActive ?
        "bg-green-100 text-green-800 border-green-200"
        : "bg-red-100 text-red-800 border-red-800";
}