# ADR-0005: Phát JWT bằng short-name claim với IdentityClaimTypes làm source of truth

## Trạng thái

Accepted — 2026-07-12

## Bối cảnh

Module Identity phát JWT access token chứa danh tính (`sub`, `email`) và role. Phải chọn tên claim và bộ handler đọc/ghi token. Có hai lối đặt tên claim trong .NET: bộ `ClaimTypes.*` (di sản WIF, mỗi tên là một URI dài kiểu WS/SAML như `http://schemas.xmlsoap.org/...`) và tên ngắn chuẩn JWT theo RFC 7519 (`sub`, `email`, `jti`, `exp`...). Chọn nhầm hoặc khai không khớp giữa phía phát và phía đọc gây ra lỗi sai lặng — không báo compile, không ném exception, chỉ chạy sai.

Ngoài tên claim, còn hai quyết định đi kèm lộ ra khi dựng generator + cấu hình `AddJwtBearer`: thời điểm hết hạn token phải test được, và signing key thiếu phải chặn sớm.

## Các phương án đã cân nhắc

- **`ClaimTypes.*`** — quen tay ("cứ dùng `ClaimTypes` cho tiện"), nhưng `ClaimTypes` không hề có member `sub`, còn `ClaimTypes.Email`/`.Role` là URI dài di sản WIF. Token phình, đọc trên jwt.io khó, và lệch chuẩn JWT.

- **`System.IdentityModel.Tokens.Jwt` (handler legacy)** — bộ cũ, còn dùng được nhưng đã có handler mới thay thế.

- **Short-name RFC 7519 qua `JwtRegisteredClaimNames` + `JsonWebTokenHandler`** — tên chuẩn ngắn (`JwtRegisteredClaimNames.Sub` ≡ `"sub"`), handler mới của `Microsoft.IdentityModel.JsonWebTokens`. Token gọn, đúng chuẩn. Đổi lại `Role` không thuộc RFC nên `JwtRegisteredClaimNames` không có — phải tự đặt tên.

## Quyết định

Chúng tôi phát claim bằng **short-name JWT**: `sub`/`email` qua `JwtRegisteredClaimNames`, `role` tự đặt short-name `"role"` (vì RFC 7519 không chuẩn hóa role). Dùng `JsonWebTokenHandler` mới, không dùng handler legacy.

Gom tên claim về một static class **`IdentityClaimTypes`** làm source of truth, đặt trong `Authentication/` của Identity.Infrastructure, dùng ở cả nơi phát lẫn nơi đọc:

- `Sub` / `Email` là **alias tham chiếu** tới `JwtRegisteredClaimNames.*` — không gõ lại literal, nên không đẻ magic string thứ hai.
- `Role` là literal `"role"` — đây là nơi *duy nhất* literal được phép tồn tại, vì thư viện không cấp hằng cho nó.

Vì đã rời mặc định của ASP.NET Core (short-name thay URL dài), phía đọc token phải khai lại cho khớp: set `TokenValidationParameters.RoleClaimType = IdentityClaimTypes.Role`, và `options.MapInboundClaims = false` (đặt trên `options`, không trong `TokenValidationParameters`).

Hai quyết định đi kèm: inject `TimeProvider` thay `DateTime.UtcNow` để tính `Expires`; fail-fast signing key đối xứng ở cả phía phát lẫn phía đọc (thiếu key thì ném lúc khởi động, không bao giờ fallback chuỗi rỗng). Không đẩy `IdentityClaimTypes` lên `SharedKernel`/`Contracts` — tên claim là wire-format nội bộ module Identity, đẩy ra là rò rỉ ranh giới cross-module.

## Hệ quả

- Token gọn, đúng RFC, đọc trên jwt.io dễ; nhiều role của một user là nhiều claim cùng type `"role"`, thư viện tự gộp thành JSON array khi serialize.
- Một source of truth cho tên claim, dùng cả nơi phát lẫn nơi validate — sửa một chỗ, không lệch hai nơi.
- Cạm bẫy phải trả giá cho việc rời mặc định: quên `RoleClaimType` thì `[Authorize(Roles=...)]` luôn 403 dù đúng role; quên `MapInboundClaims = false` thì handler remap `sub` thành `nameidentifier`, `User.FindFirst("sub")` trả null. Cả hai fail lặng, test tay khó thấy — nên phải verify end-to-end.
- `TimeProvider` cho phép bơm `FakeTimeProvider` để kiểm `exp` xác định, không phụ thuộc đồng hồ máy chạy test.
- Fail-fast signing key: thiếu config thì app không khởi động được, còn hơn chạy với chữ ký rỗng (validate bằng key rỗng gần như vô hiệu, token giả dễ qua). Xem thêm phần siết bảo mật (validate độ dài key, `ClockSkew`, `ValidAlgorithms`) ở `docs/guides/day-04/07-hardening-bao-mat.md`.
