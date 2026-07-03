# Bước 1 — Thêm package Identity vào CPM + reference đúng project

> Mục tiêu: khai hai package Identity trong Central Package Management, rồi cho đúng project tham chiếu (không kèm version — CPM lo version). Vì [Quyết định 2](00-tong-quan.md) đặt entity ở **Domain**, ta cần **hai** package ở **hai** tầng khác nhau.
>
> Lưu ý mentor: `.csproj` và `Directory.Packages.props` là cấu hình — **mình không viết hộ**. Mình nói tên package, version, đặt ở đâu; bạn tự gõ.

---

## 1.1. Cái gì

Khai hai `PackageVersion` trong `Directory.Packages.props`, rồi reference tách theo tầng:

- **`EventHub.Identity.Domain`** → `Microsoft.Extensions.Identity.Stores` (chứa `IdentityUser<TKey>`/`IdentityRole<TKey>` — abstraction thuần, **không** EF).
- **`EventHub.Identity.Infrastructure`** → `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (lớp base `IdentityDbContext<…>` + store chạy trên EF).

## 1.2. Vì sao

**CPM (nhắc lại từ Day 1):** `Directory.Packages.props` giữ **version tập trung một chỗ**; các `.csproj` chỉ khai *tên* package. Nâng/căn version chỉ sửa một file, mọi project đồng bộ.

**Vì sao tách hai package theo tầng:** đây là hệ quả trực tiếp của [Quyết định 2](00-tong-quan.md) (entity ở Domain).

- `ApplicationUser`/`ApplicationRole` (ở **Domain**) chỉ cần *type* `IdentityUser<Guid>`/`IdentityRole<Guid>` để kế thừa. Type đó nằm ở **`Microsoft.Extensions.Identity.Stores`** — một package **abstraction thuần**, không kéo EF Core/DbContext/SQL. Domain reference đúng gói nhẹ này → coupling tối thiểu, không dính database framework.
- `IdentityDbContext` (ở **Infrastructure**) mới cần lớp base EF + các `UserStore`/`RoleStore` chạy trên EF Core. Chúng ở **`Microsoft.AspNetCore.Identity.EntityFrameworkCore`**. Package này **kéo theo** (transitive) chính `Microsoft.Extensions.Identity.Stores` + `Microsoft.EntityFrameworkCore.Relational`.

Nói cách khác: Domain lấy **abstraction** (gói nhẹ); Infrastructure lấy **hiện thực EF** (gói nặng, tự bao gói nhẹ). Đây là cách "kéo framework vào solution" mà vẫn tôn trọng phân lớp.

## 1.3. Dữ kiện đã xác minh

- **Type `IdentityUser<TKey>`/`IdentityRole<TKey>`** nằm ở namespace `Microsoft.AspNetCore.Identity`, trong package **`Microsoft.Extensions.Identity.Stores`** (không phải package EF). Nguồn: [dotnet/aspnetcore — IdentityUser.cs](https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Stores/src/IdentityUser.cs).
- **`Microsoft.AspNetCore.Identity.EntityFrameworkCore`** — stable mới nhất **10.0.9** (2026-06-09); kéo theo `Microsoft.Extensions.Identity.Stores` + `Microsoft.EntityFrameworkCore.Relational` (≥ 10.0.9). Nguồn: [NuGet](https://www.nuget.org/packages/Microsoft.AspNetCore.Identity.EntityFrameworkCore).
- **`Microsoft.Extensions.Identity.Stores`** — dùng cùng dòng version **10.0.9** (cùng họ, để khớp bản mà Identity EF kéo transitively). Nguồn: [NuGet](https://www.nuget.org/packages/Microsoft.Extensions.Identity.Stores).

## 1.4. Các bước làm

1. Mở `Directory.Packages.props` ở **gốc repo**. Thêm **hai** `PackageVersion`: `Microsoft.Extensions.Identity.Stores` và `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, cả hai version `10.0.9` (đặt cùng nhóm EF Core).
2. **Căn version EF Core (quan trọng — xem mục 1.6).** Hiện CPM có `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.2` và `Microsoft.EntityFrameworkCore.Design 10.0.4`. Identity EF `10.0.9` kéo EF Core Relational `10.0.9` → lệch patch. **Nâng cả cụm EF Core lên cùng một patch** (khuyến nghị `10.0.9` cho tất cả) để tránh NuGet cảnh báo/nâng ngầm. Kiểm bản Npgsql provider tương thích tại [nuget Npgsql.EntityFrameworkCore.PostgreSQL](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL).
3. Vào **`EventHub.Identity.Domain`**, thêm `PackageReference` tới `Microsoft.Extensions.Identity.Stores` (**không** version).
4. Vào **`EventHub.Identity.Infrastructure`**, thêm `PackageReference` tới `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (**không** version).

> Ghi chú phân lớp: Infrastructure vốn đã (hoặc sẽ) reference project Domain — nên nó thấy `ApplicationUser` để cấu hình DbContext. Domain **không** reference Infrastructure và **không** cần package EF.

## 1.5. Kiểm chứng

```bash
dotnet restore EventHub.slnx
dotnet build EventHub.slnx
```

Build phải xanh **và không có warning `NU1605` (package downgrade)** hay cảnh báo lệch version EF Core. Nếu có → quay lại mục 1.4 bước 2 căn lại version.

Xác nhận Domain **không** dính EF: mở `EventHub.Identity.Domain.csproj` — chỉ thấy `Microsoft.Extensions.Identity.Stores`, **không** có `Microsoft.AspNetCore.Identity.EntityFrameworkCore` hay package EF Core nào.

## 1.6. Cạm bẫy thường gặp

- **Lệch version EF Core (hay gặp nhất):** Identity EF `10.0.9` kéo EF Core `10.0.9`, trong khi Npgsql provider ghim `10.0.2` → NuGet có thể cảnh báo hoặc nâng ngầm. Căn tất cả package họ EF Core về **cùng một patch**.
- **Đặt nhầm package EF vào Domain:** nếu Domain reference `…Identity.EntityFrameworkCore` (thay vì `…Identity.Stores`), bạn kéo cả EF Core xuống Domain — phá [Quyết định 2](00-tong-quan.md). Domain chỉ lấy gói **Stores** (abstraction).
- **Khai `Version` trong `.csproj`:** vi phạm CPM — version chỉ khai ở `Directory.Packages.props`.

## 1.7. Góc kể khi phỏng vấn

*"Tôi tách package theo tầng: Domain chỉ lấy `Microsoft.Extensions.Identity.Stores` — abstraction user, không kéo EF — nên Domain gần như thuần; Infrastructure lấy `…Identity.EntityFrameworkCore` cho lớp base DbContext và store EF. Cách này giữ coupling ở Domain tối thiểu mà vẫn cho Application dùng được `UserManager<ApplicationUser>`. Version cả cụm EF Core tôi căn cùng patch qua CPM để tránh lệch provider vs core."*

## 1.8. Link tài liệu chính thức

- [NuGet — Microsoft.Extensions.Identity.Stores](https://www.nuget.org/packages/Microsoft.Extensions.Identity.Stores)
- [NuGet — Microsoft.AspNetCore.Identity.EntityFrameworkCore](https://www.nuget.org/packages/Microsoft.AspNetCore.Identity.EntityFrameworkCore)
- [Central Package Management — Microsoft Learn](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
- [ASP.NET Core Identity — tổng quan](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-10.0)

## 1.9. Xong bước này khi

- [ ] `Directory.Packages.props` có `PackageVersion` cho `Microsoft.Extensions.Identity.Stores` + `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (`10.0.9`); cụm EF Core căn cùng patch.
- [ ] Domain reference **Stores**; Infrastructure reference **Identity.EntityFrameworkCore**; không kèm version.
- [ ] `EventHub.Identity.Domain.csproj` **không** dính package EF Core.
- [ ] `dotnet build` xanh, không warning downgrade/lệch version.

→ Sang [Bước 2 — Mô hình hóa entity](02-entities.md).
