# Flowzy production: DigitalOcean, PostgreSQL 16 và HTTPS

Hướng dẫn này dùng `compose.production.yml`, gồm một API và một Caddy reverse proxy. PostgreSQL là dịch vụ managed bên ngoài. Không dùng DigitalOcean Load Balancer hoặc Vercel Pro. Caddy chạy trong Docker và chuyển tiếp tới `api:8080`; không cài thêm một Caddy/Nginx khác chiếm cổng 80/443 trên cùng máy.

## Những thay đổi đã thực hiện trong source

- Image cài DejaVu/fontconfig để các export XLSX dùng NPOI chạy trên Linux.
- Production chạy API bằng UID/GID 1654, root filesystem chỉ đọc, `/tmp` tạm và hai volume ghi được cho upload/backup. Image vẫn hỗ trợ chế độ local hiện tại; cấu hình user/read-only nằm trong Compose production.
- Migration V34 thêm `revoked_access_token`: chỉ lưu SHA-256 của access token cùng thời điểm hết hạn. V1–V33 gốc giữ nguyên. Logout được kiểm tra từ PostgreSQL ở cả HTTP và STOMP CONNECT; cleanup xóa bản ghi đã hết hạn mỗi giờ. JWT thêm claim `jti` để hai lần đăng nhập cùng giây không sinh token giống nhau; các claim `sub`, `role`, `iat`, `exp` và JSON trả FE được giữ.
- CORS và WebSocket chỉ nhận origin nằm trong cấu hình. Production yêu cầu HTTPS, không wildcard, không path hoặc dấu `/` cuối. CORS expose `Content-Disposition` để FE đọc tên file tải xuống.
- `/actuator/health` trả HTTP 200 + `{"status":"UP"}` khi DB kết nối được; HTTP 503 + `{"status":"DOWN"}` khi không kết nối được. Không trả connection string hay chi tiết lỗi database cho người gọi.
- API chỉ tin `X-Forwarded-For`/`X-Forwarded-Proto` từ proxy đã cấu hình (và loopback mặc định của ASP.NET). Compose ấn định IP cho Caddy trong network riêng của stack.
- Swagger/OpenAPI dùng server URL `/` để `Try it out` gọi cùng origin với trang đang mở, bao gồm domain HTTPS production; không còn cố định localhost hoặc gọi nhầm server Java.
- Production từ chối JWT key sai Base64/dưới 32 byte, DB không dùng `VerifyFull`, các giá trị demo đã biết và AllowedHosts thiếu/rộng. Tạo khóa ngẫu nhiên riêng bằng trình quản lý secret hoặc `openssl rand -base64 48`.
- `pg_dump`/`pg_restore` nhận CA trong `Root Certificate` của connection string và timeout kết nối; không còn phụ thuộc vào việc tự thêm `PGSSLROOTCERT` bên ngoài.
- Build context loại `.env*`, `appsettings.Development.json`, private key và dump. Secret production phải nằm ngoài source dù đã có ignore.

## 1. Chuẩn bị DigitalOcean

1. Tạo Droplet API Ubuntu 24.04, SSH key, monitoring, cùng region/VPC với PostgreSQL.
2. Firewall: SSH 22 chỉ từ IP quản trị; 80/443 từ Internet. API không publish 8080 ra host; PostgreSQL không nằm trong Compose production.
3. Tạo Managed PostgreSQL 16 Standard Edition. Chỉ cho Droplet API vào Trusted Sources; chọn VPC/private hostname.
4. Tạo database `flowzy` và tài khoản riêng. Tài khoản chạy app cần quyền migration (tạo bảng/index/function/trigger), đọc/ghi dữ liệu và backup. Runner hiện chạy migrations lúc startup; chưa tách migration sang tài khoản riêng.
5. Tải CA certificate của cluster. Dùng connection endpoint trực tiếp, chưa dùng transaction-mode connection pool cho migration/backup.
6. Tạo DNS A `api.<domain>` tới public IP Droplet. Chỉ tạo AAAA khi thực sự cấu hình IPv6.

