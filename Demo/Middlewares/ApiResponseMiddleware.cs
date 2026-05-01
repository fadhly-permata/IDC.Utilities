using System.Text.Json;
using IDC.Utilities.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Middlewares;

public class ApiResponseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiResponseMiddleware> _logger;
    private readonly bool _includeExceptionDetails;
    private static readonly HashSet<string> SkipPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/swagger",
        "/favicon.ico",
        "/health",
    };

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public ApiResponseMiddleware(
        RequestDelegate next,
        ILogger<ApiResponseMiddleware> logger,
        bool includeExceptionDetails = false
    )
    {
        _next = next;
        _logger = logger;
        _includeExceptionDetails = includeExceptionDetails;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var originalBody = context.Response.Body;
        var endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;

        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // Skip jika bukan JSON atau sudah ApiResponse
            if (!IsJsonResponse(context.Response) || IsApiResponseEndpoint(endpoint))
            {
                await ReturnOriginalResponse(context, responseBody, originalBody);
                return;
            }

            // Baca response body
            var responseData = await ReadResponseBody(responseBody);

            // Buat ApiResponse
            var apiResponse = CreateApiResponse(responseData, context.Response.StatusCode);

            // Tulis response baru
            await WriteApiResponse(context, apiResponse, originalBody);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, originalBody);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static bool ShouldSkip(PathString path)
    {
        return SkipPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsJsonResponse(HttpResponse response)
    {
        return response.ContentType?.Contains(
                "application/json",
                StringComparison.OrdinalIgnoreCase
            ) == true
            || response.ContentType?.Contains(
                "application/problem+json",
                StringComparison.OrdinalIgnoreCase
            ) == true;
    }

    private static bool IsApiResponseEndpoint(Endpoint? endpoint)
    {
        if (endpoint?.Metadata.GetMetadata<ProducesResponseTypeAttribute>()?.Type is Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ApiResponse<>);
        }
        return false;
    }

    private static async Task<object?> ReadResponseBody(Stream body)
    {
        body.Seek(0, SeekOrigin.Begin);

        if (body.Length == 0)
            return null;

        try
        {
            // Baca sebagai JSON dan kembalikan sebagai object
            var data = await JsonSerializer.DeserializeAsync<JsonElement>(
                body,
                JsonSerializerOptions
            );

            // Cek jika response sudah berupa ApiResponse
            return IsApiResponse(data) ? data : data;
        }
        catch
        {
            // Jika gagal, baca sebagai string
            body.Seek(0, SeekOrigin.Begin);
            return await new StreamReader(body).ReadToEndAsync();
        }
    }

    private static bool IsApiResponse(JsonElement element)
    {
        // Cek properti dasar dari ApiResponse
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty("isSuccess", out _)
            && element.TryGetProperty("message", out _)
            && element.TryGetProperty("timestamp", out _);
    }

    private static object CreateApiResponse(object? data, int statusCode)
    {
        // Jika data sudah dalam format ApiResponse, kembalikan langsung
        if (data is JsonElement element && IsApiResponse(element))
        {
            return data;
        }

        var isSuccess = statusCode >= 200 && statusCode < 400;
        var message = isSuccess ? "Success" : "An error occurred";

        if (data == null)
            return new ApiResponse(isSuccess, message);

        return new ApiResponse<object>(isSuccess, message, data);
    }

    private static async Task WriteApiResponse(
        HttpContext context,
        object apiResponse,
        Stream originalBody
    )
    {
        var jsonResponse = JsonSerializer.Serialize(apiResponse, JsonSerializerOptions);

        context.Response.Body = originalBody;
        context.Response.ContentType = "application/json";
        context.Response.ContentLength = null;

        await context.Response.WriteAsync(jsonResponse);
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex, Stream originalBody)
    {
        _logger.LogError(ex, "An error occurred while processing the request");

        var errorResponse = ApiResponse.Failure(ex, includeDetails: _includeExceptionDetails);
        var jsonError = JsonSerializer.Serialize(errorResponse, JsonSerializerOptions);

        context.Response.Body = originalBody;
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsync(jsonError);
    }

    private static async Task ReturnOriginalResponse(
        HttpContext context,
        Stream responseBody,
        Stream originalBody
    )
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBody);
    }
}

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseApiResponseWrapper(
        this IApplicationBuilder builder,
        bool includeExceptionDetails = false
    )
    {
        return builder.UseMiddleware<ApiResponseMiddleware>(includeExceptionDetails);
    }
}
