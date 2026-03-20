using CommandeApi.Application.Commande;
using CommandeApi.Application.Commande.AddCommande;
using CommandeApi.Application.Commande.AddCommandeOnly;
using CommandeApi.Application.Commande.AddProductItems;
using CommandeApi.Application.Commande.CancelCommande;
using CommandeApi.Application.Commande.DeleteCommande;
using CommandeApi.Application.Commande.DeleteProductItems;
using CommandeApi.Application.Commande.GetAllCommandes;
using CommandeApi.Application.Commande.GetAllCommandesByClientId;
using CommandeApi.Application.Commande.GetCommandeById;
using Core.Extensions;
using Core.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CommandeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommandeController : ControllerBase
    {
        private readonly ISender _mediatr;

        public CommandeController(ISender mediatr)
        {
            _mediatr = mediatr;
        }

        [HttpPost("AddCommande")]
        public async Task<ActionResult<ApiResponse<CommandeResponse>>> AddCommande([FromBody] CommandeRequest commande)
        {
            var result = await _mediatr.Send(new AddCommandeCommand(commande));
            return result.ToApiResponse();
        }

        [HttpPost("AddCommandeOnly")]
        public async Task<ActionResult<ApiResponse<CommandeResponse>>> AddCommandeOnly([FromBody] CommandeRequest commande)
        {
            var result = await _mediatr.Send(new AddCommandeCommandOnly(commande));
            return result.ToApiResponse();
        }

        [HttpPost("AddProductItems")]
        public async Task<ActionResult<ApiResponse<CommandeResponse>>> AddCommandeItems([FromBody] AddProductItemsRequest commandeItems)
        {
            var result = await _mediatr.Send(new AddProductItemsCommand(commandeItems));
            return result.ToApiResponse();
        }

        [HttpDelete("DeleteProductItems")]
        public async Task<ActionResult<ApiResponse<CommandeResponse>>> DeleteProductItems([FromBody] List<int> commandeProductIds)
        {
            var result = await _mediatr.Send(new DeleteProductItemsCommande(commandeProductIds));
            return result.ToApiResponse();
        }

        [HttpDelete("DeleteCommande")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteCommande(int CommandId)
        {
            var result = await _mediatr.Send(new DeleteCommandeCommand(CommandId));
            return result.ToApiResponse();
        }

        [HttpGet("GetAllCommandesByClientId")]
        public async Task<ActionResult<ApiResponse<List<CommandeResponse>>>> GetAllCommandesBlyClientId(string ClientId, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var result = await _mediatr.Send(new GetAllCommandesQueryByClient(ClientId, pageIndex, pageSize));
            return result.ToApiResponse();
        }

        [HttpGet("GetCommandesById")]
        public async Task<ActionResult<ApiResponse<CommandeResponse>>> GetCommandesById(int CommandeId)
        {
            var result = await _mediatr.Send(new GetCommandeByIdQuery(CommandeId));
            return result.ToApiResponse();
        }


        [HttpGet("GetAllCommandes")]
        public async Task<ActionResult<ApiResponse<List<CommandeResponse>>>> GetAllCommandes(int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var result = await _mediatr.Send(new GetAllCommandesQuery(pageIndex, pageSize));
            return result.ToApiResponse();
        }

        [HttpPost("CancelCommande")]
        public async Task<ActionResult<ApiResponse<CommandeResponse>>> CancelCommande(int CommandeId)
        {
            var result = await _mediatr.Send(new CancelCommande(CommandeId));
            return result.ToApiResponse();
        }

    }
}
