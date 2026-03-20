using Application.Dto;

namespace Application.User.Authenticate;

public sealed record AuthenticateUserQuery(LoginRequest requestAuth) :IQuery<AuthenticateResponse>;
