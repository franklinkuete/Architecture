

namespace Application.Role.DeleteRole;

public sealed record DeleteRoleCommand(string roleName):ICommand<string>;

