using Application.Dto;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.User.DeleteRoleFromUser;

public sealed record DeleteRoleFromUserCommand(DeleteRoleFromUserRequest request) : ICommand<string>;
