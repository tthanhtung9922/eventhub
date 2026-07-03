# Bước 3 — Nâng cấp `IdentityDbContext` + cấu hình `RefreshToken` + `AddIdentityCore`

> Mục tiêu: đổi `IdentityDbContext` từ `DbContext` trơn (Day 2) sang kế thừa lớp base Identity, cấu hình quan hệ `RefreshToken`, và đăng ký dịch vụ Identity trong DI. Sau bước này migration ở [Bước 4](04-migration.md) mới sinh ra được schema đầy đủ.
>
> Lưu ý mentor: DbContext + đăng ký DI là code — **mình không viết hộ**. Mình mô tả cần đổi gì; bạn tự gõ.

---

## 3.1. Cái gì

Ba việc trong project `EventHub.Identity.Infrastructure`:

1. Đổi `IdentityDbContext` (class của bạn, ở `Persistence/IdentityDbContext.cs`) kế thừa `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>` của package Identity, thay vì `DbContext`.
2. Trong `OnModelCreating`: gọi `base.OnModelCreating(builder)` **trước**, rồi khai `DbSet<RefreshToken>` + cấu hình quan hệ 1-n `ApplicationUser`↔`RefreshToken`.
3. Trong `DependencyInjection.AddInfrastructure`: đăng ký Identity qua `AddIdentityCore<ApplicationUser>()` + `AddRoles<ApplicationRole>()` + `AddEntityFrameworkStores<IdentityDbContext>()`.

> **Ranh giới:** `ApplicationUser`/`ApplicationRole`/`RefreshToken` nằm ở **Domain** ([Quyết định 2](00-tong-quan.md)); `IdentityDbContext` ở **Infrastructure**. Đây là chiều đúng — Infrastructure reference Domain (thêm project reference `EventHub.Identity.Domain` cho Infrastructure nếu chưa có), rồi `using` namespace Domain để thấy các entity. Domain **không** biết gì về DbContext.

## 3.2. Vì sao

**Vì sao kế thừa base Identity:** chính việc `IdentityDbContext` kế thừa `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>` là thứ khiến EF "nhìn thấy" 7 entity Identity và sinh 7 bảng `AspNet*` khi migration. `DbContext` trơn (Day 2) không biết gì về Identity nên migration rỗng.

**Vì sao `base.OnModelCreating(builder)` phải gọi TRƯỚC:** lớp base cấu hình toàn bộ mapping Identity (khóa, index normalized username/email, tên bảng, concurrency stamp...) *bên trong* `OnModelCreating` của nó. EF theo luật **last-one-wins** — cái gọi sau ghi đè cái gọi trước. Bạn muốn base dựng nền trước, rồi *mình thêm/chỉnh* lên trên (cấu hình `RefreshToken`). Nếu gọi `base` **sau** cấu hình custom, base sẽ ghi đè phần của bạn. Nếu **quên** gọi `base`, EF không cấu hình các bảng Identity → migration thiếu bảng.

**Vì sao `AddIdentityCore` (không phải `AddIdentity`):** Day 4 xác thực bằng **JWT**, không dùng cookie/giao diện Razor của Identity. `AddIdentity` (và `AddDefaultIdentity`) kéo thêm cookie authentication + UI mặc định — thừa và gây nhiễu scheme khi bạn tự cấu hình JwtBearer. `AddIdentityCore<ApplicationUser>()` chỉ nạp phần lõi (`UserManager`, password hasher, validators), rồi bạn `.AddRoles<ApplicationRole>()` để có `RoleManager` và `.AddEntityFrameworkStores<IdentityDbContext>()` để store chạy trên EF. Gọn, đúng nhu cầu JWT.

