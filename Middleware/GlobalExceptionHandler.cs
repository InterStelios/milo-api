using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiloApi.Exceptions;

namespace MiloApi.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        logger.LogError(
            exception,
            "Unhandled exception occurred: {ExceptionType}, TraceId: {TraceId}",
            exception.GetType().Name,
            traceId
        );

        var (statusCode, title, detail) = MapExceptionToResponse(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Extensions = { ["traceId"] = traceId, ["timestamp"] = DateTime.UtcNow },
        };

        // Add exception-specific extensions
        AddExceptionSpecificDetails(exception, problemDetails);

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, string Detail) MapExceptionToResponse(
        Exception exception
    )
    {
        return exception switch
        {
            UploadNotFoundException ex => (
                (int)HttpStatusCode.NotFound,
                "Upload Not Found",
                ex.Message
            ),
            StorageFileNotFoundException ex => (
                (int)HttpStatusCode.NotFound,
                "File Not Found",
                ex.Message
            ),
            PresignedUrlGenerationException ex => (
                (int)HttpStatusCode.InternalServerError,
                "URL Generation Failed",
                "Failed to generate presigned URL. Please try again."
            ),
            MultipartUploadException ex => (
                (int)HttpStatusCode.InternalServerError,
                "Upload Failed",
                ex.Message
            ),
            StorageException ex => (
                (int)HttpStatusCode.InternalServerError,
                "Storage Operation Failed",
                ex.Message
            ),
            ArgumentNullException => (
                (int)HttpStatusCode.BadRequest,
                "Invalid Request",
                "Required parameter is missing"
            ),
            ArgumentException ex => ((int)HttpStatusCode.BadRequest, "Invalid Request", ex.Message),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please try again later."
            ),
        };
    }

    private static void AddExceptionSpecificDetails(
        Exception exception,
        ProblemDetails problemDetails
    )
    {
        switch (exception)
        {
            case UploadNotFoundException ex:
                problemDetails.Extensions["uploadId"] = ex.UploadId;
                break;
            case StorageFileNotFoundException ex:
                problemDetails.Extensions["fileName"] = ex.FileName;
                break;
            case PresignedUrlGenerationException ex:
                problemDetails.Extensions["fileName"] = ex.FileName;
                break;
            case MultipartUploadException ex:
                if (ex.UploadId != null)
                    problemDetails.Extensions["uploadId"] = ex.UploadId;
                if (ex.FileName != null)
                    problemDetails.Extensions["fileName"] = ex.FileName;
                break;
        }
    }
}
