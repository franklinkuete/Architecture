

using Core.Interfaces;

public class UserContext : IUserContext
{
    public string? UserId { get; set; }
    public string? UserName { get; set; } 
    public string? Email { get; set; }
    public IEnumerable<string> Roles { get; set; }= [];
    public Dictionary<string, string> Claims { get; set; }= [];
    public string? Culture { get; set; }= "fr-FR";


    // Métadonnées
    public string? TenantId { get; set; } 
    public string? TimeZone { get; set; } 

    // Contexte technique
    public string? IpAddress { get; set; } 
    public string? UserAgent { get; set; } 
    public string? CorrelationId { get; set; } 

    // Contrôles rapides
    public bool IsAuthenticated { get; set; }
    public bool IsAdmin { get; set; }

}



