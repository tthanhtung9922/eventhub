using Finmy.Budgeting.Application.Abstractions;
using Finmy.Budgeting.Application.Receipts.Dtos;
using Finmy.Budgeting.Domain.Receipts;
using Finmy.SharedKernel.Results;

namespace Finmy.Budgeting.Application.Receipts;

public sealed class ReceiptService(IReceiptStorage receiptStorage, IReceiptRepository receiptRepository)
{
    public async Task<Result<UploadReceiptResponse>> UploadAsync(Stream content, long sizeBytes, string contentType, string originalFileName, CancellationToken cancellationToken)
    {
        var validate = ReceiptFileValidator.Validate(sizeBytes, contentType, content);

        if (validate.IsFailure)
            return validate.Error;

        var uploadedAt = DateTimeOffset.UtcNow;

        var objectKey = ReceiptPolicy.BuildObjectKey(contentType, uploadedAt);

        // Upload to S3
        await receiptStorage.UploadAsync(content, objectKey, contentType, cancellationToken);

        var receipt = Receipt.Create(objectKey, contentType, sizeBytes, originalFileName, uploadedAt);

        if (receipt.IsFailure)
            return receipt.Error;

        receiptRepository.Add(receipt.Value);

        await receiptRepository.SaveChangesAsync(cancellationToken);

        var url = receiptStorage.GetPresignedUrl(objectKey);

        return new UploadReceiptResponse(receipt.Value.Id, url);
    }
}
