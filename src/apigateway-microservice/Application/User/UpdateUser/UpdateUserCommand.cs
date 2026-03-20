using Application.Dto;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.User.UpdateUser
{
    public sealed record UpdateUserCommand(string Username,string Email, string Phone) : ICommand<IdentityUser>;
}
