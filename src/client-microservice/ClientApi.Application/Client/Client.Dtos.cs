

namespace ClientApi.Application.Client;

public class ClientRequest
{
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
public class ClientResponse : ClientRequest
{
    public int Id { get; set; }
   
}
public static class ClientConst
{
    public const string GetAllCacheKeyPrefix = "get-allclient";
    public const string ItemCacheKeyPrefix = "client";
}