# Bước 4 — Pattern `IModule` + `AddModules()` / `UseModules()`

> Mục tiêu: cho host Bootstrap nạp module một cách **có kỷ luật** — mỗi module tự đăng ký service + endpoint của mình; host chỉ gọi chung, không biết tên từng module.
>
> Lưu ý mentor: interface, extension method, `Program.cs` đều là code — **mình không viết hộ**. Mình mô tả hợp đồng (contract) cần dựng; bạn tự gõ.

---

## 4.1. Cái gì

Định nghĩa một interface `IModule` (hợp đồng "một module biết tự đăng ký"), cho module Identity hiện thực nó, và viết hai extension method `AddModules()` (cấu hình DI) + `UseModules()` (cấu hình pipeline/endpoint) để host gọi một lần là nạp hết.

## 4.2. Vì sao

> TODO mentor: giải thích sâu — vì sao đảo từ "host biết mọi module" sang "module tự khai báo" (giảm coupling, mở rộng không sửa composition root — gần với Open/Closed). Nối với [Day 1](../day-01/00-kien-truc-tong-quan.md): module Api là **class library**; pattern này chính là cơ chế host "gọi vào" library đó. Nhắc lại ranh giới ROADMAP: cross-module chỉ qua Contracts/Wolverine — pattern module **không** phải kênh để module gọi nhau, chỉ là cách host nạp từng module.

## 4.3. Các bước làm

Mô tả bằng lời (bạn tự gõ):

1. **Định nghĩa `IModule`.** Một interface với (tối thiểu) hai thành viên:
   - Một method nhận `IServiceCollection` (+ `IConfiguration` nếu cần) để module **đăng ký service** của nó vào DI (DbContext, handler, option...).
   - Một method nhận `IEndpointRouteBuilder` để module **map endpoint** Minimal API của nó.
   > **Quyết định đã chốt (ghi lại):** đặt `IModule` ở **project Shared riêng `src/Shared/EventHub.Modularity`** — *không* nhét vào `SharedKernel`. Lý do: `SharedKernel` là "viên gạch domain" (`Result<T>`, guard, domain-event base); còn `IModule` phụ thuộc kiểu ASP.NET Core (`IEndpointRouteBuilder`, `IServiceCollection`) — trộn vào SharedKernel sẽ kéo phụ thuộc web vào lớp domain thuần. Tách project riêng giữ trách nhiệm sạch. Việc phát sinh (không ghi ở scaffold Day 1 vì lúc đó chưa cần): `dotnet new classlib` project này, `dotnet sln add`, thêm `FrameworkReference` `Microsoft.AspNetCore.App`, rồi các module Api + host reference nó.

2. **Module Identity hiện thực `IModule`.** Trong `src/Modules/Identity/EventHub.Identity.Api`, tạo một class hiện thực `IModule`: phần đăng ký service gọi vào Infrastructure (DbContext...), phần map endpoint khai các route của Identity. Nhớ từ [notes Day 1](../day-01/notes.md): project này là `Microsoft.NET.Sdk` thường nên cần **`FrameworkReference` tới `Microsoft.AspNetCore.App`** để dùng được `IEndpointRouteBuilder` / `MapGet` — **không** đổi sang SDK `.Web`.

3. **Viết `AddModules()` và `UseModules()` trong host.** Trong `src/Bootstrap/EventHub.Api`, hai extension method:
   - `AddModules()` — tìm tất cả `IModule`, gọi method đăng ký service của từng cái lên `IServiceCollection`.
   - `UseModules()` — gọi method map endpoint của từng `IModule` lên app.
   > TODO mentor: chốt **cách host tìm `IModule`**: quét assembly bằng reflection (tiện, tự động) hay đăng ký tường minh một danh sách module (rõ ràng, dễ trace)? Ghi đánh đổi + lựa chọn. Nếu reflection, lưu ý phải đảm bảo assembly của module được **nạp** (referenced) thì mới quét thấy.

4. **Gọi trong `Program.cs` của host.** Thay phần wire tay (nếu có) bằng `builder.Services.AddModules(...)` và `app.UseModules()`.

## 4.4. Kiểm chứng

```bash
dotnet build EventHub.slnx
dotnet run --project src/Bootstrap/EventHub.Api
```

- Build xanh.
- Host chạy; endpoint của module Identity (vd một endpoint thử do bạn map qua `IModule`) **gọi được** — chứng tỏ host đã nạp module qua `UseModules()`, không phải wire tay.
- Tắt host bằng `Ctrl+C`.

> TODO mentor: gợi ý một cách kiểm chứng cụ thể hơn — vd map một endpoint `GET /identity/ping` trong module rồi `curl` nó; xác nhận nó xuất hiện nhờ `UseModules()` chứ không khai trong `Program.cs`.

## 4.5. Cạm bẫy thường gặp

- **Module không được nạp nên reflection quét không thấy:** nếu host không reference (trực tiếp hoặc gián tiếp) assembly module, JIT chưa nạp nó → quét reflection ra rỗng. Đảm bảo host reference các project module Api.
- **Nhầm pattern module thành kênh giao tiếp cross-module:** `IModule` chỉ để host *nạp* module; module **không** được dùng nó để gọi sang module khác — cross-module vẫn chỉ qua Contracts/Wolverine.
- **Quên `FrameworkReference`:** module Api là library thường, thiếu `FrameworkReference` thì không có kiểu `IEndpointRouteBuilder` → không compile.
- **Thứ tự đăng ký:** một số service phải đăng ký trước khi build `app`. `AddModules()` chạy lúc cấu hình `builder.Services`; `UseModules()` chạy sau khi có `app`. Đừng đảo.

## 4.6. Góc kể khi phỏng vấn

> TODO mentor: điền — gợi ý "composition root mở-để-mở-rộng, đóng-để-sửa: thêm module không đụng Program.cs", "phân biệt nạp module vs giao tiếp cross-module".

## 4.7. Link tài liệu chính thức

- [Minimal APIs — route groups & `IEndpointRouteBuilder`](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/route-handlers)
- [Dependency injection trong ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)
- [`FrameworkReference` cho thư viện dùng ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/target-aspnetcore)

## 4.8. Xong bước này khi

- [ ] `IModule` tồn tại (ghi rõ đặt ở project nào + lý do).
- [ ] Module Identity hiện thực `IModule` (đăng ký service + map endpoint).
- [ ] Host có `AddModules()` / `UseModules()`; `Program.cs` không còn wire tay từng module.
- [ ] Host chạy, endpoint của module gọi được.

→ Sang [Bước 5 — Verify & commit](05-verify-commit.md).
