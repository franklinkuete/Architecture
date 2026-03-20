
using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.Identity;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Application.User.UpdateUser;

public sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, IdentityUser>
{

    private readonly UserManager<IdentityUser> _userManager;

    public UpdateUserCommandHandler(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<IdentityUser>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {

        // Get user
        var user = await _userManager.FindByNameAsync(request.Username);

        if (user == null)
            return Result.Invalid(new ValidationError("UserNotExistFailed", $"l'utilisateur {request.Username} n'existe pas"));

        // Update user
        user.Email = request.Email;
        user.PhoneNumber = request.Phone;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Invalid(new ValidationError("UserNotUpdated", errors));
        }
        var updatedUser = await _userManager.FindByNameAsync(request.Username);

        return Result.Success(updatedUser!);
    }
}
