using System;
using System.Collections.Generic;
using System.Text;

namespace Application.User.RegisterUser
{
  public sealed record RegisterResponse(bool IsRegistered, string Username);
}
