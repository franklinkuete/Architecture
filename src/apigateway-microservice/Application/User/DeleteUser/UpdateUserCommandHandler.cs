
using Application.User.Delete;
using Ardalis.Result;
using Microsoft.AspNetCore.Identity;

namespace Application.User.UpdateUser;

public sealed class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand,string>
{

    private readonly UserManager<IdentityUser> _userManager;

    public DeleteUserCommandHandler(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<string>> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        // Get user
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null)
            return Result.Invalid(new ValidationError("UserNotExistFailed", $"l'utilisateur {request.Username} n'existe pas"));

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return Result.Invalid(new ValidationError("UserDeleteFailed", errors));
        }

        return Result.Success($"L'utilisateur {request.Username} a été supprimé");
    }
}
