# Conventional Commits — Hướng dẫn nhanh

## Cú pháp

```
<type>(<scope>): <description>

[body tùy chọn]

[footer tùy chọn]
```

- **type**: loại thay đổi (bắt buộc)
- **scope**: phạm vi ảnh hưởng, ví dụ `auth`, `api` (tùy chọn)
- **description**: mô tả ngắn, viết thường, không dấu chấm cuối (bắt buộc)

## Các loại type phổ biến

| Type | Ý nghĩa |
|------|---------|
| `feat` | Thêm tính năng mới |
| `fix` | Sửa lỗi |
| `docs` | Thay đổi tài liệu |
| `style` | Định dạng code (không đổi logic) |
| `refactor` | Tái cấu trúc code |
| `perf` | Cải thiện hiệu năng |
| `test` | Thêm/sửa test |
| `build` | Thay đổi hệ thống build, dependencies |
| `ci` | Thay đổi cấu hình CI |
| `chore` | Công việc lặt vặt khác |
| `revert` | Hoàn tác commit trước |

## Ví dụ

```
feat(auth): thêm đăng nhập bằng Google
fix: sửa lỗi crash khi giỏ hàng rỗng
docs: cập nhật hướng dẫn cài đặt
refactor(api): tách logic xử lý đơn hàng
```

## Breaking changes

Thêm `!` sau type/scope, hoặc dùng footer `BREAKING CHANGE:`.

```
feat(api)!: đổi định dạng response

BREAKING CHANGE: trường `userId` đổi thành `user_id`
```

## Quy tắc chính

1. Dùng thì hiện tại, mệnh lệnh: "thêm" thay vì "đã thêm".
2. Description ngắn gọn (≤ 50 ký tự là lý tưởng).
3. Mỗi commit chỉ làm một việc.
