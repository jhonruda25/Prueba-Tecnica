using System.Net;
using System.Text.Json;
using Delamujer.ProductCatalog.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Delamujer.ProductCatalog.Api.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;

        ProblemDetails problemDetails;
        int statusCode;

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = (int)HttpStatusCode.BadRequest;
                var errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                problemDetails = new ValidationProblemDetails(errors)
                {
                    Status = statusCode,
                    Title = "One or more validation errors occurred.",
                    Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                    Detail = "The request payload failed domain or format validations.",
                    Instance = context.Request.Path
                };
                _logger.LogWarning(exception, "Validation error occurred. TraceId: {TraceId}", traceId);
                break;

            case InsufficientStockException insufficientStockException:
                statusCode = (int)HttpStatusCode.BadRequest;
                problemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = "Insufficient Stock for Adjustment",
                    Type = "https://fundaciondelamujer.com/errors/insufficient-stock",
                    Detail = insufficientStockException.Message,
                    Instance = context.Request.Path,
                    Extensions =
                    {
                        ["productId"] = insufficientStockException.ProductId,
                        ["currentStock"] = insufficientStockException.CurrentStock,
                        ["requestedDelta"] = insufficientStockException.RequestedDelta
                    }
                };
                _logger.LogWarning(exception, "Insufficient stock attempt for Product {ProductId}. TraceId: {TraceId}",
                    insufficientStockException.ProductId, traceId);
                break;

            case DomainValidationException domainValidationException:
                statusCode = (int)HttpStatusCode.BadRequest;
                problemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = "Domain Business Rule Violation",
                    Type = "https://fundaciondelamujer.com/errors/domain-validation",
                    Detail = domainValidationException.Message,
                    Instance = context.Request.Path
                };
                _logger.LogWarning(exception, "Domain validation failure. TraceId: {TraceId}", traceId);
                break;

            case ProductNotFoundException notFoundException:
                statusCode = (int)HttpStatusCode.NotFound;
                problemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = "Product Not Found",
                    Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
                    Detail = notFoundException.Message,
                    Instance = context.Request.Path,
                    Extensions =
                    {
                        ["productId"] = notFoundException.ProductId
                    }
                };
                _logger.LogInformation(exception, "Product not found: {ProductId}. TraceId: {TraceId}",
                    notFoundException.ProductId, traceId);
                break;

            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                problemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = "An unexpected error occurred while processing your request.",
                    Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                    Detail = "Internal server error. Please contact support with the trace identifier.",
                    Instance = context.Request.Path
                };
                _logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}", traceId);
                break;
        }

        problemDetails.Extensions["traceId"] = traceId;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, problemDetails.GetType(), jsonOptions));
    }
}
