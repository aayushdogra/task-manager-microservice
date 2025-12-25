using TaskManager.Dto;

namespace TaskManager.Helpers;

public static class ApiResults
{
    /* SUCCESS RESPONSES */

    public static IResult Ok<T>(T data)
    {
        return Results.Ok(new ApiResponse<T>(
            Success: true,
            Data: data,
            Error: null
        ));
    }

    public static IResult Created<T>(string location, T data)
    {
        return Results.Created(location, new ApiResponse<T>(
            Success: true,
            Data: data,
            Error: null
        ));
    }

    public static IResult NoContent()
    {
        return Results.Ok(new ApiResponse<object>(
            Success: true,
            Data: null,
            Error: null
        ));
    }

    /* ERROR RESPONSES */

    public static IResult BadRequest(string code, string message, object? details = null)
    {
        return Results.BadRequest(new ApiResponse<object>(
            Success: false,
            Data: null,
            Error: new ApiError(code, message, details)
        ));
    }

    public static IResult NotFound(string code, string message)
    {
        return Results.NotFound(new ApiResponse<object>(
            Success: false,
            Data: null,
            Error: new ApiError(code, message)
        ));
    }

    public static IResult Unauthorized(string message = "Unauthorized")
    {
        return Results.Json(
            new ApiResponse<object>(
                Success: false,
                Data: null,
                Error: new ApiError("UNAUTHORIZED", message)
            ),
            statusCode: StatusCodes.Status401Unauthorized
        );
    }

    /* VALIDATION ERROR RESPONSE */

    public static IResult ValidationError(object errors)
    {
        return Results.BadRequest(new ApiResponse<object>(
            Success: false,
            Data: null,
            Error: new ApiError(
                Code: "VALIDATION_ERROR",
                Message: "One or more validation errors occurred",
                Details: errors
            )
        ));
    }
}