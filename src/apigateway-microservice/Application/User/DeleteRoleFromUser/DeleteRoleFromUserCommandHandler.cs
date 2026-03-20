using Ardalis.Result;
using Microsoft.AspNetCore.Identity;

namespace Application.User.DeleteRoleFromUser;

public sealed class DeleteRoleFromUserCommandHandler : ICommandHandler<DeleteRoleFromUserCommand,string>
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DeleteRoleFromUserCommandHandler(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<Result<string>> Handle(DeleteRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        // verifier si l'utilisateur existe
        var user = await _userManager.FindByNameAsync(request.request.username);

        if (user == null)
            return Result.Invalid(new ValidationError("UserNotExistFailed", $"l'utilisateur {request.request.username} n'existe pas"));

        if (request.request.deleteAllRoles)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Supprimer tous les rôles existants
            var resultDeleteAllRoles = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!resultDeleteAllRoles.Succeeded)
            {
                var errors = string.Join("; ", resultDeleteAllRoles.Errors.Select(e => e.Description));
                return Result.Invalid(new ValidationError("RoleRemoving", errors));
            }

            return Result.Success($"Tout les rôles ont été déassignés à l'utilisateur {request.request.username}");

        }
        // verifier si la liste de role existe
        foreach (var role in request.request.newRoles)
        {
            var roleExist = await _roleManager.RoleExistsAsync(role);

            if (!roleExist)
            {
                return Result.Invalid(new ValidationError("RoleNotExist", $"Le rôle {role} n'existe pas"));
            }
        }

        // Supprimer tous les rôles 
        var result = await _userManager.RemoveFromRolesAsync(user, request.request.newRoles);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Invalid(new ValidationError("RoleUnassigning", errors));
        }

        return Result.Success($"Les rôles {string.Join("; ", request.request.newRoles)} ont été déassignés à l'utilisateur {request.request.username}");
    }
}
