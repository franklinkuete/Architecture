namespace Application.Dto;

public sealed record RegisterRequest(string Username,string Email, string Password,string Role);
