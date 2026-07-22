using System.ComponentModel;

namespace Finmy.Budgeting.Application.Receipts.Dtos;

public sealed record UploadReceiptResponse(Guid Id, string Url);