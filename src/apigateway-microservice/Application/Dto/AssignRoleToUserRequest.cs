namespace Application.Dto;

public sealed record AssignRoleToUserRequest(string username, string[] newRoles);

