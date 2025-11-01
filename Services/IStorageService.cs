namespace MiloApi.Services;

public interface IStorageService
{
    Task<string> GeneratePresignedUploadUrlAsync(
        string fileName,
        string contentType,
        int expirationMinutes = 60
    );
    Task<MultipartUploadInfo> InitiateMultipartUploadAsync(string fileName, string contentType);
    Task<string> GeneratePresignedPartUploadUrlAsync(
        string fileName,
        string uploadId,
        int partNumber,
        int expirationMinutes = 60
    );
    Task<string> CompleteMultipartUploadAsync(
        string fileName,
        string uploadId,
        List<PartETag> parts
    );
    Task<string> GeneratePresignedDownloadUrlAsync(string fileName, int expirationMinutes = 60);
    Task<string> GeneratePresignedViewUrlAsync(string fileName, int expirationMinutes = 60);
}

public record MultipartUploadInfo(string UploadId, string FileName);

public record PartETag(int PartNumber, string ETag);
