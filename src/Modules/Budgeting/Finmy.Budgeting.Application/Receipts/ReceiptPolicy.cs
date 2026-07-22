namespace Finmy.Budgeting.Application.Receipts;

public sealed class ReceiptPolicy
{
    // 1) Content type cho phép — so sánh không phân biệt hoa thường
    public static readonly IReadOnlySet<string> AllowedContentTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
        };

    // 2) Kích thước tối đa (5 MB)
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    // 3) Bảng magic bytes theo content type
    private static readonly Dictionary<string, byte[]> MagicBytes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = [0xFF, 0xD8, 0xFF],
            ["image/png"] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],
        };

    // 4) Dựng object key: receipts/{yyyy}/{MM}/{guid v7}{ext}
    public static string BuildObjectKey(string contentType, DateTimeOffset nowUtc)
    {
        var ext = contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            _ => throw new ArgumentException($"Unsupported ContentType: {contentType}", nameof(contentType)),
        };

        return $"receipts/{nowUtc:yyyy}/{nowUtc:MM}/{Guid.CreateVersion7()}{ext}";
    }

    public static bool IsAllowedContentType(string? contentType) =>
        contentType is not null && AllowedContentTypes.Contains(contentType);

    /// <summary>Kiểm tra vài byte đầu của file có khớp magic bytes của content type không.</summary>
    public static bool MatchesMagicBytes(string contentType, ReadOnlySpan<byte> header)
    {
        if (!MagicBytes.TryGetValue(contentType, out var signature))
            return false;

        return header.Length >= signature.Length
            && header[..signature.Length].SequenceEqual(signature);
    }
}
