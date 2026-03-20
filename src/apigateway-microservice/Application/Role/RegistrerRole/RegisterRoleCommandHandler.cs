using Ardalis.Result;
using Microsoft.AspNetCore.Identity;

namespace Application.Role.RegistrerRole;

public sealed class RegisterRoleCommandHandler : ICommandHandler<RegisterRoleCommand,string>
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public RegisterRoleCommandHandler(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<Result<string>> Handle(RegisterRoleCommand request, CancellationToken cancellationToken)
    {
        if (await _roleManager.RoleExistsAsync(request.requestRole.Role))
        {
            return Result.Invalid(new ValidationError("RoleAlreadyExists", $"Le rôle {request.requestRole.Role} existe déjà."));
        }
        var role = new IdentityRole
        {
            Name = request.requestRole.Role,
            NormalizedName = request.requestRole.Role.ToUpperInvariant()
        };
        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            return Result.Invalid(new ValidationError("Error", $"Il y a eu un probleme lors de la creation du role {request.requestRole.Role}"));
        }

        return Result.Success($"Le rôle {request.requestRole.Role} a été crée avec succès");
    }
}
