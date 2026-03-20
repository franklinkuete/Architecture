using Application.Role.DeleteRole;
using Application.Role.GetAllRoles;
using Application.Role.GetUsersAssignedToRole;
using Application.Role.RegistrerRole;
using Application.User.GetRoles;
using Core.Extensions;
using Core.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly ISender _mediatr;
        private readonly ILogger<RolesController> _logger;
        public RolesController(ISender mediateur, ILogger<RolesController> logger)
        {
            _mediatr = mediateur;
            _logger = logger;
        }

        [HttpGet("GetAllRoles")]
        public async Task<ActionResult<ApiResponse<string[]>>> GetAllRolesFromUser()
        {
            var query = new GetAllRoleQuery();

            var resultat = await _mediatr.Send(query);

            return resultat.ToApiResponse();
        }


        [HttpPost("RegisterRole")]
        public async Task<ActionResult<ApiResponse<string>>> EnregistrerRole(RoleRequest request)
        {
            // creation de la commande a envoyé
            var command = new RegisterRoleCommand(request);

            var resultat = await _mediatr.Send(command);

            return resultat.ToApiResponse();

        }

        [HttpGet("GetRolesByUser")]
        public async Task<ActionResult<ApiResponse<string[]>>> GetRoles([FromQuery] string username)
        {
            var command = new GetRolesQuery(username);

            var resultat = await _mediatr.Send(command);

            return resultat.ToApiResponse();
        }

        [HttpDelete("DeleteRole")]
        public async Task<ActionResult<ApiResponse<string>>> DeleteRole([FromQuery] string roleName)
        {
            // creation de la commande a envoyé
            var command = new DeleteRoleCommand(roleName);

            var resultat = await _mediatr.Send(command);

            return resultat.ToApiResponse();
        }

        [HttpGet("GetUsersAssignedToRole")]
        public async Task<ActionResult<ApiResponse<List<IdentityUser>>>> GetUsersAssignedToRoleAsync([FromQuery] string roleName)
        {
            var Query = new GetUsersAssignedToRoleQuery(roleName);

            var resultat = await _mediatr.Send(Query);

            return resultat.ToApiResponse();
        }

    }
}
