

namespace Application.User.Delete;

public sealed record DeleteUserCommand(string Username) : ICommand<string>;
