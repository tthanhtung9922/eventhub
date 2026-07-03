# Bước A (00) — Hiểu trước khi gõ: Identity model & hai quyết định đã chốt

> Mục tiêu bước này: **chỉ đọc, chưa gõ gì**. Nắm bức tranh: ASP.NET Core Identity cho bạn cái gì sẵn, mô hình dữ liệu Identity gồm những bảng nào, và **hai quyết định thiết kế** (đã chốt cùng mentor) định hình cả Day 3. Hiểu xong mới sang [Bước 1](01-package.md).

---

## A.1. Bức tranh Day 3

Day 2 bạn có `IdentityDbContext` kế thừa `DbContext` **trơn**, chưa `DbSet` nào — chỉ đủ chứng minh pipeline EF chạy. Day 3 biến nó thành **mô hình dữ liệu auth thật**: User, Role, và RefreshToken.

Bạn sẽ **không** tự viết bảng User/Role từ số 0. Stack project (xem [ROADMAP](../../ROADMAP.md) mục 2) đã chọn **ASP.NET Core Identity + JWT**. Nghĩa là bạn *kế thừa* mô hình có sẵn của ASP.NET Core Identity rồi mở rộng thêm phần của mình (RefreshToken).

## A.2. Vì sao dùng ASP.NET Core Identity thay vì domain model thuần

Auth là chỗ **dễ làm sai một cách nguy hiểm**. Nếu tự dựng bảng user + tự hash mật khẩu, bạn phải tự lo: thuật toán hash đúng (PBKDF2 nhiều vòng + salt), khóa tài khoản sau N lần sai (lockout), `SecurityStamp` để vô hiệu hóa session khi đổi mật khẩu, chuẩn hóa username/email để tra cứu không phân biệt hoa thường, token provider cho reset mật khẩu/xác nhận email... Làm thiếu một mảnh = lỗ hổng.

ASP.NET Core Identity cho **sẵn tất cả** những thứ đó, đã được kiểm nghiệm ở quy mô lớn, kèm API `UserManager`/`RoleManager` để thao tác. Bạn tập trung vào phần *của bạn* (JWT, refresh rotation, phân quyền) thay vì phát minh lại phần hạ tầng auth.

**Đánh đổi:** coupling — entity của bạn phải kế thừa type của framework (`IdentityUser`), kéo phụ thuộc package Identity. Đây chính là lý do có **Quyết định 2** ở dưới (đặt entity ở đâu, và coupling này "bẩn" tới đâu).

> Câu chốt khi phỏng vấn: *"Tôi không tự hash mật khẩu — auth là chỗ tự làm dễ tạo lỗ hổng. Tôi dùng ASP.NET Core Identity cho phần lưu trữ user/hashing/lockout đã kiểm nghiệm, và tự viết phần đặc thù (JWT + refresh token rotation). Coupling với framework tôi cân nhắc kỹ khi chọn nơi đặt entity (Quyết định 2)."*

## A.3. ASP.NET Core Identity cho sẵn cái gì (7 bảng)

ASP.NET Core Identity định nghĩa **7 kiểu entity** (đều tiền tố `Identity*`) và ánh xạ ra **7 bảng**:

| Entity (CLR type) | Bảng DB | Vai trò |
|-------------------|---------|---------|
| `IdentityUser` | `AspNetUsers` | Tài khoản người dùng (username, email, password hash…) |
| `IdentityRole` | `AspNetRoles` | Vai trò (Admin, User…) |
| `IdentityUserClaim` | `AspNetUserClaims` | Claim gắn với 1 user |
| `IdentityUserLogin` | `AspNetUserLogins` | Liên kết login ngoài (Google…) với user |
| `IdentityUserToken` | `AspNetUserTokens` | Token xác thực của user (do provider ngoài cấp) |
| `IdentityRoleClaim` | `AspNetRoleClaims` | Claim cấp cho mọi user trong 1 role |
| `IdentityUserRole` | `AspNetUserRoles` | Bảng join user ↔ role (quan hệ n-n) |

Quan hệ (theo tài liệu Microsoft): mỗi `User` có nhiều `UserClaim`/`UserLogin`/`UserToken`; mỗi `Role` có nhiều `RoleClaim`; `User`↔`Role` là **n-n** qua bảng join `UserRole`.

Bạn **không** phải tự tạo 7 bảng này — chỉ cần cho `IdentityDbContext` kế thừa lớp base `IdentityDbContext<TUser, TRole, TKey>`, EF sẽ sinh cả 7 bảng khi migration. Chi tiết ở [Bước 3](03-dbcontext.md).

> Nguồn: [Identity model customization — Microsoft Learn (aspnetcore-10.0)](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0) — mục "The Identity model" liệt kê 7 entity + tên bảng.

## A.4. Phần bạn tự thêm: `RefreshToken`

Identity **không** có sẵn entity cho refresh token kiểu bạn cần (access token ngắn hạn + refresh token dài hạn để cấp lại — dùng ở Day 4). Đây là entity **của riêng bạn**: một bảng `RefreshTokens`, quan hệ **1-n** với User (một user có nhiều refresh token qua thời gian: nhiều thiết bị, và mỗi lần rotation đẻ một token mới).

