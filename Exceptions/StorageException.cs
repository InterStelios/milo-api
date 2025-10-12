namespace MiloApi.Exceptions;

public class StorageException : Exception
{
    protected StorageException(string message)
        : base(message) { }

    protected StorageException(string message, Exception innerException)
        : base(message, innerException) { }
}

public class UploadNotFoundException(string uploadId)
    : StorageException($"Upload with ID '{uploadId}' was not found")
{
    public string UploadId { get; } = uploadId;
}

public class StorageFileNotFoundException(string fileName)
    : StorageException($"File '{fileName}' was not found")
{
    public string FileName { get; } = fileName;
}

public class PresignedUrlGenerationException(string fileName, Exception innerException)
    : StorageException($"Failed to generate presigned URL for file '{fileName}'", innerException)
{
    public string FileName { get; } = fileName;
}

public class MultipartUploadException(
    string message,
    string? fileName = null,
    string? uploadId = null,
    Exception? innerException = null
) : StorageException(message, innerException!)
{
    public string? UploadId { get; } = uploadId;
    public string? FileName { get; } = fileName;
}
