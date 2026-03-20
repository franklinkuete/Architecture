using Core.Extensions;
using Core.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductApi.Application.Categorie.AddCategorie;
using ProductApi.Application.Categorie.DeleteCategorie;
using ProductApi.Application.Categorie.GetAllCategorie;
using ProductApi.Application.Product;
using ProductApi.Application.Product.AddProduct;
using ProductApi.Application.Product.DeleteProduct;

namespace ProductApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategorieController : ControllerBase
    {
        private readonly ISender _mediatr;
        public CategorieController(ISender mediatr)
        {
            _mediatr = mediatr;
        }
        [HttpPost("AddCategorie")]
        public async Task<ActionResult<ApiResponse<CategorieResponse>>> AddCategorie(string CategorieName)
        {
            var result = await _mediatr.Send(new AddCategorieCommand(CategorieName));

            return result.ToApiResponse();
        }

        [HttpGet("GetAllCategorie")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CategorieResponse>>>> GetAllCategorie()
        {
            var result = await _mediatr.Send(new GetAllCategorieCommand());

            return result.ToApiResponse();
        }

        [HttpDelete("DeleteCategorieById")]
        public async Task<ActionResult<ApiResponse<bool?>>> DeleteById(int Id)
        {
            var result = await _mediatr.Send(new DeleteCategorieCommand(Id));

            return result.ToApiResponse();
        }
    }
}
