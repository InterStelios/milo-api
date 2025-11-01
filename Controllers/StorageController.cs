using Microsoft.AspNetCore.Mvc;
using MiloApi.Services;

namespace MiloApi.Controllers;

public class StorageController(
    IStorageService storageService,
    IUploadTrackingService trackingService
) : BaseApiController
{
    [HttpPost("upload/presigned-url")]
    public async Task<IActionResult> GetPresignedUploadUrl([FromBody] PresignedUrlRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FileName, nameof(request.FileName));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ContentType, nameof(request.ContentType));

        if (request.ExpirationMinutes is < 1 or > 10080)
            throw new ArgumentException(
                "Expiration minutes must be between 1 and 10080 (7 days)",
                nameof(request.ExpirationMinutes)
            );

        var url = await storageService.GeneratePresignedUploadUrlAsync(
            request.FileName,
            request.ContentType,
            request.ExpirationMinutes ?? 60
        );

        return Ok(new { url });
    }

    [HttpPost("upload/multipart/initiate")]
    public async Task<IActionResult> InitiateMultipartUpload(
        [FromBody] InitiateMultipartRequest request
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FileName, nameof(request.FileName));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ContentType, nameof(request.ContentType));

        var uploadInfo = await storageService.InitiateMultipartUploadAsync(
            request.FileName,
            request.ContentType
        );

        // Track the upload
        trackingService.TrackUpload(
            new UploadMetadata(
                uploadInfo.UploadId,
                uploadInfo.FileName,
                request.ContentType,
                DateTime.UtcNow,
                UploadStatus.Initiated
            )
        );

        return Ok(uploadInfo);
    }

    [HttpPost("upload/multipart/part-url")]
    public async Task<IActionResult> GetMultipartPartUrl([FromBody] MultipartPartUrlRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FileName, nameof(request.FileName));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UploadId, nameof(request.UploadId));

        if (request.PartNumber < 1 || request.PartNumber > 10000)
            throw new ArgumentException(
                "Part number must be between 1 and 10000",
                nameof(request.PartNumber)
            );

        if (request.ExpirationMinutes is < 1 or > 10080)
            throw new ArgumentException(
                "Expiration minutes must be between 1 and 10080 (7 days)",
                nameof(request.ExpirationMinutes)
            );

        var url = await storageService.GeneratePresignedPartUploadUrlAsync(
            request.FileName,
            request.UploadId,
            request.PartNumber,
            request.ExpirationMinutes ?? 60
        );

        return Ok(new { url });
    }

    [HttpPost("upload/multipart/complete")]
    public async Task<IActionResult> CompleteMultipartUpload(
        [FromBody] CompleteMultipartRequest request
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FileName, nameof(request.FileName));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.UploadId, nameof(request.UploadId));
        ArgumentNullException.ThrowIfNull(request.Parts, nameof(request.Parts));

        if (request.Parts.Count == 0)
            throw new ArgumentException("Parts list cannot be empty", nameof(request.Parts));

        var location = await storageService.CompleteMultipartUploadAsync(
            request.FileName,
            request.UploadId,
            request.Parts
        );

        // Update tracking
        var existing = trackingService.GetUpload(request.UploadId);
        trackingService.TrackUpload(
            existing with
            {
                Status = UploadStatus.Completed,
                Location = location,
            }
        );

        return Ok(new { location });
    }

    [HttpGet("uploads")]
    public IActionResult GetAllUploads()
    {
        var uploads = trackingService.GetAllUploads();
        return Ok(uploads);
    }

    [HttpGet("download/presigned-url")]
    public async Task<IActionResult> GetPresignedDownloadUrl(
        [FromQuery] string fileName,
        [FromQuery] int? expirationMinutes
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));

        if (expirationMinutes is < 1 or > 10080)
            throw new ArgumentException(
                "Expiration minutes must be between 1 and 10080 (7 days)",
                nameof(expirationMinutes)
            );

        var url = await storageService.GeneratePresignedDownloadUrlAsync(
            fileName,
            expirationMinutes ?? 60
        );

        return Ok(new { url });
    }

    [HttpGet("view/presigned-url")]
    public async Task<IActionResult> GetPresignedViewUrl(
        [FromQuery] string fileName,
        [FromQuery] int? expirationMinutes
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));

        if (expirationMinutes is < 1 or > 10080)
            throw new ArgumentException(
                "Expiration minutes must be between 1 and 10080 (7 days)",
                nameof(expirationMinutes)
            );

        var url = await storageService.GeneratePresignedViewUrlAsync(
            fileName,
            expirationMinutes ?? 60
        );

        return Ok(new { url });
    }
}

public record PresignedUrlRequest(string FileName, string ContentType, int? ExpirationMinutes);

public record InitiateMultipartRequest(string FileName, string ContentType);

public record MultipartPartUrlRequest(
    string FileName,
    string UploadId,
    int PartNumber,
    int? ExpirationMinutes
);

public record CompleteMultipartRequest(string FileName, string UploadId, List<PartETag> Parts);
