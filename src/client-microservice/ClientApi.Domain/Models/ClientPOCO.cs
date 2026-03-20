namespace ClientApi.Domain.Entities;

public class ClientPOCO
{
    public int Id { get; set; }
    public string Lastname { get; set; } = string.Empty;

    public string Firstname { get; set; } = string.Empty;
   // public string Fullname => $"{Firstname} {Lastname}";

    public string Email { get; set; } = string.Empty;

    public string Telephone { get; set; } = string.Empty;

    public string Ville { get; set; } = string.Empty;

    public string Codepostal { get; set; } = string.Empty;

    public DateOnly? Datenaissance { get; set; } = null;

    public DateOnly? Datecreation { get; set; } = null;

    public DateOnly? Datemodification { get; set; } = null;

}
