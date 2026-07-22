namespace Finmy.Budgeting.Infrastructure.Storage;

public class S3StorageOptions
{
    public const string SectionName = "Storage";
    public string? Endpoint { get; set; }
    public string? Bucket { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public int PresignedUrlLifetimeMinutes { get; set; }
}
