# Bước 2 — Mô hình hóa `ApplicationUser`, `ApplicationRole`, `RefreshToken`

> Mục tiêu: khai ba entity của module Identity, **tất cả ở project `EventHub.Identity.Domain`** ([Quyết định 2](00-tong-quan.md)). Hai cái đầu **mở rộng** type có sẵn của ASP.NET Core Identity; cái thứ ba là POCO thuần.
>
> Lưu ý mentor: entity là code — **mình không viết hộ**. Mình mô tả class/property cần tạo bằng văn xuôi; bạn tự gõ.

---

## 2.1. Cái gì

Tạo ba class, **tất cả trong `EventHub.Identity.Domain`**:

- `ApplicationUser` — kế thừa `IdentityUser<Guid>`.
- `ApplicationRole` — kế thừa `IdentityRole<Guid>`.
- `RefreshToken` — POCO thuần, quan hệ **1-n** với `ApplicationUser`.

## 2.2. Vì sao

**Kế thừa `IdentityUser` thay vì tự dựng bảng user:** cách chính chủ Microsoft khuyến nghị để thêm dữ liệu riêng cho user (đặt tên `ApplicationUser` theo template). Bạn thừa hưởng sẵn UserName, Email, PasswordHash, SecurityStamp, các cờ lockout... — không phải khai lại, không phải tự lo an toàn.

**Vì sao cả ba ở Domain:** xem lý do đầy đủ ở [Quyết định 2](00-tong-quan.md) — tóm tắt: Application (Day 4) cần `UserManager<ApplicationUser>` nên phải thấy type `ApplicationUser`; đặt ở Domain giữ chiều phân lớp Application → Domain đúng. Type `IdentityUser<Guid>` đến từ package abstraction nhẹ `Microsoft.Extensions.Identity.Stores` (không kéo EF) nên coupling Domain là tối thiểu.

**Vì sao `RefreshToken` tách riêng, không nhét vào `AspNetUserTokens`:** `AspNetUserTokens` (`IdentityUserToken`) là chỗ Identity lưu token của *provider ngoài* (Google, 2FA...), khóa tổng hợp `(UserId, LoginProvider, Name)` — không hợp lưu refresh token JWT với vòng đời/rotation riêng. Nên phải là **bảng của bạn**.

**Vì sao quan hệ 1-n (một User → nhiều RefreshToken):** một user đăng nhập trên **nhiều thiết bị** (mỗi thiết bị một token), và **mỗi lần rotation** (Day 4) sinh token mới thay token cũ. Quan hệ 1-n cho phép **thu hồi từng token** độc lập.

## 2.3. Dữ kiện đã xác minh — cách mở rộng đúng chuẩn

Theo tài liệu Microsoft ([customize-identity-model, aspnetcore-10.0](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0)):

- Thêm dữ liệu riêng cho user: tạo class kế thừa `IdentityUser` (lệ đặt tên `ApplicationUser`); EF map property mới **theo convention**.
- Đổi khóa sang `Guid` (Quyết định 1): **bắt buộc** dùng bản generic — `ApplicationUser : IdentityUser<Guid>`, `ApplicationRole : IdentityRole<Guid>`.
- `IdentityUser<TKey>`/`IdentityRole<TKey>` ở package `Microsoft.Extensions.Identity.Stores` (đã reference ở [Bước 1](01-package.md)).

## 2.4. Các bước làm

Tất cả trong **`EventHub.Identity.Domain`**:

1. **`ApplicationUser`:** class kế thừa `IdentityUser<Guid>`. Chưa cần thêm field domain nào — có thể thêm sau. Không tự khai lại UserName/Email/PasswordHash: base đã có. Có thể thêm navigation `ICollection<RefreshToken>` để biểu diễn phía "một" của quan hệ 1-n (cùng project Domain nên tham chiếu thẳng được).
2. **`ApplicationRole`:** class kế thừa `IdentityRole<Guid>` — cùng kiểu khóa `Guid` với `ApplicationUser` (nếu lệch, khai DbContext generic sẽ không biên dịch).
3. **`RefreshToken`:** POCO thuần. Thiết kế **đầy đủ rotation** (đã chốt — phục vụ Day 4):
   - `Id` — khóa chính riêng (`Guid`).
   - `UserId` (`Guid`) — **khóa ngoại** trỏ về `ApplicationUser`.
   - `TokenHash` — **lưu hash** của refresh token, không lưu token thô.
   - `ExpiresAt` (`DateTimeOffset`) — thời điểm hết hạn.
   - `CreatedAt` (`DateTimeOffset`) — thời điểm tạo.
   - `RevokedAt` (`DateTimeOffset?`) — null nếu còn hiệu lực.
   - `ReplacedByTokenHash` (`string?`) — khi rotation, trỏ token mới thay nó (phát hiện tái sử dụng token cũ).
   - (Tùy chọn) `CreatedByIp` — audit.

