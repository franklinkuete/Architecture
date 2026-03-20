using Ardalis.Result;
using Microsoft.AspNetCore.Identity;


namespace Application.Role.GetAllRoles;

public sealed class GetAllRolesQueryHandler : IQueryHandler<GetAllRoleQuery, string?[]>
{
    private readonly RoleManager<IdentityRole> _roleManager;
    public GetAllRolesQueryHandler(RoleManager<IdentityRole> roleManager)
    {
        _roleManager=roleManager;
    }
    public async Task<Result<string?[]>> Handle(GetAllRoleQuery request, CancellationToken cancellationToken)
    {

        var allRoles = _roleManager.Roles.Select(c => c.Name).ToArray();

        return Result.Success(allRoles);

    }
}