> Dữ kiện: tài liệu Microsoft nêu rõ `AddDefaultIdentity` ≈ `AddAuthentication(cookies)` + `AddIdentityCore` + `AddDefaultUI` — tức phần cookie/UI là thứ `AddIdentityCore` **không** kéo theo. Nguồn: [customize-identity-model — mục AddDefaultIdentity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0#custom-user-data).

## 3.3. Dữ kiện đã xác minh

Theo [customize-identity-model (aspnetcore-10.0)](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0):

- Context custom kế thừa `IdentityDbContext<TUser, TRole, TKey>` — điền `ApplicationUser`, `ApplicationRole`, `Guid`.
- Khi override `OnModelCreating`, **`base.OnModelCreating` phải gọi trước**, cấu hình custom gọi sau (EF *last-one-wins*).
- Cấu hình quan hệ 1-n dùng Fluent API: `HasMany`/`WithOne`/`HasForeignKey`.

## 3.4. Các bước làm

1. **Đổi lớp base:** `IdentityDbContext` của bạn (đang `: DbContext(options)`) đổi thành kế thừa `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`. Constructor vẫn nhận `DbContextOptions<IdentityDbContext>` và chuyền xuống base.
   - ⚠️ **Trùng tên — đọc kỹ:** class của bạn tên `IdentityDbContext`, mà lớp base **cũng** tên `IdentityDbContext<…>` (namespace `Microsoft.AspNetCore.Identity.EntityFrameworkCore`). Trùng tên đơn → trình biên dịch dễ hiểu thành "class kế thừa chính nó". Cách xử lý: **qualify tên base bằng full namespace** khi khai kế thừa, hoặc dùng **`using` alias** cho base. (Cách khác: đổi tên context của bạn — nhưng phải sửa factory/DI/migration đã có; cân nhắc chi phí.)
2. **`OnModelCreating`:** gọi `base.OnModelCreating(builder)` ở **dòng đầu**. Sau đó khai `DbSet<RefreshToken>` (thuộc tính trên context) và cấu hình quan hệ 1-n: `ApplicationUser` có nhiều `RefreshToken`, FK là `RefreshToken.UserId`, `IsRequired`. Cân nhắc thêm index trên `RefreshToken.TokenHash` (tra cứu nhanh khi verify token ở Day 4).
3. **Đăng ký DI trong `AddInfrastructure`:** sau `AddDbContext<IdentityDbContext>(UseNpgsql(...))` đã có từ Day 2, nối chuỗi `AddIdentityCore<ApplicationUser>()` → `.AddRoles<ApplicationRole>()` → `.AddEntityFrameworkStores<IdentityDbContext>()`.
4. **Design-time factory:** kiểm tra `IdentityDbContextFactory` (đã có từ Day 2). Vì Day 3 **không** đặt option ảnh hưởng model (không đụng `MaxLengthForKeys`/`SchemaVersion`), factory hiện tại vẫn dùng được nguyên. Nếu sau này bạn có đặt các option đó, phải áp cùng cấu hình ở design-time (hoặc để `dotnet ef` mượn cấu hình host qua `--startup-project` như Day 2).

## 3.5. Kiểm chứng

```bash
dotnet build EventHub.slnx
```

Build xanh nghĩa là context kế thừa hợp lệ, generic khớp `Guid`, quan hệ cấu hình đúng cú pháp, và đã xử lý trùng tên. (Bảng vẫn chưa có — migration ở [Bước 4](04-migration.md).)

## 3.6. Cạm bẫy thường gặp

- **Trùng tên `IdentityDbContext` (bẫy đặc trưng Day 3):** không qualify/alias base → lỗi biên dịch khó hiểu kiểu "circular base class". Xử lý như mục 3.4 bước 1.
- **Quên `base.OnModelCreating(builder)` hoặc gọi sai thứ tự:** quên → migration thiếu 7 bảng Identity. Gọi *sau* cấu hình custom → base ghi đè cấu hình của bạn.
- **Generic lệch kiểu khóa:** `<…, Guid>` nhưng entity khai `string` → không biên dịch.
- **Dùng `AddIdentity`/`AddDefaultIdentity` thay vì `AddIdentityCore`:** kéo cookie scheme thừa, dễ đụng độ khi cấu hình JwtBearer ở Day 4.

## 3.7. Góc kể khi phỏng vấn

*"Kế thừa `IdentityDbContext<…, Guid>` để EF tự sinh schema Identity; tôi thêm `RefreshToken` là bảng của mình, cấu hình 1-n bằng Fluent API **sau khi** gọi `base.OnModelCreating` — vì EF last-one-wins, gọi base trước để nó dựng nền rồi mình chỉnh lên trên. Tôi chọn `AddIdentityCore` thay vì `AddIdentity` vì auth qua JWT nên không cần lớp cookie/UI."*

## 3.8. Link tài liệu chính thức

- [Customize the model — base context types & OnModelCreating](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0#customize-the-model)
- [Configure ASP.NET Core Identity (`AddIdentityCore` vs `AddIdentity`)](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0)
- [EF Core Relationships — one-to-many](https://learn.microsoft.com/en-us/ef/core/modeling/relationships/one-to-many)

## 3.9. Xong bước này khi

- [ ] `IdentityDbContext` kế thừa `IdentityDbContext<ApplicationUser, ApplicationRole, Guid>`, đã xử lý trùng tên (qualify/alias).
- [ ] `OnModelCreating` gọi `base` **trước**, rồi cấu hình `RefreshToken` (DbSet + quan hệ 1-n + index TokenHash).
- [ ] `AddInfrastructure` đăng ký `AddIdentityCore`/`AddRoles`/`AddEntityFrameworkStores`.
- [ ] `dotnet build` xanh.

→ Sang [Bước 4 — Sinh & áp migration](04-migration.md).
