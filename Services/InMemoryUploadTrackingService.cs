using System.Collections.Concurrent;
using MiloApi.Exceptions;

namespace MiloApi.Services;

public class InMemoryUploadTrackingService : IUploadTrackingService
{
    private readonly ConcurrentDictionary<string, UploadMetadata> _uploads = new();
    private readonly ILogger<InMemoryUploadTrackingService> _logger;

    public InMemoryUploadTrackingService(ILogger<InMemoryUploadTrackingService> logger)
    {
        _logger = logger;
        _logger.LogInformation("InMemoryUploadTrackingService initialized");
    }

    public void TrackUpload(UploadMetadata metadata)
    {
        _logger.LogDebug(
            "Tracking upload for UploadId: {UploadId}, FileName: {FileName}, Status: {Status}",
            metadata.UploadId,
            metadata.FileName,
            metadata.Status
        );

        _uploads.AddOrUpdate(metadata.UploadId, metadata, (_, _) => metadata);

        _logger.LogDebug(
            "Upload tracked successfully. Total uploads: {TotalUploads}",
            _uploads.Count
        );
    }

    public UploadMetadata GetUpload(string uploadId)
    {
        _logger.LogDebug("Retrieving upload for UploadId: {UploadId}", uploadId);

        if (!_uploads.TryGetValue(uploadId, out var metadata))
        {
            _logger.LogWarning("Upload not found for UploadId: {UploadId}", uploadId);
            throw new UploadNotFoundException(uploadId);
        }

        _logger.LogDebug(
            "Upload found for UploadId: {UploadId}, Status: {Status}",
            uploadId,
            metadata.Status
        );
        return metadata;
    }

    public List<UploadMetadata> GetAllUploads()
    {
        _logger.LogDebug("Retrieving all uploads. Total count: {TotalUploads}", _uploads.Count);

        var uploads = _uploads.Values.OrderByDescending(u => u.CreatedAt).ToList();

        _logger.LogDebug("Retrieved {Count} uploads", uploads.Count);
        return uploads;
    }

    public void RemoveUpload(string uploadId)
    {
        _logger.LogDebug("Removing upload for UploadId: {UploadId}", uploadId);

        var removed = _uploads.TryRemove(uploadId, out var removedMetadata);

        if (removed)
        {
            _logger.LogDebug(
                "Upload removed successfully for UploadId: {UploadId}. Remaining uploads: {TotalUploads}",
                uploadId,
                _uploads.Count
            );
        }
        else
        {
            _logger.LogDebug("Upload not found to remove for UploadId: {UploadId}", uploadId);
        }
    }
}
