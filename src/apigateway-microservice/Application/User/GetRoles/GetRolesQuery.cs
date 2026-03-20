using Ardalis.Result;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.User.GetRoles
{
    public sealed record GetRolesQuery(string username) : IQuery<string[]>;
   
}
