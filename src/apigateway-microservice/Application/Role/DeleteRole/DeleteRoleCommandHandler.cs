
using Ardalis.Result;
using Microsoft.AspNetCore.Identity;

namespace Application.Role.DeleteRole;

public sealed class DeleteRoleCommandHandler : ICommandHandler<DeleteRoleCommand,string>
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;
    public DeleteRoleCommandHandler(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<Result<string>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        // verifie si le role existe
        var roleExist = await _roleManager.RoleExistsAsync(request.roleName);
        if (!roleExist)
        {
            return Result.Invalid(new ValidationError("RoleNotExist", $"Le rôle {request.roleName} n'existe pas"));
        }

        // verifie que le role n'est pas assigné
        var users = await _userManager.GetUsersInRoleAsync(request.roleName);
        if (users.Any())
        {
            return Result.Invalid(new ValidationError("RoleUsersExist", $"Des utilisateurs sont associés au rôle {request.roleName}"));
        }

        // suppression du role
        var role = await _roleManager.FindByNameAsync(request.roleName);

        var resultat = await _roleManager.DeleteAsync(role!);
        if (!resultat.Succeeded)
        {
            return Result.Invalid(new ValidationError("RoleDeleteFailed", $"Une erreur est survenue lors de la suppression du rôle {request.roleName}"));
        }

        return Result.Success($"Le rôle {request.roleName} a été supprimé.");
    }
}
