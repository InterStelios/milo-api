namespace MiloApi.Services;

public class BackblazeStorageOptions
{
    public const string SectionName = "BackblazeStorage";

    public string KeyId { get; set; } = string.Empty;
    public string ApplicationKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-west-004";
    public string ServiceUrl { get; set; } = "https://s3.us-west-004.backblazeb2.com";
}
