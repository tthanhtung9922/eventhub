# ADR-0008: Serve ảnh hóa đơn bằng presigned URL, đặt CDN trước object-storage origin

## Trạng thái

Accepted — 2026-07-23

## Bối cảnh

Day 11 dựng chiều upload: ảnh hóa đơn được đẩy lên object store (MinIO cục bộ, S3 hoặc Cloudflare R2 lúc production) qua S3 API, và Postgres giữ một dòng `Receipt` trỏ tới object bằng object key. Bucket để private vì hóa đơn là dữ liệu riêng của từng Space.

Day 12 cần chiều đọc: cho một `Receipt`, trả cho client một cách xem lại tấm ảnh. Câu hỏi kiến trúc là serve kiểu gì để vừa giữ bucket private, vừa không biến app thành nút thắt băng thông, vừa nối được với hình dạng production nơi một CDN đứng trước origin. Đây là lát cắt khép lại khái niệm CDN của project, nên cách chọn phải giải thích được khi phỏng vấn, không chỉ chạy được trên máy.

## Các phương án đã cân nhắc

- **Mở bucket cho đọc công khai.** Mỗi object có một URL cố định, CDN cache ở edge dễ vì URL không đổi. Nhưng ai đoán trúng đường dẫn cũng xem được, không chấp nhận được với dữ liệu riêng.

- **Proxy bytes qua app.** App tự tải object từ origin rồi stream lại cho client, có thể gắn cache response ở giữa. Kiểm soát chặt (app đứng giữa mọi request) nhưng app gánh băng thông cho mọi tấm ảnh của mọi người dùng, đúng việc CDN sinh ra để làm thay. Phần cache response để dành Day 13, không chọn làm đường serve chính.

- **Presigned URL cộng 302 redirect, cache object key.** App tra object key (qua HybridCache, cache-aside), ký một presigned URL có hạn rồi `302` redirect client tới origin; client tự lấy bytes. Giữ bucket private mà app không chạm byte ảnh nào.

## Quyết định

Chọn phương án presigned URL cộng redirect. Endpoint `GET /receipts/{id}` cache object key qua HybridCache, ký presigned URL tươi ở mỗi request, và trả `302` với `Cache-Control: private`.

Hai điểm định hình cách làm, ghi riêng vì có lý do:

- **Cache object key, không cache presigned URL.** Object key của một hóa đơn ổn định (`Receipt` immutable) nên cache được lâu; presigned URL có hạn (`PresignedUrlLifetimeMinutes`) nên phải ký tươi từ key đã cache ở mỗi lần phục vụ. Cache một presigned URL sống lâu hơn hạn của nó sẽ phát ra link đã chết, một loại bug chạy đúng vài phút rồi mới thối.

- **Đổi endpoint là chuyển origin.** Code nói S3 API qua `AWSSDK.S3` (quyết định Day 11); MinIO chỉ khác S3 hay R2 thật ở `ServiceURL`. Lên production, object store thật làm origin và một CDN như Cloudflare đứng trước nó, phần code serve không đổi.

## Hệ quả

- Lát cắt hôm nay là *origin-direct*: client đi thẳng tới origin qua presigned URL, chưa hưởng lợi từ một tầng edge. Đây là hệ quả của việc chọn presigned URL, không phải thiếu sót cần vá ngay.

- Presigned URL đá nhau với edge cache. CDN cache theo URL, mà presigned URL đổi mỗi lần ký, nên edge không cache được object qua đường presigned. Cloudflare R2 nói rõ presigned URL chỉ chạy trên domain lưu trữ, cache chỉ chạy trên custom domain. Muốn vừa private vừa cache được ở edge, việc ký phải dời lên tầng CDN: CloudFront signed URL hoặc signed cookie cộng Origin Access Control (object nằm sau CDN ở path bền để edge cache, phần "ai được xem" do cơ chế ký của CDN lo), hoặc Cloudflare custom domain cộng Worker. Đây là bước tiến hóa production, ghi lại để không nhầm lát cắt MinIO hôm nay đã có tầng edge.

- `Cache-Control` phải là `private` vì hóa đơn là dữ liệu cá nhân hóa: một shared cache giữ redirect rồi phát cho người khác là rò rỉ. Mặc định `no-store` cho khỏi phải canh; nếu tối ưu bằng `max-age` để browser cache redirect thì `max-age` bắt buộc nhỏ hơn hạn presigned URL, không thì browser đi theo redirect đã cache tới URL đã hết hạn.

- Endpoint `GET /receipts/{id}` chưa có phân quyền: presigned URL giữ bucket private, nhưng bản thân endpoint thì ai gọi cũng ký được. Khi Space và Member lên hình (Tuần 3), đây là chỗ kiểm caller thuộc Space sở hữu hóa đơn trước khi ký.

- Có một khoản đánh đổi thừa hưởng từ Day 11 còn nguyên: thứ tự upload-bytes-trước, lưu-con-trỏ-sau để lại khả năng object mồ côi nếu commit DB hỏng. Nằm ngoài phạm vi ADR này, xử lý cùng chỗ với concurrency của Ledger (Tuần 3).
