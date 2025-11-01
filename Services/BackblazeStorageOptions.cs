namespace MiloApi.Services;

public class BackblazeStorageOptions
{
    public const string SectionName = "BackblazeStorage";

    public string KeyId { get; set; } = string.Empty;
    public string ApplicationKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string ServiceUrl { get; set; } = string.Empty;
}
