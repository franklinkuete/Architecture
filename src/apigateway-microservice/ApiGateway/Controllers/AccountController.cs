using Application.Dto;
using Application.User.AssignRoleToUser;
using Application.User.Authenticate;
using Application.User.CreateUser;
using Application.User.Delete;
using Application.User.DeleteRoleFromUser;
using Application.User.RegisterUser;
using Application.User.UpdateUser;
using Core.Extensions;
using Core.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly ISender _mediatr;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ISender mediateur, ILogger<AccountController> logger)
    {
        _mediatr = mediateur;
        _logger = logger;
    }

   
    [HttpPost("AssignRolesToUser")]
    public async Task<ActionResult<ApiResponse<string>>> AssignRole(AssignRoleToUserRequest request)
    {
        var command = new AssignRoleCommand(request);

        var resultat = await _mediatr.Send(command);

        return resultat.ToApiResponse();
    }


    [HttpDelete("DeleteRolesFromUser")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteRolesFromUser(DeleteRoleFromUserRequest request)
    {
        var command = new DeleteRoleFromUserCommand(request);

        var resultat = await _mediatr.Send(command);

        return resultat.ToApiResponse();
    }

 
    /// <summary>
    /// Enregistre les roles.
    /// </summary>


    [HttpPost("RegisterUser")]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> Register(RegisterRequest request)
    {
        // creation de la commande a envoyé
        var command = new RegisterUserCommand(request);

        var resultat = await _mediatr.Send(command);

        return resultat.ToApiResponse();
    }

    [HttpPut("UpdateUser")]
    public async Task<ActionResult<ApiResponse<IdentityUser>>> UpdateUserAsync(UpdateRequest request)
    {
        // creation de la commande a envoyé
        var command = new UpdateUserCommand(request.Username, request.Email, request.Phone);

        var resultat = await _mediatr.Send(command);

        return resultat.ToApiResponse();
    }

    [HttpDelete("DeleteUser")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteUser([FromQuery] string username)
    {
        var command = new DeleteUserCommand(username);

        var resultat = await _mediatr.Send(command);

        return resultat.ToApiResponse();
    }


    [HttpPost("Token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthenticateResponse>>> Token(LoginRequest request)
    {
        var queryCommand = new AuthenticateUserQuery(request);

        var resultat = await _mediatr.Send(queryCommand);

        return resultat.ToApiResponse();
    }
}
