using Application.Dto;

namespace Application.User.AssignRoleToUser
{
    public sealed record AssignRoleCommand(AssignRoleToUserRequest request) : ICommand<string>;
}
