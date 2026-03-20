using Core.Extensions;
using Core.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ProductApi.Application.Product;
using ProductApi.Application.Product.AddProduct;
using ProductApi.Application.Product.DeleteProduct;
using ProductApi.Application.Product.GetAllProduct;
using ProductApi.Application.Product.GetProductById;
using ProductApi.Application.Product.UpdateProduct;

namespace ProductApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    private readonly ISender _mediatr;

    public ProductController(ISender mediatr)
    {
        _mediatr = mediatr;
    }

    [HttpPost("AddProduct")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> AddProduct([FromBody] AddProductRequest request)
    {
        var result = await _mediatr.Send(new AddProductCommand(request));

        return result.ToApiResponse();
    }

    [HttpGet("GetAllProduct")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductResponse>>>> GetAllProduct(int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var result = await _mediatr.Send(new GetAllProductCommand(pageIndex, pageSize));

        return result.ToApiResponse();
    }

    [HttpGet("GetProductById")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> GetProductById(Guid productId)
    {
        var result = await _mediatr.Send(new GetProductByIdQuery(productId));

        return result.ToApiResponse();
    }

    [HttpDelete("DeleteById")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteById(Guid Id)
    {
        var result = await _mediatr.Send(new DeleteProductCommand(Id));

        return result.ToApiResponse();
    }

    [HttpPut("UpdateProduct")]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> UpdateProduct([FromBody] UpdateProductRequest request)
    {
        var result = await _mediatr.Send(new UpdateProductCommand(request));

        return result.ToApiResponse();
    }

}
