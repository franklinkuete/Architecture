using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Dto;

public sealed record DeleteRoleFromUserRequest(string username, string[] newRoles,bool deleteAllRoles);

