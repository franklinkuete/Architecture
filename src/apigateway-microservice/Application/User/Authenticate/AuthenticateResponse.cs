using Microsoft.AspNetCore.Identity;
namespace Application.User.Authenticate;

public sealed record AuthenticateResponse( IdentityUser user,string Token);