
namespace Application.Role.RegistrerRole;

public sealed record RegisterRoleCommand(RoleRequest requestRole) : ICommand<string>;
