using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using MiloApi.Exceptions;

namespace MiloApi.Services;

public class BackblazeStorageService : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly BackblazeStorageOptions _options;
    private readonly ILogger<BackblazeStorageService> _logger;

    public BackblazeStorageService(
        IOptions<BackblazeStorageOptions> options,
        ILogger<BackblazeStorageService> logger
    )
    {
        _options = options.Value;
        _logger = logger;

        var credentials = new BasicAWSCredentials(_options.KeyId, _options.ApplicationKey);
        var config = new AmazonS3Config
        {
            ServiceURL = _options.ServiceUrl,
            ForcePathStyle = false,
            UseHttp = false,
        };

        _s3Client = new AmazonS3Client(credentials, config);

        _logger.LogInformation(
            "BackblazeStorageService initialized for bucket: {BucketName}",
            _options.BucketName
        );
    }

    public async Task<string> GeneratePresignedUploadUrlAsync(
        string fileName,
        string contentType,
        int expirationMinutes = 60
    )
    {
        _logger.LogDebug(
            "Generating presigned upload URL for {FileName}, ContentType: {ContentType}, ExpirationMinutes: {ExpirationMinutes}",
            fileName,
            contentType,
            expirationMinutes
        );

        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = fileName,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            };

            var preSignedUrl = await _s3Client.GetPreSignedURLAsync(request);

            return preSignedUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to generate presigned upload URL for {FileName}",
                fileName
            );
            throw new PresignedUrlGenerationException(fileName, ex);
        }
    }

    public async Task<MultipartUploadInfo> InitiateMultipartUploadAsync(
        string fileName,
        string contentType
    )
    {
        _logger.LogInformation(
            "Initiating multipart upload for {FileName}, ContentType: {ContentType}",
            fileName,
            contentType
        );

        try
        {
            var request = new InitiateMultipartUploadRequest
            {
                BucketName = _options.BucketName,
                Key = fileName,
                ContentType = contentType,
            };

            var initiateMultipartUploadResponse = await _s3Client.InitiateMultipartUploadAsync(
                request
            );
            var uploadId = initiateMultipartUploadResponse.UploadId;

            _logger.LogInformation(
                "Multipart upload initiated with UploadId: {UploadId}",
                uploadId
            );

            return new MultipartUploadInfo(uploadId, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate multipart upload for {FileName}", fileName);
            throw new MultipartUploadException(
                $"Failed to initiate multipart upload for file '{fileName}'",
                fileName,
                innerException: ex
            );
        }
    }

    public async Task<string> GeneratePresignedPartUploadUrlAsync(
        string fileName,
        string uploadId,
        int partNumber,
        int expirationMinutes = 60
    )
    {
        _logger.LogDebug(
            "Generating presigned part URL for UploadId: {UploadId}, PartNumber: {PartNumber}",
            uploadId,
            partNumber
        );

        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = fileName,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                UploadId = uploadId,
                PartNumber = partNumber,
            };

            var preSignedUrl = await _s3Client.GetPreSignedURLAsync(request);

            return preSignedUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to generate presigned part URL for UploadId: {UploadId}, PartNumber: {PartNumber}",
                uploadId,
                partNumber
            );
            throw new MultipartUploadException(
                $"Failed to generate presigned URL for part {partNumber}",
                fileName,
                uploadId,
                ex
            );
        }
    }

    public async Task<string> CompleteMultipartUploadAsync(
        string fileName,
        string uploadId,
        List<PartETag> parts
    )
    {
        _logger.LogInformation(
            "Completing multipart upload for UploadId: {UploadId}, PartsCount: {PartsCount}",
            uploadId,
            parts.Count
        );

        try
        {
            var request = new CompleteMultipartUploadRequest
            {
                BucketName = _options.BucketName,
                Key = fileName,
                UploadId = uploadId,
                PartETags = parts
                    .Select(p => new Amazon.S3.Model.PartETag(p.PartNumber, p.ETag))
                    .ToList(),
            };

            var completeMultipartUploadResponse = await _s3Client.CompleteMultipartUploadAsync(
                request
            );
            var location = completeMultipartUploadResponse.Location;

            _logger.LogInformation("Multipart upload completed, Location: {Location}", location);

            return location;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to complete multipart upload for UploadId: {UploadId}",
                uploadId
            );
            throw new MultipartUploadException(
                "Failed to complete multipart upload",
                fileName,
                uploadId,
                ex
            );
        }
    }

    public async Task<string> GeneratePresignedDownloadUrlAsync(
        string fileName,
        int expirationMinutes = 60
    )
    {
        _logger.LogDebug(
            "Generating presigned download URL for {FileName}, ExpirationMinutes: {ExpirationMinutes}",
            fileName,
            expirationMinutes
        );

        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = fileName,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            };
            var preSignedUrl = await _s3Client.GetPreSignedURLAsync(request);

            return preSignedUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to generate presigned download URL for {FileName}",
                fileName
            );
            throw new PresignedUrlGenerationException(fileName, ex);
        }
    }
}