Tham khảo: [DigitalOcean PostgreSQL TLS](https://docs.digitalocean.com/products/databases/postgresql/how-to/connect/), [Trusted Sources](https://docs.digitalocean.com/products/databases/postgresql/how-to/secure/), [Docker Engine Ubuntu](https://docs.docker.com/engine/install/ubuntu/).

## 2. Chuẩn bị cấu hình trên Droplet

Cài Docker Engine + Compose plugin. Đưa source đã kiểm tra lên server. Không sao chép `.env` local hoặc database test SU26 vào production tự động.

Lưu bản cấu hình điền từ `.env.production.example` tại `/etc/flowzy/production.env`; đặt quyền 600. Lưu CA tại `/etc/flowzy/ca.crt`, quyền 644 (CA là chứng chỉ công khai). Docker cần đọc được file từ đường dẫn này.

| Biến | Giá trị |
|---|---|
| `API_DOMAIN` | Hostname, ví dụ `api.example.com`, không gồm `https://` |
| `FRONTEND_ORIGINS` | Ví dụ `https://flowzy-frontend.vercel.app`; nhiều origin phân cách bằng dấu phẩy |
| `DATABASE_CONNECTION_STRING` | Npgsql connection string với private hostname, DB/user/password thực, `SSL Mode=VerifyFull;Root Certificate=/etc/flowzy/ca.crt` |
| `POSTGRES_CA_CERT` | Đường dẫn file CA trên host, ví dụ `/etc/flowzy/ca.crt` |
| `JWT_SECRET_KEY` | Khóa ngẫu nhiên riêng, Base64, ít nhất 32 byte sau giải mã |
| `GOOGLE_CLIENT_ID` | Cùng Google Web Client ID với frontend |
| `ADMIN_EMAIL`, `ADMIN_PASSWORD` | Chỉ dùng bootstrap admin chưa tồn tại; đăng nhập đổi mật khẩu xong thì bỏ hai giá trị và recreate API |
| `FLOWZY_IMAGE_TAG` | Tag có phiên bản để giữ image trước khi cập nhật |
| `FLOWZY_SUBNET`, `FLOWZY_PROXY_IP` | Mặc định `172.30.60.0/24` và `172.30.60.2`; đổi đồng bộ nếu trùng mạng đang dùng |

Admin seed không thay mật khẩu tài khoản đã tồn tại. Nếu database mới và không cấu hình admin, hệ thống không tự có người quản trị.

Connection string dùng cú pháp Npgsql, không phải URI `postgresql://...`. Password có dấu `;` hoặc dấu nháy cần escape theo Npgsql. File dotenv hỗ trợ bọc toàn bộ giá trị bằng nháy đơn để tránh Compose thay thế ký tự `$`; không đưa secret vào terminal history.

## 3. Build và chạy

Chạy trong thư mục source trên server:

```bash
sudo docker compose --env-file /etc/flowzy/production.env -f compose.production.yml config --quiet
sudo docker compose --env-file /etc/flowzy/production.env -f compose.production.yml build api
sudo docker compose --env-file /etc/flowzy/production.env -f compose.production.yml up -d
sudo docker compose --env-file /etc/flowzy/production.env -f compose.production.yml ps
sudo docker compose --env-file /etc/flowzy/production.env -f compose.production.yml logs --tail=100 api
```

Luôn chỉ định file production. Không ghép thêm `docker-compose.yml` local. Dùng `config --quiet` để kiểm tra mà không in secrets đã nội suy. `up` chạy V1–V34 nếu database rỗng, hoặc chỉ các migration thiếu nếu đã chạy trước đó. Flyway history chỉ được nhận cho 33 migration chung với Java; V34 luôn là migration Flowzy riêng.

Caddy đợi API healthy trước khi khởi động và quản lý HTTPS cho `API_DOMAIN`. Persistent volumes giữ certificate Caddy, uploads và backups khi recreate container. Không dùng `down -v` khi cập nhật vì nó xóa các volume của stack.

```bash
curl --fail https://api.example.com/actuator/health
```

Kết quả đúng: `{"status":"UP"}`. Sau đó đăng nhập admin, đổi mật khẩu theo gate, gọi API có quyền và mở Swagger ở `/swagger-ui.html`. Health là kiểm tra DB availability, không thay thế kiểm tra toàn bộ nghiệp vụ. Caddy chuyển tiếp WebSocket `/ws`; client vẫn dùng STOMP.

## 4. Kết nối frontend

```dotenv
NEXT_PUBLIC_API_BASE_URL=https://api.example.com
NEXT_PUBLIC_WS_URL=wss://api.example.com/ws
NEXT_PUBLIC_GOOGLE_CLIENT_ID=<same-client-id-as-backend>
```

Build/deploy lại FE sau khi đổi biến. Google OAuth client phải cho phép JavaScript origin của FE và backend phải dùng cùng client ID. Email đăng nhập vẫn cần có tài khoản ACTIVE trong database theo nghiệp vụ hiện tại.

## 5. Backup, cập nhật và khôi phục

- Lập lịch backup bằng màn hình/API admin. Backup nằm trong volume `backups`, không tự tải lên Spaces.
- Managed PostgreSQL backup và bản sao ngoài Droplet cần được cấu hình ở hạ tầng. Trước khi phục vụ dữ liệu thật, thiết lập bản sao ngoài máy (ví dụ Spaces private với công cụ upload/backup đã kiểm tra) và kiểm tra tải/restore từ bản sao đó. Chưa có Spaces key nên source này không tự đồng bộ offsite.
- Trước cập nhật: lưu tag image trước, tạo backup, chờ import/backup đang chạy hoàn tất, rồi build tag mới và recreate API. Stack này có thể gián đoạn ngắn; frontend cần reconnect WebSocket.
- Chỉ chạy **một instance API**: import/backup worker vẫn là queue trong tiến trình, có đánh dấu job bị ngắt khi startup. Việc làm blacklist bền vững không đồng nghĩa toàn bộ app đã hỗ trợ scale nhiều replica.
- Restore ghi đè trạng thái DB bằng nội dung dump. Thử restore trên database riêng trước. Khi khôi phục production về snapshot cũ, đổi JWT key và thu hồi refresh token theo quy trình quản trị, vì snapshot có thể khôi phục trạng thái xác thực cũ.
- Rollback image không tự rollback SQL schema. V34 là bảng bổ sung, có thể giữ lại khi xử lý rollback; không xóa bảng/journal thủ công trên production. Bản cũ vẫn có lỗi blacklist RAM nên chỉ rollback trong quá trình khắc phục sự cố có kiểm soát.

## 6. Kiểm thử có thể chạy lại

Kiểm thử .NET dùng PostgreSQL Testcontainers riêng, không lấy connection string từ DB người dùng. Để chạy cả bài test image production:

```powershell
docker build -t flowzy-api:production-test .
$env:FLOWZY_PRODUCTION_TEST_IMAGE = 'flowzy-api:production-test'
dotnet test Flowzy.sln --logger 'trx;LogFileName=production-verification.trx'
```

Trên Bash, thay dòng gán biến bằng `export FLOWZY_PRODUCTION_TEST_IMAGE=flowzy-api:production-test`.

Bài test image tạo CA và PostgreSQL bật TLS riêng, chạy đúng image API ở Production/non-root/read-only, kiểm tra V34/seed, export XLSX mentor/instructor/feedback, logout qua restart thật, `pg_dump`/`pg_restore` qua VerifyFull và HTTP 503 khi DB tắt. Nếu không đặt biến image, bài test này được ghi SKIPPED rõ ràng; các test khác vẫn chạy.

Kết quả trên môi trường tạm không xác nhận DNS, TLS public, firewall, quyền user và thông số cluster DigitalOcean thực tế. Những bước đó được kiểm tra khi điền cấu hình server.

## Dịch vụ chưa tích hợp

Redis/Valkey, Spaces trong luồng upload nghiệp vụ, SendGrid và AI chatbot/gợi ý problem bank vẫn là công việc riêng. Bộ sửa production này không yêu cầu mua Redis và không tự gửi email hoặc gọi AI.
