using Finmy.Budgeting.Application.Receipts;

using Shouldly;

namespace Finmy.UnitTests.Budgeting;

public class ReceiptFileValidatorTests
{
    private const long ValidSize = 1024; // trong hạn

    private static MemoryStream StreamOf(params byte[] bytes) => new(bytes);

    [Fact]
    public void Validate_ValidJpeg_ReturnsSuccess()
    {
        var content = StreamOf(0xFF, 0xD8, 0xFF, 0xE0);

        var result = ReceiptFileValidator.Validate(ValidSize, "image/jpeg", content);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validate_ValidPng_ReturnsSuccess()
    {
        var content = StreamOf(0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A);

        var result = ReceiptFileValidator.Validate(ValidSize, "image/png", content);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Validate_JpegContentTypeButExeBytes_ReturnsContentMismatch()
    {
        // Khai image/jpeg nhưng mở đầu là MZ (header .exe) → sniff phải bắt được
        var content = StreamOf(0x4D, 0x5A);

        var result = ReceiptFileValidator.Validate(ValidSize, "image/jpeg", content);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ReceiptUploadErrors.ContentMismatch);
    }

    [Fact]
    public void Validate_ContentTypeNotInWhitelist_ReturnsContentTypeNotAllowed()
    {
        var content = StreamOf(0x25, 0x50, 0x44, 0x46); // "%PDF"

        var result = ReceiptFileValidator.Validate(ValidSize, "application/pdf", content);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(ReceiptUploadErrors.ContentTypeNotAllowed("application/pdf").Code);
    }

    [Fact]
    public void Validate_SizeExceedsMax_ReturnsTooLarge()
    {
        var content = StreamOf(0xFF, 0xD8, 0xFF);

        var result = ReceiptFileValidator.Validate(
            ReceiptPolicy.MaxSizeBytes + 1, "image/jpeg", content);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(
            ReceiptUploadErrors.TooLarge(ReceiptPolicy.MaxSizeBytes).Code);
    }

    [Fact]
    public void Validate_ZeroSize_ReturnsEmpty()
    {
        var content = StreamOf(0xFF, 0xD8, 0xFF);

        var result = ReceiptFileValidator.Validate(0, "image/jpeg", content);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ReceiptUploadErrors.Empty);
    }
}