> **Đừng nhầm** `IdentityUserToken`/`AspNetUserTokens` với `RefreshToken` của bạn. `IdentityUserToken` là chỗ Identity lưu token cho provider ngoài (vd token của Google/2FA), **không phải** refresh token JWT của bạn. Hai khái niệm khác nhau, hai bảng khác nhau.

## A.5. Hai quyết định đã chốt (và vì sao)

Đây là phần **quan trọng nhất** của Day 3. Hai quyết định dưới đã cùng mentor chốt — guide các bước sau viết theo lựa chọn này.

### Quyết định 1 — Kiểu khóa chính: **`Guid`** ✅

Mặc định `IdentityUser` dùng khóa chính kiểu `string` (một GUID lưu dạng text). Ta đổi sang **`Guid` thật** bằng cách kế thừa bản generic `IdentityUser<Guid>` / `IdentityRole<Guid>` và dùng context `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`.

**Vì sao Guid:**

- Postgres có kiểu `uuid` native (16 byte) — gọn và index tốt hơn cột text lưu GUID (37 byte).
- Kiểu rõ ràng ở tầng C# (`Guid` chứ không phải `string` mơ hồ), tránh truyền nhầm.
- Là điểm kể được: "tôi cân nhắc kiểu khóa và chọn `uuid` thật thay vì string default".

**Cạm bẫy quan trọng:** đổi kiểu khóa **phải làm ở migration ĐẦU TIÊN**. Đổi PK sau khi bảng đã tạo là rất khó (phải drop/tạo lại bảng — [xem "Change the primary key type"](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0#change-the-primary-key-type)). Vì Day 2 đã áp một migration rỗng `InitialCreate`, ta sẽ **dựng lại migration đó** (Hướng B ở [Bước 4](04-migration.md)) để `Guid` là kiểu khóa ngay từ đầu.

### Quyết định 2 — Đặt `ApplicationUser`/`ApplicationRole`/`RefreshToken` ở **Domain** ✅

`ApplicationUser` kế thừa `IdentityUser<Guid>`, `ApplicationRole` kế thừa `IdentityRole<Guid>`. Cả ba entity (User, Role, RefreshToken) đặt ở **`EventHub.Identity.Domain`** — khớp cả chữ [ROADMAP mục 5](../../ROADMAP.md) ("Module Identity — **Domain** (User, Role, RefreshToken)") lẫn chiều phân lớp.

**Mâu thuẫn cốt lõi — hai luật "sạch" đánh nhau:** đặt entity ở đâu thì project đó phải reference package chứa `IdentityUser`.

- **Luật DDD:** Domain nên thuần POCO, không dính framework.
- **Luật phân lớp:** Application **không** được reference Infrastructure (chỉ Infrastructure → Application/Domain, không ngược).

Không chỗ nào thỏa cả hai. Ở Day 4, handler Login/Register trong `Identity.Application` cần inject **`UserManager<ApplicationUser>`** → Application phải *nhìn thấy* type `ApplicationUser`. Nếu để `ApplicationUser` ở Infrastructure thì **Application phải reference Infrastructure** — đảo chiều phân lớp, mùi nặng. Đặt ở **Domain** thì Application → Domain, đúng chiều.

**Vì sao coupling Doman chấp nhận được:** `IdentityUser<TKey>`/`IdentityRole<TKey>` nằm trong package **`Microsoft.Extensions.Identity.Stores`** — abstraction thuần, **không** kéo EF Core / DbContext / SQL. Nên "Domain reference Identity" ở đây chỉ là *abstraction user nhẹ*, không phải kéo database framework xuống lõi. Vi phạm Luật DDD **nhẹ**; đổi lại giữ được chiều phân lớp **đúng**.

`RefreshToken` là POCO thuần (không kế thừa gì của Identity), cũng ở Domain, chỉ giữ `UserId: Guid`.

```text
EventHub.Identity.Domain → ApplicationUser/ApplicationRole (: IdentityUser<Guid>)
                           + RefreshToken (POCO: UserId: Guid)
                           ref Microsoft.Extensions.Identity.Stores (abstraction, KHÔNG EF)
EventHub.Identity.Infrastructure → IdentityDbContext (kế thừa base Identity EF)
                                   + cấu hình quan hệ 1-n
```

> Câu chốt khi phỏng vấn: *"Đặt entity Identity ở đâu là trade-off giữa hai luật sạch: Domain thuần POCO vs Application không depend Infrastructure. Vì handler cần `UserManager<ApplicationUser>`, đặt ở Domain giữ chiều phân lớp đúng. Coupling Domain chỉ là `Microsoft.Extensions.Identity.Stores` — abstraction user, không kéo EF — nên vi phạm 'Domain thuần' rất nhẹ, chấp nhận được. Nếu sau này user cần hành vi domain giàu hơn, tôi sẽ tách `User` domain riêng khỏi `ApplicationUser` identity."*

## A.6. Xong bước này khi

- [ ] Bạn kể được 7 bảng `AspNet*` dùng làm gì và quan hệ giữa chúng.
- [ ] Bạn phân biệt được `IdentityUserToken` (của Identity) vs `RefreshToken` (của bạn).
- [ ] Bạn nói lại được **vì sao** chọn `Guid` và **vì sao** đặt entity ở Domain (mâu thuẫn hai luật sạch + `UserManager<ApplicationUser>` cần ở Application) — không chỉ "đã chốt vậy".

→ Sang [Bước 1 — Thêm package Identity EF](01-package.md).
