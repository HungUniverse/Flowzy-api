# Kết quả sửa lỗi production — 29/09/2026

Source được sửa tại `D:/FLowzy-BE/Flowzy-api` (thư mục `D:/Flowzy-api` cũ không còn tồn tại). Đây là kiểm chứng source và môi trường Docker tạm trên máy local, chưa phải kết quả triển khai lên DigitalOcean.

## Các lỗi và cấu hình đã xử lý

| Hạng mục | Thay đổi | Bằng chứng |
|---|---|---|
| Export XLSX lỗi thiếu font trên Linux | Cài DejaVu/fontconfig trong image | Export mentor meeting, bảng điểm instructor và feedback admin mở được bằng NPOI từ HTTP response trong image production |
| Logout mất hiệu lực sau restart | Lưu digest access token vào PostgreSQL qua migration V34, kiểm tra HTTP và STOMP | Token cũ bị 401 sau restart container thật; fresh host kiểm tra cùng DB cũng từ chối |
| Token mới có thể trùng token vừa logout trong cùng giây | Thêm claim `jti` ngẫu nhiên, giữ các claim và envelope đã có | Đăng nhập lại có token khác và gọi `/api/auth/me` thành công |
| CORS/WebSocket origin không bị giới hạn thực tế | Danh sách origin chính xác từ cấu hình; kiểm tra trước WebSocket upgrade | Origin được cho phép qua; origin khác bị chặn; STOMP CONNECT từ chối token đã thu hồi |
| Health luôn UP dù DB hỏng | Kiểm tra PostgreSQL, trả 503/DOWN khi không kết nối được | Test tắt kết nối DB và bật lại; test image khi PostgreSQL dừng |
| Backup không truyền CA cho libpq | Chuyển Root Certificate, SSL mode và timeout sang biến của tiến trình | `pg_dump` và `pg_restore` thực qua VerifyFull dùng CA tự tạo cho DB test |
| Dùng cấu hình local để deploy | Compose production riêng, API non-root/read-only, volume upload/backup, Caddy, log rotation, không publish 8080 | Compose/Caddy validate thành công; image chạy Production với cùng user/read-only và volume ghi được |
| Secret/cấu hình phát triển lọt build context | Mở rộng ignore cho dotenv, development settings, private keys và dumps | Image build thành công từ context đã loại các file này |
| Forwarded headers bị giả mạo | Chỉ tin IP proxy cụ thể và loopback framework mặc định | Test HTTPS forwarding từ proxy; địa chỉ khác không được đổi scheme |
| Swagger gọi nhầm localhost khi deploy | Server URL `/`, giữ nguyên operations/schemas của oracle | 7 test về contract và route pass sau thay đổi |

## Kiểm thử đã chạy

- `dotnet build Flowzy.sln --no-restore`: thành công. Repo vẫn có các cảnh báo analyzer/nullability đã có; không có compile error.
- Toàn bộ `dotnet test Flowzy.sln`, bật bài test image: **211 passed, 0 failed, 0 skipped**. File kết quả: `tests/Flowzy.Tests/TestResults/production-verification.trx`.
- Sau sửa URL Swagger: **7 passed, 0 failed, 0 skipped** trong `production-swagger.trx`.
- Image cuối đã build lại và chạy lại bài test production thành công: **1 passed, 0 failed, 0 skipped**, file `production-final-image.trx`. Image ID: `sha256:ba46b87abfd360c7992affd26481a1a9c9cff002b2ac8af255fd7b2e52e525d6`.
- Bài test image: tự tạo PostgreSQL 16, CA/server certificate, network và volume riêng; dùng `flowzy-api:production-test`. Không dùng database ứng dụng, tài khoản SU26 hay volume thật. Resource tạm do Testcontainers quản lý và dọn sau test.
- `git diff --check`: không có lỗi whitespace. V1–V33 SQL gốc không bị sửa; chỉ thêm V34.
- `docker compose --env-file .env.production.example -f compose.production.yml config --quiet`: thành công với giá trị kiểm tra không bí mật cho các biến bắt buộc.
- `caddy validate`: cấu hình hợp lệ, reverse proxy `api:8080`.

211 test .NET không đồng nghĩa đã port toàn bộ baseline 861 test Java. Đây là kết quả regression của bộ .NET hiện có, gồm kiểm thử production mới.

## Phạm vi triển khai còn lại

- Điền domain FE/API, Google client ID, JWT secret production, private PostgreSQL endpoint/user/password và CA thực tế.
- Tạo database/user và quyền migration trên DigitalOcean; kiểm tra firewall, DNS, chứng chỉ HTTPS và Google login từ FE thật sau deploy.
- Cấu hình backup ngoài Droplet. Backup nội bộ và TLS đã sửa/test; upload tự động lên Spaces chưa tích hợp và chưa được kiểm chứng.
- Chỉ chạy một instance API: queue import/backup vẫn ở tiến trình; Redis, Spaces nghiệp vụ, SendGrid và dịch vụ AI chưa được tích hợp trong đợt này.
- Source sửa xong chưa tự thay container ứng dụng local hiện có và chưa deploy lên cloud.

Các bước thực hiện nằm trong [PRODUCTION_DEPLOYMENT.md](PRODUCTION_DEPLOYMENT.md).
