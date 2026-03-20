using Microsoft.AspNetCore.Identity;

namespace Application.Role.GetUsersAssignedToRole;

public sealed record GetUsersAssignedToRoleQuery(string roleName) : IQuery<List<IdentityUser>>;
