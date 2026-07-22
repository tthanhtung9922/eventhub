namespace Finmy.Budgeting.Application.Abstractions;

public interface IReceiptStorage
{
    Task UploadAsync(Stream content, string objectKey, string contentType, CancellationToken cancellationToken);

    string GetPresignedUrl(string objectKey);
}
