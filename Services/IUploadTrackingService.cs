namespace MiloApi.Services;

public interface IUploadTrackingService
{
    void TrackUpload(UploadMetadata metadata);
    UploadMetadata GetUpload(string uploadId);
    List<UploadMetadata> GetAllUploads();
    void RemoveUpload(string uploadId);
}

public record UploadMetadata(
    string UploadId,
    string FileName,
    string ContentType,
    DateTime CreatedAt,
    UploadStatus Status,
    string? Location = null
);

public enum UploadStatus
{
    Initiated,
    InProgress,
    Completed,
    Failed,
}
