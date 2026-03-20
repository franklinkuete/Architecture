namespace Core.Interfaces;

// Interface pour représenter le contexte de l'utilisateur courant, incluant les informations d'identification,
// les rôles, les revendications, et d'autres métadonnées pertinentes pour la gestion de l'authentification et de l'autorisation dans l'application.
public interface IUserContext
{
    string? UserId { get; set; }
    string? UserName { get; set; }
    string? Email { get; set; }
    IEnumerable<string> Roles { get; set; }
    Dictionary<string, string> Claims { get; set; }
    string? TenantId { get; set; }
    string? Culture { get; set; }
    string? TimeZone { get; set; }
    bool IsAuthenticated { get; set; }
    bool IsAdmin { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
}
