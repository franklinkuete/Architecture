using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ardalis.Result;
using Infrastructure;

namespace Application.User.Authenticate;

public  class AuthenticateUserQueryHandler : IQueryHandler<AuthenticateUserQuery, AuthenticateResponse>
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthenticateUserQueryHandler(UserManager<IdentityUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<Result<AuthenticateResponse>> Handle(AuthenticateUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.requestAuth.Username);

        if (user != null)
        {
            var checkValidUser = await _userManager.CheckPasswordAsync(user!, request.requestAuth.Password);

            if (checkValidUser)
            {
                var token = await GenerateJwtToken(user);
                var reponse = new AuthenticateResponse(user, token);

                return Result.Success(reponse);
            }
        }
        return Result.Invalid(new ValidationError("AuthenticationFailed", "Invalid username or password"));
    }

    private async Task<string> GenerateJwtToken(IdentityUser user)
    {
        var authClaims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.UserName!),
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

        // Email
        if (!string.IsNullOrEmpty(user.Email))
        {
            authClaims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        // Rôles
        var userRoles = await _userManager.GetRolesAsync(user);
        foreach (var role in userRoles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        bool isAdmin = userRoles.Any(c =>c == Constante.Role.ADMINISTRATOR ||c == Constante.Role.SUPERADMIN);

        // 🔹 Claims personnalisés (issus de UserContext ou ta logique métier)
        authClaims.Add(new Claim("TenantId", $"Franklin-Tenant-{Guid.NewGuid().ToString()}"));          // Multi-tenant
        authClaims.Add(new Claim("Culture", "fr-FR"));                 // Langue préférée
        authClaims.Add(new Claim("Timezone", "Europe/Paris"));         // Fuseau horaire   // Avatar
        authClaims.Add(new Claim("IsAdmin", isAdmin.ToString()));

        var authSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["JwtSettings:PrivateKey"]!)
        );

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            expires: DateTime.Now.AddDays(1),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}


