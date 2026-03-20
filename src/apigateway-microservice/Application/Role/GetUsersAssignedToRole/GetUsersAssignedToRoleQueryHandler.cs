using Ardalis.Result;
using Microsoft.AspNetCore.Identity;

namespace Application.Role.GetUsersAssignedToRole;

public sealed class GetUsersAssignedToRoleQueryHandler : IQueryHandler<GetUsersAssignedToRoleQuery, List<IdentityUser>>
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;

    public GetUsersAssignedToRoleQueryHandler(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<Result<List<IdentityUser>>> Handle(GetUsersAssignedToRoleQuery request, CancellationToken cancellationToken)
    {
        // verifie que le role existe
        var roleExist = await _roleManager.RoleExistsAsync(request.roleName);
        if (!roleExist)
        {
            return Result.Invalid(new ValidationError("RoleNotExist", $"Le rôle {request.roleName} n'existe pas"));
        }

        //recupérer les users de ce role
        var users = await _userManager.GetUsersInRoleAsync(request.roleName);

        return Result.Success(users.ToList());
    }
}
