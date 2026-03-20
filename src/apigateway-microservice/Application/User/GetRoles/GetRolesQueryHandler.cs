using Ardalis.Result;
using Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Application.User.GetRoles;

public sealed class GetRolesQueryHandler : IQueryHandler<GetRolesQuery, string[]>
{
    private readonly UserManager<IdentityUser> _userManager;

    // test du UserContext
    private readonly IUserContext _currentUser;
    public GetRolesQueryHandler(UserManager<IdentityUser> userManager, IUserContext currentUser)
    {
        _userManager = userManager;
        _currentUser = currentUser;
    }

    public async Task<Result<string[]>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        //test pour reccupere les roles de l'utilisateur courant pour verifier les droits
        var testRoleFromContext =_currentUser.Roles!;

        var user = await _userManager.FindByNameAsync(request.username);

        if (user == null)
            return Result.Invalid(new ValidationError("UserNotExistFailed", $"l'utilisateur {request.username} n'existe pas"));

        
        var resultat = await _userManager.GetRolesAsync(user);
        return Result.Success(resultat.ToArray());
    }
}
