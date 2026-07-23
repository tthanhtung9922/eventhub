namespace Finmy.Budgeting.Infrastructure.Storage;

public class S3StorageOptions
{
    public const string SectionName = "Storage";
    public required string Endpoint { get; set; }
    public required string Bucket { get; set; }
    public required string AccessKey { get; set; }
    public required string SecretKey { get; set; }
    public int PresignedUrlLifetimeMinutes { get; set; }
}
