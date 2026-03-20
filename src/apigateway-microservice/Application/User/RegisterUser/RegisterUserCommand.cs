using Application.Dto;
using Application.User.RegisterUser;

namespace Application.User.CreateUser;

public sealed record RegisterUserCommand(RegisterRequest requestCommande) : ICommand<RegisterResponse>;
