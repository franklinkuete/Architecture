namespace ClientApi.Infrastructure.Entities;

public partial class Client
{
    public int Id { get; set; }

    public string? Lastname { get; set; }

    public string? Firstname { get; set; }

    public string? Email { get; set; }

    public string? Telephone { get; set; }

    public string? Ville { get; set; }

    public string? Codepostal { get; set; }

    public DateOnly? Datenaissance { get; set; }

    public DateOnly? Datecreation { get; set; }

    public DateOnly? Datemodification { get; set; }
}
