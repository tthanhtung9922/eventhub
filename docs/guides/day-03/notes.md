# Ghi chú & đính chính Day 3

> Ba khái niệm dưới đây là phần dễ hiểu *mơ hồ* nhất Day 3 (lộ ra khi quiz + review code). Bản ghi này chốt cách hiểu **đúng cơ chế** — chính chỗ này mới là điểm "kể được khi phỏng vấn", không phải chỉ "làm xong". Đọc lại sau khi hoàn thành các bước, và bổ sung nếu review lộ thêm chỗ mơ hồ.

---

## Ghi chú 1 — Ranh giới: vì sao entity Identity đặt ở Domain (không phải Infrastructure)?

**Hiểu sai thường gặp:** "`ApplicationUser` kế thừa framework Identity → phải nhét xuống Infrastructure để Domain thuần POCO."

**Đúng cơ chế — hai luật "sạch" đánh nhau, phải chọn cái ít hại hơn:**

- **Luật DDD:** Domain nên thuần POCO, không dính framework.
- **Luật phân lớp:** Application **không** được depend Infrastructure (chỉ Infra → App/Domain).

Đặt `ApplicationUser` ở đâu thì hai luật này không thể thỏa cùng lúc. Điểm quyết định: Day 4, handler ở `Identity.Application` cần inject **`UserManager<ApplicationUser>`** → Application phải *thấy* type `ApplicationUser`.

- Đặt ở **Infrastructure** → Application phải reference Infrastructure → **đảo chiều phân lớp** (hại nặng).
- Đặt ở **Domain** → Application → Domain, đúng chiều. Domain có dính Identity, nhưng chỉ là **`Microsoft.Extensions.Identity.Stores`** — abstraction user, **không** kéo EF Core/DbContext. Vi phạm "Domain thuần" **rất nhẹ**.

Cân hai cái: vi phạm nhẹ (Domain dính abstraction) < vi phạm nặng (App→Infra). Nên chọn **Domain**.

- **Kiểm chứng:** `EventHub.Identity.Domain.csproj` chỉ được có `Microsoft.Extensions.Identity.Stores`, **không** có package EF Core hay `…Identity.EntityFrameworkCore`. `IdentityDbContext` phải ở Infrastructure, Domain không đụng tới.

**Câu chốt khi phỏng vấn:** *"Nơi đặt entity Identity là trade-off giữa 'Domain thuần POCO' và 'Application không depend Infrastructure'. Vì handler cần `UserManager<ApplicationUser>`, tôi đặt ở Domain để giữ chiều phân lớp đúng; coupling chỉ là `Identity.Stores` (abstraction, không EF) nên vi phạm 'Domain thuần' rất nhẹ. Nếu user cần hành vi domain giàu hơn, tôi tách `User` domain riêng khỏi `ApplicationUser`."*

---

## Ghi chú 2 — `base.OnModelCreating` gọi trước: cơ chế last-one-wins

**Hiểu sai thường gặp:** "gọi `base.OnModelCreating` ở đâu trong hàm cũng được, miễn có gọi."

**Đúng cơ chế:** EF Core cấu hình mô hình theo luật **last-one-wins** — lệnh cấu hình gọi *sau* ghi đè lệnh gọi *trước* cho cùng một thuộc tính. Lớp base `IdentityDbContext<…>` cấu hình toàn bộ 7 bảng Identity *bên trong* `base.OnModelCreating`. Vì vậy:

- Gọi `base` **đầu tiên** → base dựng nền, rồi cấu hình custom của bạn (`RefreshToken`, index, đổi tên...) chồng lên → đúng ý.
- Gọi `base` **sau** cấu hình custom → base ghi đè lại phần bạn vừa chỉnh → mất tùy biến.
- **Quên** gọi `base` → EF không hề cấu hình 7 bảng Identity → migration sinh ra thiếu bảng (hoặc chỉ có `RefreshTokens`).

**Câu chốt khi phỏng vấn:** *"EF last-one-wins nên tôi gọi `base.OnModelCreating` trước để nó dựng mapping Identity, rồi tôi cấu hình RefreshToken chồng lên. Đảo thứ tự thì base ghi đè phần của tôi; quên gọi thì migration mất 7 bảng Identity."*

---

## Ghi chú 3 — `RefreshToken` ≠ `AspNetUserTokens`, và vì sao lưu hash

**Hiểu sai thường gặp:** "Identity đã có bảng `AspNetUserTokens` rồi, refresh token nhét vào đó cho đỡ đẻ bảng."

**Đúng cơ chế:** `AspNetUserTokens` (`IdentityUserToken`) dành cho token của **provider ngoài** (Google, Microsoft, 2FA...), khóa tổng hợp `(UserId, LoginProvider, Name)` — không có chỗ cho `ExpiresAt`, `RevokedAt`, `ReplacedByTokenHash` mà refresh token JWT cần cho **rotation** và **thu hồi**. Nên refresh token phải là **bảng riêng** `RefreshTokens` với vòng đời riêng.

**Vì sao lưu `TokenHash` chứ không phải token thô:** cùng lý do không lưu mật khẩu thô. Nếu DB rò rỉ, token thô = tài khoản bị chiếm; token *đã hash* thì kẻ tấn công không tái tạo được token dùng được. Khi verify (Day 4), bạn hash token client gửi lên rồi so với `TokenHash` trong DB.

**Câu chốt khi phỏng vấn:** *"`AspNetUserTokens` là cho provider ngoài, không hợp refresh token JWT vì thiếu vòng đời expiry/revoke/rotation. Tôi tách bảng riêng và lưu hash thay token thô để rò rỉ DB không đồng nghĩa lộ token dùng được."*