> **Vì sao lưu `TokenHash` chứ không token thô, và vì sao `DateTimeOffset`:** hash để rò rỉ DB không đồng nghĩa lộ token dùng được (giống lý do không lưu mật khẩu thô). `DateTimeOffset` map sang Postgres `timestamptz` (có offset) — an toàn khi so sánh thời điểm hết hạn qua các múi giờ. Logic sinh/verify/rotation token ở **Day 4**; Day 3 chỉ dựng *hình dạng dữ liệu*.

> **Lưu ý ranh giới (nay cả ba ở Domain):** `RefreshToken` có thể cầm navigation `ApplicationUser` hoặc chỉ `UserId` — cả hai đều hợp lệ vì cùng project Domain. Chỉ cần nhớ: **Domain không được reference Infrastructure**; entity thì OK, nhưng đừng để Domain đụng `IdentityDbContext` (nằm ở Infrastructure).

## 2.5. Kiểm chứng

```bash
dotnet build EventHub.slnx
```

Build xanh nghĩa là ba entity hợp lệ về kiểu và Domain thấy `IdentityUser<Guid>` qua package Stores. (Nếu đỏ vì thiếu `IdentityUser` → bạn chưa reference `Microsoft.Extensions.Identity.Stores` ở Domain, xem [Bước 1](01-package.md).) Bảng xuất hiện sau migration ([Bước 4](04-migration.md)).

## 2.6. Cạm bẫy thường gặp

- **Lệch kiểu khóa User vs Role:** `ApplicationUser : IdentityUser<Guid>` nhưng `ApplicationRole : IdentityRole` (string) → khai `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>` không biên dịch. Hai cái phải cùng `Guid`.
- **Domain reference nhầm package EF:** để kế thừa `IdentityUser` chỉ cần `Microsoft.Extensions.Identity.Stores`. Nếu lỡ kéo `…Identity.EntityFrameworkCore` vào Domain → dính EF, phá Quyết định 2.
- **Domain đụng `IdentityDbContext`:** DbContext ở Infrastructure. Entity ở Domain **không** được import/tham chiếu DbContext (sai chiều phân lớp).
- **Tự khai lại field có sẵn:** UserName, Email, PasswordHash, SecurityStamp… đã có trong `IdentityUser`.

## 2.7. Góc kể khi phỏng vấn

*"Tôi mở rộng `IdentityUser<Guid>` để tận dụng hashing/lockout/security stamp có sẵn. Cả ba entity đặt ở Domain — coupling chỉ là abstraction `Identity.Stores`, không kéo EF — để Application dùng được `UserManager<ApplicationUser>` mà không phải reference Infrastructure. Refresh token tôi tách bảng riêng, lưu hash, quan hệ 1-n để hỗ trợ rotation và thu hồi từng token trên từng thiết bị."*

## 2.8. Link tài liệu chính thức

- [Custom user data — Identity model customization](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0#custom-user-data)
- [IdentityUser.cs — dotnet/aspnetcore (package Identity.Stores)](https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Stores/src/IdentityUser.cs)
- [Date/time trong Npgsql — timestamptz vs timestamp](https://www.npgsql.org/doc/types/datetime.html)

## 2.9. Xong bước này khi

- [ ] `ApplicationUser`/`ApplicationRole`/`RefreshToken` đều ở **Domain**; User/Role kế thừa `IdentityUser<Guid>`/`IdentityRole<Guid>`.
- [ ] `RefreshToken` có `UserId: Guid` + các field rotation.
- [ ] `dotnet build` xanh; Domain **không** dính package EF, **không** đụng `IdentityDbContext`.

→ Sang [Bước 3 — Nâng cấp DbContext](03-dbcontext.md).
