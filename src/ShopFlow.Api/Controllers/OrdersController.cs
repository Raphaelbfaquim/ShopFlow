using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShopFlow.Api.Extensions;
using ShopFlow.Application.Orders.Commands;
using ShopFlow.Application.Orders.Queries;

namespace ShopFlow.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[EnableRateLimiting("api")]
[Route("api/v{version:apiVersion}/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Place([FromBody] PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value, version = "1.0" }, result.Value)
            : this.ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOrderByIdQuery(id), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("{id:guid}/pay")]
    public async Task<IActionResult> Pay(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new PayOrderCommand(id), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CancelOrderCommand(id), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("dashboard")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOrderDashboardQuery(), cancellationToken);
        return this.ToActionResult(result);
    }
}
