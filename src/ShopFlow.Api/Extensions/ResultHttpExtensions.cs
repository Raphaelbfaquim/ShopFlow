using Microsoft.AspNetCore.Mvc;
using ShopFlow.BuildingBlocks.Results;

namespace ShopFlow.Api.Extensions;

public static class ResultHttpExtensions
{
    public static IActionResult ToActionResult(this ControllerBase controller, Result result)
    {
        return result.IsSuccess ? controller.NoContent() : controller.ToErrorResult(result.Error);
    }

    public static IActionResult ToActionResult<T>(this ControllerBase controller, Result<T> result)
    {
        return result.IsSuccess ? controller.Ok(result.Value) : controller.ToErrorResult(result.Error);
    }

    public static IActionResult ToCreatedResult<T>(this ControllerBase controller, Result<T> result, string actionName, object routeValues)
    {
        return result.IsSuccess
            ? controller.CreatedAtAction(actionName, routeValues, result.Value)
            : controller.ToErrorResult(result.Error);
    }

    private static IActionResult ToErrorResult(this ControllerBase controller, Error error)
    {
        var status = error.Code switch
        {
            "Validation" => StatusCodes.Status400BadRequest,
            "NotFound" => StatusCodes.Status404NotFound,
            "Conflict" => StatusCodes.Status409Conflict,
            "Unauthorized" => StatusCodes.Status401Unauthorized,
            "Forbidden" => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return controller.Problem(title: error.Code, detail: error.Message, statusCode: status);
    }
}
