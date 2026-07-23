using Amazon.S3;
using Amazon.S3.Model;

using Finmy.Budgeting.Application.Abstractions;

using Microsoft.Extensions.Options;

namespace Finmy.Budgeting.Infrastructure.Storage;

public class S3ReceiptStorage(IAmazonS3 s3, IOptions<S3StorageOptions> options) : IReceiptStorage
{
    public async Task UploadAsync(Stream content, string objectKey, string contentType, CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            BucketName = options.Value.Bucket,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };

        await s3.PutObjectAsync(request, cancellationToken);
    }

    public string GetPresignedUrl(string objectKey)
    {
        var protocol = new Uri(options.Value.Endpoint).Scheme == Uri.UriSchemeHttps ? Protocol.HTTPS : Protocol.HTTP;

        var request = new GetPreSignedUrlRequest
        {
            BucketName = options.Value.Bucket,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Protocol = protocol,
            Expires = DateTime.UtcNow.AddMinutes(options.Value.PresignedUrlLifetimeMinutes)
        };

        return s3.GetPreSignedURL(request);
    }
}
