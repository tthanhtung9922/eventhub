using Finmy.SharedKernel.Results;

namespace Finmy.Budgeting.Domain.Receipts;

public static class ReceiptErrors
{
    public static readonly Error ObjectKeyRequired = new(
        "Receipt.ObjectKeyEmpty",
        "The receipt object key must not be empty.",
        ErrorType.Validation);

    public static readonly Error ContentTypeRequired = new(
        "Receipt.ContentTypeEmpty",
        "The receipt content type must not be empty.",
        ErrorType.Validation);

    public static readonly Error SizeNotPositive = new(
        "Receipt.SizeNotPositive",
        "The receipt size must be greater than zero.",
        ErrorType.Validation);
}
