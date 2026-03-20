using Ardalis.Result;
using Microsoft.AspNetCore.Identity;

namespace Application.User.AssignRoleToUser;

public sealed class AssignRoleCommandHandler : ICommandHandler<AssignRoleCommand,string>
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    public AssignRoleCommandHandler(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<Result<string>> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        // verifier si l'utilisateur existe
        var user = await _userManager.FindByNameAsync(request.request.username);

        if (user == null)
            return Result.Invalid(new ValidationError("UserNotExistFailed", $"l'utilisateur {request.request.username} n'existe pas"));

        var currentRoles = await _userManager.GetRolesAsync(user);

        try
        {
            // verifier si la liste de role existe
            foreach (var role in request.request.newRoles)
            {
                var roleExist = await _roleManager.RoleExistsAsync(role);

                if (!roleExist)
                {
                    return Result.Invalid(new ValidationError("RoleNotExist", $"Le rôle {role} n'existe pas"));
                }
                else
                {
                    var r = currentRoles.FirstOrDefault(c => c == role);
                    if (r == null)
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return Result.Invalid(new ValidationError("AssignRoleError", ex.Message));
        }

        return Result.Success($"Le(s) rôle(s) {string.Join("; ", request.request.newRoles)} ont(a) été assignés à {request.request.username}");
    }
}
