using ClientApi.Application.Client;
using ClientApi.Application.Client.AddClient;
using ClientApi.Application.Client.DeleteClient;
using ClientApi.Application.Client.GetAll;
using ClientApi.Application.Client.GetById;
using Core.Extensions;
using Core.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ClientApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClientController : ControllerBase
{
    private readonly ISender _mediatr;

    public ClientController(ISender mediatr)
    {
        _mediatr = mediatr;
    }

    // GET api/client/{id}
    [HttpGet("GetClientById")]
    public async Task<ActionResult<ApiResponse<ClientResponse>>> GetClientById(int id)
    {
        var result = await _mediatr.Send(new GetByIdQuery(id));
 
        return result.ToApiResponse();
    }

    // GET api/client
    [HttpGet("GetAllClient")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ClientResponse>>>> GetAllClients(int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var result = await _mediatr.Send(new GetAllClientQuery(pageIndex,pageSize));
        return result.ToApiResponse();
    }

    // POST api/client
    [HttpPost("AddClient")]
    public async Task<ActionResult<ApiResponse<ClientResponse>>> AddClient([FromBody] ClientRequest client)
    {
        var result = await _mediatr.Send(new AddClientCommand(client));
        return result.ToApiResponse();
    }

    // DELETE api/client/{id}
    [HttpDelete("RemoveClient")]
    public async Task<ActionResult<ApiResponse<bool>>> RemoveClient(int id)
    {
        var result = await _mediatr.Send(new DeleteClientCommand(id));
        return result.ToApiResponse();
    }

}
