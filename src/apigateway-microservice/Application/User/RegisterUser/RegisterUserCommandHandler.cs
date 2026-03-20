using Application.User.CreateUser;
using Ardalis.Result;
using MediatR;
using Microsoft.AspNetCore.Identity;


namespace Application.User.RegisterUser
{
    public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, RegisterResponse>
    {

        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterUserCommandHandler(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<Result<RegisterResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByNameAsync(request.requestCommande.Username);

            if (user != null)
                return Result.Invalid(new ValidationError("UserExistFailed", $"l'utilisateur {request.requestCommande.Username} existe déjà"));

            if (string.IsNullOrEmpty(request.requestCommande.Role) || !await _roleManager.RoleExistsAsync(request.requestCommande.Role))
            {
                return Result.Invalid(new ValidationError("RoleExistFailed", $"Le rôle {request.requestCommande.Role} n'existe pas"));
            }
            var newUser = new IdentityUser()
            {
                UserName = request.requestCommande.Username,
                Email = request.requestCommande.Email,
            };

            // create user
            IdentityResult identityResult = await _userManager.CreateAsync(newUser, request.requestCommande.Password);

            if (identityResult.Succeeded)
            {
                //Association au role

                var response = await _userManager.AddToRoleAsync(newUser, request.requestCommande.Role);

                if (response.Succeeded)
                {
                    return Result.Success( new RegisterResponse(true, request.requestCommande.Username));
                }
            }
            else
            {
                var errors = string.Join("; ", identityResult.Errors.Select(e => e.Description));
                return Result.Invalid(new ValidationError("UserCreationFailed", errors));
            }

            return Result.Success(new RegisterResponse(true, request.requestCommande.Username));
        }
    }

}
