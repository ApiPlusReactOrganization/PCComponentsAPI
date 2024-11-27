using Api.Dtos;
using Api.Modules.Errors;
using Application.Common.Interfaces.Queries;
using Application.Orders.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("orders")]
// [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
// [Authorize(Roles = AuthSettings.UserRole)]
[ApiController]
public class OrdersController(ISender sender, IOrderQueries orderQueries) : ControllerBase
{
    [HttpGet("get-all")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAll(CancellationToken cancellationToken)
    {
        var entities = await orderQueries.GetAll(cancellationToken);

        return entities.Select(OrderDto.FromDomainModel).ToList();
    }

    [HttpPost("create")]
    public async Task<ActionResult<OrderDto>> Create([FromBody] OrderDto request,
        CancellationToken cancellationToken)
    {
        var input = new CreateOrderCommand()
        {
            UserId = request.UserId!.Value,
            Status = request.Status,
            DeliveryAddress = request.DeliveryAddress,
        };
    
        var result = await sender.Send(input, cancellationToken);
    
        return result.Match<ActionResult<OrderDto>>(
            order => OrderDto.FromDomainModel(order),
            e => e.ToObjectResult());
    }
}