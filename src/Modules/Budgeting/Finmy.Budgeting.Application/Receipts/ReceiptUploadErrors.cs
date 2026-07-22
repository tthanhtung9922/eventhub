using Finmy.SharedKernel.Results;

namespace Finmy.Budgeting.Application.Receipts;

public static class ReceiptUploadErrors
{
    public static readonly Error Empty = new(
        "ReceiptUpload.Empty",
        "The receipt file is empty. File size must be greater than zero.",
        ErrorType.Validation);

    public static Error TooLarge(long maxSizeBytes)
    {
        return new(
            "ReceiptUpload.TooLarge",
            $"The receipt file exceeds the maximum allowed size of {maxSizeBytes / (1024 * 1024)} MB.",
            ErrorType.Validation);
    }

    public static Error ContentTypeNotAllowed(string contentType)
    {
        return new(
            "ReceiptUpload.ContentTypeNotAllowed",
            $"The receipt content type {contentType} is not allowed. Only JPEG and PNG images are accepted.",
            ErrorType.Validation);
    }

    public static readonly Error ContentMismatch = new(
        "ReceiptUpload.ContentMismatch",
        "The receipt file content does not match its declared content type.",
        ErrorType.Validation);
}
