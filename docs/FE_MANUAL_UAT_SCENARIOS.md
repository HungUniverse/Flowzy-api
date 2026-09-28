# Flowzy — kịch bản test nghiệp vụ bằng thao tác trên FE

Ngày soạn: 28/09/2026. FE được đối chiếu: `D:/Flowzy-FE/Flowzy-frontend`.

Tài liệu này dành cho người test trực tiếp bằng trình duyệt: đăng nhập, chọn menu, nhập form, bấm nút và kiểm tra kết quả trên màn hình. **Không cần Postman, Swagger, token hay câu lệnh SQL để thực hiện luồng chính.**

Các kết quả bên dưới là **kết quả mong đợi**, chưa phải kết quả đã chạy PASS. Kịch bản được đối chiếu với component FE và tài liệu nghiệp vụ Java; chưa thực hiện toàn bộ các bước trên trình duyệt. Nếu giao diện hiện tại khác mô tả hoặc trả lỗi, ghi FAIL/BLOCKED, không sửa dữ liệu thật để vượt qua.

Quy mô: **22 nhóm kịch bản, 143 case**, kèm bản đồ liên hệ đến 37 controller Java. Không đồng nghĩa đã bao phủ mọi API bằng UI.

## 1. Bắt đầu và nguyên tắc an toàn

1. Mở [Flowzy local — Login](http://localhost:3000/login). Dùng nhất quán `localhost`, không luân phiên với `127.0.0.1` vì phiên đăng nhập và Google origin có thể khác nhau.
2. Chỉ dùng FE local đang trỏ đến BE local cổng 8080. Không thực hiện bài test phá dữ liệu trên bản Vercel/production.
3. Dùng **browser profile riêng** cho Admin, Instructor, Mentor, S1 và S2; hoặc đăng xuất rồi đổi tài khoản tuần tự. Hai tab thường trong cùng profile dùng chung phiên. Nhiều cửa sổ ẩn danh cùng trình duyệt cũng có thể dùng chung phiên.
4. Mỗi bước thành công phải kiểm tra lại bằng cách đóng/mở màn hình hoặc F5. Với bước liên quan hai người, phải kiểm tra ở cả hai tài khoản.
5. Chỉ tạo/sửa/xóa dữ liệu có tiền tố UAT do bạn tạo. Không reset mật khẩu, disable, xóa hoặc archive các tài khoản mentor/student thật đã import.
6. **Đóng kỳ và archive chạy cuối. Restore chỉ test ở môi trường DB dùng một lần, không chạy trên DB Flowzy hiện tại.** Không có bước mở lại kỳ trong luồng chuẩn này.

Ở lần kiểm tra local trước đó: có 1 admin, 101 mentor, 16 student, chưa có instructor/nhóm/kỳ. Đây là mốc tham khảo, không phải dữ liệu vừa được đọc lại cho tài liệu này. Batch mentor #1 đã hoàn thành 101/105 dòng; student-account #2 hoàn thành 15/15 dòng. Không import lại hai file gốc chỉ để chuẩn bị test.

## 2. Bộ dữ liệu bạn tự tạo trên giao diện

Thay `0928a` bằng hậu tố riêng của lượt chạy nếu đã tồn tại. Mật khẩu do bạn chọn và lưu riêng, không ghi vào báo cáo/ảnh chụp.

| Ký hiệu | Vai trò / tên hiển thị | Email mẫu | Mã profile | Mục đích |
|---|---|---|---|---|
| A | Admin hiện có | Dùng admin local của bạn | Có sẵn | Quản trị |
| I1 | INSTRUCTOR / UAT Instructor 1 | uat.i1.0928a@example.com | UATI10928A | Giảng viên nhóm G1 |
| I2 | INSTRUCTOR / UAT Instructor 2 | uat.i2.0928a@example.com | UATI20928A | Test sai phạm vi và claim cạnh tranh |
| M1 | MENTOR / UAT Mentor 1 | uat.m1.0928a@example.com | UATM10928A | Mentor nhóm G1/G2 |
| M2 | MENTOR / UAT Mentor 2 | uat.m2.0928a@example.com | UATM20928A | Test không được giao nhóm |
| S1 | STUDENT / UAT Student 1 | uat.s1.0928a@example.com | UATS10928A | Leader G1 |
| S2 | STUDENT / UAT Student 2 | uat.s2.0928a@example.com | UATS20928A | Thành viên G1 |
| S3 | STUDENT / UAT Student 3 | uat.s3.0928a@example.com | UATS30928A | Xin vào/rời G1, sau đó leader G2 |
| S4–S7 | STUDENT / UAT Student 4–7 | Cùng quy tắc đặt email | Cùng quy tắc mã | Tùy chọn: test tối đa 6 người |
| X | STUDENT / UAT Disposable | uat.delete.0928a@example.com | UATDEL0928A | Reset, khóa, xóa; không cho vào nhóm chính |

Email `example.com` chỉ dùng đăng nhập mật khẩu; không thể dùng để đăng nhập Google. Bài Google cần tài khoản Google thật do bạn kiểm soát và email trùng tài khoản ACTIVE đã được tạo/import.

| Dữ liệu | Giá trị dùng xuyên suốt |
|---|---|
| TERM | `UAT0928A`, chỉ tạo khi không có kỳ OPEN; nếu đang có kỳ thật, dừng phần tạo kỳ và yêu cầu môi trường UAT riêng, không đóng kỳ thật |
| Course | `EXE101` — chọn đúng giá trị FE cho phép |
| G1 | `UAT Flowzy Team A`, leader S1, thành viên S2 |
| G2 | `UAT Flowzy Team B`, leader S3; tạo sau khi S3 rời G1 |
| Domain | Code `UATD0928A`, name `UAT Software` |
| P1 | Official problem code `UATP0928A`, title `UAT Campus Booking` |
| Board | `UAT Sprint 1` |
| T1 / T2 | `UAT Thiết kế màn hình` / `UAT Kiểm tra đặt lịch` |
| MS1 | `UAT Prototype`, weight 40, max score 10, position 1 |
| MS2 | `UAT Final`, weight 60, max score 10, position 2 |

## 3. Thứ tự thực hiện

| Chặng | Các mục | Kết quả trước khi đi tiếp |
|---|---|---|
| Chuẩn bị | U01–U05 | Tài khoản đăng nhập được; có kỳ OPEN và dữ liệu import kiểm chứng được |
| Nhóm | U06–U08 | G1 gồm S1/S2, I1 và M1 được giao; G2 do S3 làm leader |
| Đề tài và công việc | U09–U11 | Có đề tài, proposal đã review, task có assignee/checklist/comment |
| Mentor và chấm điểm | U12–U15 | Có cuộc họp và minh chứng; hai milestone được đồng thuận và chấm |
| Kiểm tra chéo | U16–U18 | Notification, dashboard, scope, cạnh tranh |
| Kết thúc kỳ | U19–U20 | Kỳ đóng, feedback kiểm tra xong; archive tùy chọn |
| Vận hành / lịch sử | U21–U22 | Backup an toàn; legacy chỉ test phần FE thực sự hỗ trợ |

Muốn test nhanh: chạy các case “H” trước. Case “N” là nhánh lỗi/biên, chạy trước khi đóng kỳ nếu có ghi dữ liệu. Mỗi ô `☐` ban đầu là NOT RUN; thay bằng PASS/FAIL/BLOCKED khi đã có bằng chứng. Nếu tiền điều kiện lỗi, các bước phụ thuộc ghi BLOCKED thay vì FAIL dây chuyền.

## U01. Admin tạo tài khoản test

Người test: A. Mở **Users** tại `/admin/users`.

| ID | Thao tác trên FE | Kết quả nhìn thấy / dữ liệu cần giữ | KQ |
|---|---|---|---|
| U01-H1 | Bấm **Create user** → Email của I1 → Role `INSTRUCTOR` → Initial password → Instructor code, Full name; điền thêm các trường bắt buộc được form đánh dấu → **Save user**. | Modal đóng khi lưu thành công. Search email thấy đúng một tài khoản, đúng role và thông tin profile. | ☐ |
| U01-H2 | Làm tương tự cho I2, M1/M2 với role MENTOR, S1/S2/S3/X với role STUDENT. | Từng tài khoản có mã riêng, ACTIVE. Không đổi các tài khoản đã import thật. | ☐ |
| U01-H3 | Lọc **Role**, **Status**, nhập tên/email/mã vào **Search**; xóa bộ lọc sau khi kiểm tra. | Danh sách chỉ có kết quả phù hợp. F5 không làm mất tài khoản vừa tạo. | ☐ |
| U01-H4 | Bấm **Edit** của X → đổi Full name/Phone → **Save user** → tìm lại. | Lưu đúng tài khoản X; email/role của người khác không thay đổi. | ☐ |
| U01-N1 | Tạo lại đúng email I1; sau đó thử thiếu Email/Full name/mã theo trường bắt buộc và mật khẩu dưới 6 ký tự. | Form hoặc BE báo lỗi rõ ràng, không có tài khoản trùng/thiếu hồ sơ. Không coi “nút không bấm được” là đã kiểm tra validation BE. | ☐ |
| U01-N2 | Với X: **Reset** → **New password** → **Reset password**; thử login X bằng mật khẩu cũ rồi mới. | Mật khẩu cũ bị từ chối, mật khẩu mới đăng nhập được; nếu hiện yêu cầu đổi mật khẩu thì hoàn thành U02. | ☐ |
| U01-N3 | **Edit** X → Status INACTIVE → lưu; thử đăng nhập lại X. Sau đó A bật lại ACTIVE. | X không vào được khi INACTIVE. Sau khi bật lại, có thể đăng nhập đúng điều kiện. | ☐ |
| U01-N4 | Sau các bài test X, **Delete** X → thử Cancel trước, rồi mở lại và xác nhận **Delete user**. | Cancel không đổi dữ liệu; xác nhận loại X khỏi danh sách theo nghiệp vụ. Không dùng X cho bước sau. | ☐ |

## U02. Đăng nhập mật khẩu, đổi mật khẩu bắt buộc, đăng xuất

Người test: lần lượt I1, M1, S1, S2, S3 và các actor phụ đã tạo. Mở `/login`.

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U02-H1 | Nhập email/mật khẩu ban đầu → **Sign in**. | Đúng tài khoản. Tài khoản được tạo với cờ yêu cầu đổi mật khẩu sẽ đến `/change-password`, không vào nghiệp vụ ngay. | ☐ |
| U02-H2 | Nhập **Current password**, **New password**, **Confirm new password** → **Update password**. | Hết yêu cầu đổi mật khẩu; vào workspace đúng vai trò. Ghi riêng mật khẩu mới của actor. | ☐ |
| U02-H3 | Mở **Profile** kiểm tra email/role/tên. Bấm **Sign out** từ menu tài khoản. Bấm Back rồi F5 hoặc mở lại URL workspace. | Không tiếp tục xem/thao tác trang bảo vệ bằng phiên đã đăng xuất. Login mới vào được. | ☐ |
| U02-N1 | Thử email không tồn tại hoặc mật khẩu sai, bỏ trống trường bắt buộc. | Có lỗi trên form, không chuyển vào workspace và không hiển thị dữ liệu người khác. | ☐ |
| U02-N2 | Khi đang bị buộc đổi mật khẩu, gõ trực tiếp `/student/groups` với S1. Sau đó thử current password sai hoặc hai ô mật khẩu mới khác nhau. | Không vượt qua màn đổi mật khẩu; các dữ liệu sai không được lưu. | ☐ |
| U02-N3 | Tại Login bấm **Forgot password?**. | Hiện hướng dẫn liên hệ admin; FE hiện không có luồng gửi email/OTP tự khôi phục. Không chờ thư reset. | ☐ |

## U03. Google login và hồ sơ cá nhân

Không thay đổi Google Cloud trong bài test này. Nếu nút Google không hoạt động do origin/cấu hình, ghi BLOCKED và xử lý cấu hình trước.

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U03-H1 | Tại Login, chọn Google account thật có email đã tạo/import và ACTIVE. | Đăng nhập đúng role/profile đã có trong hệ thống; không tự tạo tài khoản mới hoặc tự đổi role. Nếu bị yêu cầu đổi mật khẩu mà tài khoản Google không biết mật khẩu hiện tại, ghi lại vấn đề và nhờ admin xử lý đúng chính sách. | ☐ |
| U03-N1 | Nếu có Google account phụ chưa đăng ký, dùng account đó để thử. | Báo `Account is not registered in the system`, không vào workspace. Không tạo account phụ chỉ để làm test nếu không có nhu cầu. | ☐ |
| U03-N2 | Nếu gặp `Google token audience is invalid`, chụp lỗi và dừng case. | Đây là lỗi client ID/audience, không kết luận do chưa import email. Không nới kiểm tra token để vượt qua. | ☐ |
| U03-H2 | Với S1: **Profile** → Personal information → sửa Phone/Address → **Save profile** → F5. Lặp với Company/Expertise của M1, Department của I1. | Các trường thuộc đúng role được lưu, các trường không chỉnh vẫn giữ. Email/Role/Profile code không bị đổi thành thông tin người khác. | ☐ |
| U03-H3 | Với tài khoản test biết mật khẩu: Profile → **Change password** → nhập đủ 3 ô → lưu; đăng xuất/đăng nhập lại. | Mật khẩu mới dùng được, mật khẩu cũ không dùng được. Không ghi mật khẩu trong ảnh/báo cáo. | ☐ |

## U04. Tạo kỳ học

Người test: A → **Terms** (`/admin/terms`).

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U04-H1 | Kiểm tra danh sách trước. Nếu chưa có kỳ OPEN, bấm **Create term** → Term code `UAT0928A` → xác nhận tạo. | Có đúng kỳ test, trạng thái OPEN, chưa có nhóm. | ☐ |
| U04-N1 | Khi đã có kỳ OPEN, quan sát nút **Create term**. | Bị khóa và có giải thích phải đóng kỳ đang OPEN; không tạo hai kỳ OPEN từ UI. | ☐ |
| U04-H2 | Mở form **Create group** của S1 nhưng chưa lưu. | Dropdown Term có kỳ vừa tạo. Thoát form bằng Cancel rồi thực hiện U06 sau. | ☐ |

Nếu đang có kỳ OPEN là dữ liệu thật: **BLOCKED phần setup UAT**, không đóng kỳ đó chỉ để test. Kỳ học khác nhau không thể cùng OPEN theo nghiệp vụ hiện tại.

## U05. Import bằng màn hình Admin

Người test: A → **Imports** (`/admin/imports`). Đây là nhánh riêng, không thay thế bộ S1/S2/S3 đã tạo. Chỉ dùng bản sao template và các dòng UAT mới. Giữ nguyên header, tên sheet, cấu trúc template tải từ ứng dụng.

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U05-H1 | Bấm **Mentor template**, **Student account template**, **Student template**, **Problem bank template**. | Từng file tải được và mở được; không phải file HTML/JSON lỗi đổi đuôi xlsx. | ☐ |
| U05-H2 | **Batch lookup** → nhập Batch ID `1` → **View status**, **View errors**; lặp với `2`, nếu dữ liệu vẫn còn từ lần import trước. | Batch 1: 105 tổng / 101 thành công / 4 lỗi; batch 2: 15/15 thành công. Nếu dữ liệu đã thay đổi, ghi số thực tế và nguồn thay đổi. Không upload lại file gốc. | ☐ |
| U05-H3 | Bản sao mentor template: tạo 1 dòng mentor UAT hợp lệ + 1 dòng email sai. Target **Mentors** → chọn File → nút Import → đợi job kết thúc → **Inspect batch**. | Có batch ID/trạng thái cuối; số thành công/lỗi đúng 2 dòng đã chuẩn bị; lỗi chỉ ra dòng/trường. Trong Users có mentor hợp lệ, không có account từ dòng sai. | ☐ |
| U05-H4 | Sau khi batch mentor kết thúc, dùng bản sao **Student account template** với 1 email/mã UAT mới. Target **Student accounts** → chọn file → Import. | Account/profile được tạo. `GroupName` là lớp sinh viên; không tự tạo project group hay membership. Kiểm tra Users và Groups để xác nhận. | ☐ |
| U05-H5 | Nhánh riêng: từ **Student template**, giữ cấu trúc roster nhóm, dùng kỳ test và một nhóm phụ/mã SV mới; chọn Target **Students** → Import. | Kiểm tra cả Users lẫn Groups: nhóm, leader, member và mentor nếu có trong file. Đọc các cảnh báo thay vì chỉ nhìn tổng thành công. Không dùng nhầm template Student accounts. | ☐ |
| U05-H6 | Dùng **Problem bank template**, thêm đề tài mã UAT mới đúng cấu trúc → Target **Problem bank** → Import. | Job xong; vào Problem Bank tìm đúng đề tài, domain, nội dung. Chưa có file đúng mẫu thì ghi BLOCKED, không coi batch rỗng là PASS. | ☐ |
| U05-N1 | Thử upload chưa chọn file; rồi trên file phụ thử header sai/trùng mã đã import. | Có lỗi phù hợp; không tạo bản ghi trùng. Có thể lỗi cấp file hoặc cấp dòng, phải ghi đúng trạng thái thực tế. | ☐ |
| U05-H7 | Trong **Batch errors**, lọc message/code/field/row; chuyển trang nếu đủ dữ liệu; F5 và tra cứu lại Batch ID. | Kết quả batch và lỗi vẫn đọc được; không mất toàn bộ báo cáo khi đổi trang. | ☐ |

Google login sau import chỉ kiểm tra được bằng email Google thật có quyền truy cập. Email demo không chứng minh được luồng này.

## U06. Tạo nhóm và mời thành viên

Người test: S1/S2. **Groups** (`/student/groups`).

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U06-H1 | S1: **My Groups** → **Create group** → Term = TERM, Course code = EXE101, Group name = G1, Project name = `UAT Campus Project`, Research domain = `Software`, Target grade = 8; mô tả ý tưởng ngắn → **Save group**. | My Group Workspace hiển thị G1; S1 là Leader, Members có S1; trạng thái Recruiting. Ghi lại groupNo được hệ thống cấp. | ☐ |
| U06-H2 | S1: **Edit group** → sửa mô tả/project name; thêm Recruitment needs với Role lấy từ dropdown và Quantity hợp lệ → Save group. | Dữ liệu và nhu cầu tuyển còn đúng sau F5. Không tự gõ role ngoài danh mục. | ☐ |
| U06-H3 | S1: **Invite member** → **Invite by code or email** nhập mã/email S2, Message `UAT invitation` → **Send**. | S1 thấy lời mời PENDING ở **Sent Invitations**. S2 chưa thành member ngay khi chỉ mới gửi. | ☐ |
| U06-H4 | S2: Groups → **Invitations** → đúng lời mời G1 → **Accept**, xác nhận nếu có. | S2 thấy G1 trong My Groups. S1 F5 thấy đúng hai thành viên S1/S2, không lặp membership. | ☐ |
| U06-N1 | S1 mời S3; S3 **Decline** và xác nhận nếu có. Mời lại bằng lời mời mới nếu UI cho phép; S1 bấm **Cancel** ở Sent Invitations → xác nhận **Cancel invitation** trước khi S3 nhận. | Decline/cancel không thêm thành viên. Không dùng lời mời terminal cũ để test accept thành công. | ☐ |
| U06-N2 | S2 mở workspace G1 và thử tìm Edit group/Invite member. | Không có quyền leader; không tự đổi nhóm hoặc mời người khác bằng UI. | ☐ |
| U06-N3 | S1 vào Discover Groups → Create group và thử tạo nhóm khác cùng TERM/EXE101 khi đang là member G1. | Bị chặn; không có hai membership cùng kỳ/môn. Dùng thao tác bình thường, không chỉnh request. | ☐ |

## U07. Xin vào nhóm, khóa, chuyển leader và rời nhóm

Chạy trước khi lưu contribution/chấm điểm. Kết thúc mục này, G1 phải trở lại **S1 leader + S2 member**.

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U07-H1 | S3: Groups → **Discover Groups** → tìm G1 theo Group name → chọn card → nhập lời nhắn → **Request to join**. | Có request PENDING; S3 chưa là member. S1 thấy request ở **Join Requests**. | ☐ |
| U07-N1 | S3 **Cancel request**; tạo request mới, S1 **Reject**. | Mỗi nhánh kết thúc đúng trạng thái, không tăng Members. | ☐ |
| U07-H2 | S3 tạo request mới; S1 **Approve**. | Cả S1/S3 thấy S3 trong nhóm, tổng 3 member. | ☐ |
| U07-H3 | S1 bấm **Lock group** → xác nhận. S3 kiểm tra Discover/invite bằng phiên khác. Sau đó S1 **Unlock group**. | Locked hiện Closed (khóa tuyển thành viên, không phải đóng kỳ). Invite/remove bị khóa theo UI; không thêm người khi locked. Unlock cho quản lý membership trở lại. | ☐ |
| U07-N2 | S1 bấm **Leave group** khi vẫn còn S2/S3. | Báo `Transfer leadership before leaving this group.`; S1 không tự bỏ nhóm còn thành viên. | ☐ |
| U07-H4 | Trong **Members**, S1 bấm **Make leader** ở dòng S2 → xác nhận **Transfer leadership**. Kiểm tra cả hai phiên; sau đó S2 chuyển lại S1. | Chỉ có một leader; nút quản lý chuyển theo leader mới. Cuối bước S1 là leader. | ☐ |
| U07-H5 | S1 bấm **Remove** ở dòng S3 → xác nhận **Remove member**. S3 xin vào lại, S1 approve; S3 **Leave group** và xác nhận. | Cả remove lẫn leave bỏ S3 khỏi Members. F5 S3 không còn workspace G1 như thành viên. | ☐ |
| U07-H6 | S3 tạo G2 bằng Create group, cùng TERM/EXE101 sau khi đã rời G1. | Tạo thành công, S3 là leader G2. G1 vẫn chỉ S1/S2. | ☐ |
| U07-N3 | Tùy chọn trên nhóm phụ: thêm các student UAT cho đủ 6 người, người thứ 7 xin vào/nhận lời mời. | Không vượt 6 thành viên. Không đưa S1/S2 ra khỏi G1 để chuẩn bị bài quota. | ☐ |

## U08. Gán instructor/mentor và instructor tự nhận nhóm

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U08-H1 | A: **Groups** (`/admin/groups`) → Search groups tìm G1 → **Assign instructor** → tìm I1 → chọn → **Save**. | Current assignment là I1; sau F5 không mất. | ☐ |
| U08-H2 | Cùng G1: **Assign mentor** → tìm M1 → chọn → Save. Lặp gán M1 cho G2. | S1/S2 thấy I1/M1 ở My Group Workspace. M1 thấy G1/G2 trong **Groups**; M2 không có chúng trong danh sách được giao. | ☐ |
| U08-H3 | I1: **Groups** (`/instructor/groups`) → lọc Open term = TERM, nhóm chưa có instructor G2 → **Claim this group** → **Claim group**. | G2 trở thành nhóm của I1; A và S3 kiểm tra thấy cùng instructor. I2 không thể claim lại G2. | ☐ |
| U08-N1 | A trên G2 phụ: mentor **Change** sang M2 → Save → kiểm tra M1/M2 → đổi lại M1. | Assignment không trùng; phạm vi nhóm của M1/M2 đổi đúng. Không làm trên G1 sau khi đã có cuộc họp/contribution để tránh đổi tiền điều kiện. | ☐ |
| U08-N2 | Trên nhóm phụ, bấm **Unassign** nếu UI cung cấp → Cancel; mở lại → **Confirm unassign** → gán lại. | Cancel giữ nguyên; xác nhận bỏ đúng người; UI thể hiện chưa được giao. Nếu không có thao tác unassign cho role đó, ghi không có UI, không giả định phải có. | ☐ |
| U08-H4 | A mở danh sách thành viên của G1 trong Groups; tìm S1 ở Users và mở thông tin Group memberships. | Tên nhóm/kỳ/môn/leader nhất quán giữa màn quản trị và màn student. | ☐ |

## U09. Domain và đề tài chính thức

Người test: A/S1/S2.

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U09-H1 | A: **Problem Bank** (`/admin/problems`) → **Domain Manager** → **New Domain** → Code/Name theo bộ dữ liệu → lưu. Mở Edit, sửa Description rồi lưu. | Domain ACTIVE, tìm được bằng Search domains; sửa tồn tại sau F5. | ☐ |
| U09-H2 | **Thesis Topics List** → **Create Topic** → Topic Title/Topic Code theo P1 → chọn Domain UAT → Difficulty Beginner → Status Active → mô tả bài toán và Expected Deliverables → **Save Topic**. | Danh sách có đúng P1; View details hiển thị các trường đã nhập. | ☐ |
| U09-H3 | S1: **Problems** → tìm `UAT Campus Booking`, lọc domain/difficulty nếu cần → **View Details** → **Select for Group**. | **Selected Problem** của G1 là P1; S2 mở cùng nhóm thấy cùng đề tài. | ☐ |
| U09-H4 | S1 mở P1 đang chọn → **Clear Selected Problem** → xác nhận → chọn lại P1. | Clear bỏ lựa chọn của nhóm, không xóa đề tài khỏi bank; chọn lại được. | ☐ |
| U09-N1 | S2 mở cùng chi tiết P1. | Không có quyền tự chọn/bỏ đề tài thay leader. | ☐ |
| U09-N2 | A tạo đề tài phụ rồi View details → **Status** Inactive và xác nhận. Student tìm lại. Bật lại Active sau test. | Đề tài INACTIVE không xuất hiện trong bank ACTIVE của student để chọn mới; khi bật lại thì tìm được. Không dùng P1 đang được chọn cho bài này. | ☐ |
| U09-N3 | A thử tạo domain/topic trùng code hoặc bỏ trống trường bắt buộc. | Lỗi rõ ràng; không có bản ghi trùng. | ☐ |
| U09-H5 | Trong View Details, đọc phần tiêu chí đánh giá nếu có. | Tên/mô tả/thang điểm được hiển thị từ danh mục. Đây là bài xem tiêu chí, không phải màn nhập điểm problem. | ☐ |

## U10. Đề xuất đề tài, chỉnh sửa và review

Người test: S1, I1, I2, A. G1 đã có I1; còn kỳ OPEN. Proposal có thể thay lựa chọn đề tài hiện tại của G1, cần kiểm tra Selected Problem sau mỗi thao tác.

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U10-H1 | S1: Problems → **Propose Custom Topic (n/3)** → Topic Title `UAT Proposal Review`, Select Domain UAT, Difficulty, mô tả, Expected Output → **Submit Proposal**. | Có proposal PENDING_REVIEW, mã do hệ thống sinh; nhóm tự chọn proposal vừa tạo. | ☐ |
| U10-H2 | S1 mở **Archived Problem Bank (n)** → proposal pending → **Edit** → đổi title/mô tả → **Save Changes**. | Lưu trên proposal cũ, không tạo thêm proposal. Tên “Archived Problem Bank” ở UI là nơi xem lịch sử proposal của nhóm, không có nghĩa tất cả đều ARCHIVED. | ☐ |
| U10-H3 | I1: **Problems** (`/instructor/problems`) → tìm G1 → **Review** đúng proposal → **Approve Proposal** → nhập nhận xét → **Save Review**. | S1 F5: đề tài được duyệt trở thành OFFICIAL/ACTIVE; không yêu cầu badge cuối cùng phải là APPROVED. | ☐ |
| U10-N1 | I2 mở Problems tìm proposal G1 trước khi I1 duyệt. | Không được review proposal ngoài nhóm I2 phụ trách; không đổi được kết quả của G1. | ☐ |
| U10-H4 | S1 tạo proposal thứ hai `UAT Proposal Reject`. I1 chọn **Reject Proposal**, thử bỏ nhận xét, sau đó nhập lý do và Save Review. | Thiếu lý do bị chặn. Có lý do: proposal REJECTED, selection liên quan được bỏ; lý do hiển thị khi FE có trường tương ứng. | ☐ |
| U10-H5 | S1 tạo proposal phụ chưa review → Archived Problem Bank → **Delete** → Cancel; mở lại và xác nhận. | Cancel giữ nguyên, xác nhận xóa đúng pending proposal; không xóa đề tài official đã duyệt. | ☐ |
| U10-H6 | Trên G2, S3 tạo proposal riêng. A vào Problem Bank → lọc pending → **Review** → quyết định và lưu. | Luồng admin review hoạt động; kết quả đồng bộ ở S3. Không dùng Status dropdown để thay thế bài review. | ☐ |
| U10-N2 | Khi nhóm đã đủ 3 proposal được tính trong danh sách, kiểm tra nút Propose; thử sửa/xóa proposal đã review. | FE hiển thị giới hạn 3/3; không cung cấp sửa/xóa như pending. Chỉ chạy nhánh quota trên nhóm phụ nếu còn cần proposal ở luồng chính. | ☐ |

## U11. Task board, người phụ trách, checklist và trao đổi

Người test: S1/S2. **Tasks** (`/student/tasks`) → chọn G1 bằng bộ chọn nhóm → **Group Kanban Board**.

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U11-H1 | Bấm dấu cộng có nhãn/tooltip **Add board** → Board name `UAT Sprint 1`, Description → **Create board** → chọn board này. | Board tồn tại sau F5; không lẫn task của board khác. | ☐ |
| U11-H2 | Ở cột **To Do**, bấm nút thêm task → Task Title T1, Description ngắn, Priority High, Due Date tương lai, Assign Task chọn S2 → xác nhận tạo. | T1 ở đúng board/cột, có S2 và deadline đúng giờ đã nhập. Tạo T2 cùng cột để test thứ tự. | ☐ |
| U11-H3 | S2: Tasks → **My Task List** → lọc/chọn đúng G1. | Có T1 được giao cho S2; task không giao S2 không tự xuất hiện như task của mình. | ☐ |
| U11-H4 | Mở T1 → bấm vùng tiêu đề/mô tả để sửa → **Save Changes**; đổi Priority/Due Date, chọn/bỏ assignee trong **Assignees**. | Đúng title/description/priority/deadline/assignee sau đóng/mở lại; My Task List cập nhật theo assignee mới. Cuối bước gán lại S2. | ☐ |
| U11-H5 | Kéo T1 To Do → **In Progress** → **In Review** → **Done**. Kéo T2 và đổi thứ tự hai task trong cùng cột. | Task ở đúng cột/vị trí sau F5; không bị nhân đôi hoặc biến mất. Kiểm tra cả phiên S2. | ☐ |
| U11-H6 | Mở T1 → checklist **Add item...** nhập `Kiểm tra form` → nút thêm; tích hoàn thành rồi bỏ tích, thêm item thứ hai và xóa item đó. | Tiến độ/tick cập nhật đúng; chỉ item được chọn bị xóa; trạng thái tồn tại sau F5. | ☐ |
| U11-H7 | Tab **Comments** → nhập `UAT comment của S1` → **Send Comment**. S2 đọc rồi thêm phản hồi của S2. | Mỗi comment đúng nội dung/người/thời gian, không lặp do bấm hai lần; thử gửi toàn dấu cách phải bị chặn. | ☐ |
| U11-H8 | Tác giả xóa comment test của mình bằng **Delete**. Mở tab activity trong cùng panel. | Comment bị xóa đúng quyền; lịch sử thể hiện các thao tác task với người thực hiện phù hợp. | ☐ |
| U11-H9 | Chuyển **Board / List / Timeline / Due Tasks**, lọc Search tasks, Priority, Assignee rồi xóa lọc. | Cùng một dữ liệu task, khác cách trình bày; bộ lọc không làm mất dữ liệu sau khi bỏ lọc. Timeline/Due Tasks cần task có deadline. | ☐ |
| U11-H10 | Tạo task phụ `UAT Archive Only` → mở → **Archive**, xác nhận nếu có. Trở lại board; tick **Archived**. | Mặc định không còn task phụ; tick Archived thấy lại task đã archive. Đây chưa phải thao tác restore. | ☐ |
| U11-N1 | Tạo task title rỗng hoặc deadline quá khứ. | Form/BE từ chối, không tạo task lỗi. Không bắt buộc UI cho tạo trực tiếp ở Done: create chỉ Backlog/To Do theo nghiệp vụ. | ☐ |
| U11-N2 | S3 chọn workspace G2 rồi vào Tasks. | Không nhìn thấy task G1 như task thuộc G2; đổi group không giữ nhầm nội dung panel cũ. | ☐ |

FE hiện chưa thấy nút sửa/xóa board, restore task, sửa comment, sửa tên/sắp thứ tự checklist trong các component đã đối chiếu. Không thêm các bước bấm nút không tồn tại; xem mục 5 để phân biệt phần chưa được bao phủ.

## U12. Mentor tạo lịch trống và student đặt lịch

Người test: M1/S1/S2/S3. M1 đã được gán G1/G2. Chọn ngày **tương lai**, giờ Việt Nam, ví dụ ngày mai 09:00–10:00 và 10:30–11:30. Mỗi slot đúng 60 phút, bắt đầu :00 hoặc :30, cùng ngày.

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U12-H1 | M1: **Availability** → **Add event** → Start/End → Google Meet link hợp lệ → Note `UAT slot 1` → **Save slot**. | Calendar có slot AVAILABLE đúng ngày/giờ. End có thể được tự tính, không cần sửa bằng cách khác. | ☐ |
| U12-H2 | Bấm slot AVAILABLE → **Edit slot** đổi Note; **Duplicate** sang khung giờ không trùng → Save slot. | Slot gốc sửa đúng, slot mới độc lập, không trùng lịch. | ☐ |
| U12-N1 | Thử slot trùng giờ; nhập link không phải Google Meet; thử giờ không :00/:30 hoặc thời lượng sai nếu input cho phép. | Lỗi rõ ràng hoặc control khóa giá trị sai; không thêm slot không hợp lệ. | ☐ |
| U12-H3 | S1: Groups → G1 → kéo xuống **Mentor meetings** → **Upcoming availability** → **Book this slot** → xác nhận. | **Meeting history** của G1 có cuộc hẹn SCHEDULED. Slot không còn sẵn để nhóm khác đặt. M1 nhìn thấy đúng G1 trong Meeting reports. | ☐ |
| U12-N2 | S2 mở cùng G1; S3/G2 thử chọn slot đã được S1 đặt. | S2 không có quyền booking như leader. G2 không đặt trùng cùng slot. | ☐ |
| U12-H4 | M1 mở một slot AVAILABLE phụ → **Cancel slot** và xác nhận. | Slot chuyển CANCELED/không còn cho student book; không ảnh hưởng slot đã chọn cho G1. | ☐ |

Ứng dụng nhận URL Google Meet, không tự tạo phòng. `https://meet.google.com/abc-defg-hij` chỉ là ví dụ định dạng, không bảo đảm phòng thật. Nếu cần test mở cuộc họp, dùng link thực do bạn tạo.

## U13. Cuộc họp trực tiếp, hủy và minh chứng

Để kiểm minh chứng, có thể chờ cuộc họp thật kết thúc hoặc dùng cuộc họp UAT do mentor tạo với giờ đã qua nếu nghiệp vụ/màn hình cho phép. Không đổi đồng hồ máy hay sửa DB để vượt điều kiện.

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U13-H1 | M1: **Meeting reports** (`/mentor/meetings`) → **Add team & time slot** → Team G1, Start time, Meeting link, Note → **Save meeting**. | Có cuộc hẹn trực tiếp cho G1, bộ đếm không vượt 2 cuộc không bị hủy. Chọn một khung giờ khác slot ở U12. | ☐ |
| U13-H2 | Khi cuộc hẹn còn SCHEDULED và chưa minh chứng: **Edit** đổi Note/giờ hợp lệ → Save meeting. | Cả mentor và student thấy thông tin mới; không thêm một cuộc hẹn thứ ba. | ☐ |
| U13-N1 | Khi G1 đã có 2 cuộc SCHEDULED/COMPLETED, thử thêm hoặc book cuộc thứ ba. | Nút bị khóa/nhóm không còn eligible hoặc báo giới hạn; không vượt quota. | ☐ |
| U13-H3 | M1 **Cancel** cuộc hẹn phụ SCHEDULED. Kiểm tra S1 và lịch slot nếu cuộc hẹn có slot. | Meeting CANCELED, nhường quota; slot gắn cuộc hẹn bị hủy không tự coi là AVAILABLE nếu nghiệp vụ đánh dấu CANCELED. Cuộc COMPLETED không có nút hủy như SCHEDULED. | ☐ |
| U13-H4 | Đảm bảo còn một cuộc hẹn của G1 đã kết thúc. S2: Groups → G1 → Meeting history → **Evidence image URL** nhập URL ảnh test có thể truy cập → **Submit evidence & complete**. | Bất kỳ member ACTIVE hợp lệ có thể nộp, không chỉ leader. Meeting thành COMPLETED; link minh chứng xuất hiện ở student và mentor. | ☐ |
| U13-N2 | Với cuộc hẹn chưa đến giờ kết thúc, kiểm tra phần minh chứng. | Không được hoàn thành sớm. Nếu chưa có cuộc hẹn đã kết thúc, U13-H4 ghi BLOCKED thời gian, không PASS. | ☐ |
| U13-H5 | M1: chọn Report term = TERM → **Export report**; I1 vào Groups → phần **Mentor meeting reports** của G1. | File báo cáo mở được, đúng kỳ/nhóm/giờ/trạng thái/minh chứng; I1 xem đúng nhóm được giao. | ☐ |

Cuộc hẹn tạo trực tiếp không nhất thiết có availability slot. FE Meeting reports hiện không thấy nút confirm cuộc hẹn; không coi nút Save meeting là đã test endpoint confirm.

## U14. Instructor cấu hình milestone

Người test: I1 → **Milestones** (`/instructor/milestones`), đã được gán G1.

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U14-H1 | **New milestone** → Term TERM, Course code EXE101, Title MS1, Weight 40, Max score 10, Position 1, Deadline tương lai, Description → lưu. | Timeline milestones có MS1 trong đúng kỳ/môn/phạm vi I1. | ☐ |
| U14-H2 | Tạo MS2 tương tự: Weight 60, Max score 10, Position 2, deadline sau MS1. | Có hai milestone tổng trọng số 100; thứ tự đúng. G2 cùng scope của I1 cũng có thể thấy bộ milestone này, không kỳ vọng chỉ G1. | ☐ |
| U14-H3 | **Edit** MS1 đổi Description/Deadline rồi lưu. S1: **Grades** → chọn G1 → mở MS1. | Detail hiển thị đúng title/weight/max score/deadline vừa lưu. | ☐ |
| U14-N1 | I2 chưa có nhóm được giao trong scope này vào Milestones. | Không tự sửa milestone thuộc I1; thiếu scope thì báo rỗng/không cho chọn thay vì tạo dưới danh nghĩa I1. | ☐ |
| U14-N2 | Thử thiếu Title hoặc Max score không hợp lệ trên form mới; Cancel sau kiểm tra. | Form/BE từ chối. Không thêm milestone rác vào bộ 40/60. | ☐ |
| U14-N3 | Tùy chọn trên scope UAT phụ: tạo milestone chưa có grade → **Archive** và xác nhận. | Bị archive theo nghiệp vụ, không xóa nhầm MS1/MS2. Nếu không có scope phụ thì để NOT RUN. | ☐ |

## U15. Contribution → đồng thuận → chấm điểm → xem điểm

Tiền điều kiện: G1 **chỉ có S1 và S2**, S1 leader, I1 instructor; MS1/MS2 ở trạng thái dùng để chấm; chưa đóng kỳ. Đây là bài quan trọng, thực hiện đúng thứ tự.

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U15-H1 | S1: **Grades** (`/student/grades`) → Group G1 → MS1 **View details** → **Member Contributions**. Nhập S1=100, S2=80 → **Save Contributions**. | Tạo revision contribution; đúng hai giá trị. Tổng 180 là hợp lệ vì chấm độc lập từng người, không bắt buộc tổng 100. | ☐ |
| U15-N1 | Trước khi mọi người Agree, I1: **Grading** → TERM/EXE101 → mở ô G1/MS1. | Có cảnh báo chưa đủ đồng thuận, **Save Grade** không cho chấm lần đầu. | ☐ |
| U15-H2 | S1 mở lại MS1 → **Agree**. S2 mở MS1 → Reason when requesting changes = `UAT: đề nghị kiểm tra tỷ lệ` → **Request changes**. | Revision thành CHANGES_REQUESTED; hiện lý do. Không tiếp tục Agree revision bị yêu cầu sửa. | ☐ |
| U15-H3 | S1 lưu revision mới, vẫn dùng S1=100/S2=80 nếu thống nhất dùng bộ số này; đóng/mở detail → Agree. S2 mở lại revision mới → Agree. | Cả hai phải đồng ý revision mới; đồng ý cũ không tự chuyển sang. Hiện 2/2 và AGREED. | ☐ |
| U15-H4 | I1: Grading → ô G1/MS1 → Score = 8, Feedback / Comments = `UAT prototype đạt` → **Save Grade**. | Ô được đánh dấu Graded; S1/S2 xem đúng group grade 8/10, contribution 100/80 và feedback. | ☐ |
| U15-N2 | S1/S2 mở MS1 đã được chấm. | Không còn sửa contribution hoặc thay quyết định đồng thuận như trước khi chấm. | ☐ |
| U15-H5 | Lặp cho MS2: S1=100, S2=100 → Save Contributions → S1 Agree + S2 Agree → I1 chấm 9/10. | Hai milestone đều Graded; Academic Summary có dữ liệu đầy đủ. | ☐ |
| U15-H6 | S1 và S2 F5 Grades → **My Total Grade**. | S1 = **8.60**; S2 = **7.96**. Phép tính: S1 = 8×1×0.4 + 9×1×0.6; S2 = 8×0.8×0.4 + 9×1×0.6. Nếu có thêm member/milestone/khác weight phải tính lại, không dùng hai số này máy móc. | ☐ |
| U15-H7 | I1: Grading → **Export CSV** trong đúng TERM/EXE101. Mở file đã tải. | Đúng nhóm/member/milestone và điểm đối chiếu được; ký tự tiếng Việt đọc được. FE hiện nút CSV, không giả định có nút XLSX. | ☐ |
| U15-N3 | Trên milestone chưa chấm/fixture phụ: contribution -1/101/rỗng; Request changes thiếu lý do; Score <0 hoặc >Max score. | Không lưu dữ liệu sai. Tránh sửa bộ điểm chính sau khi đã chốt U15-H6. | ☐ |

## U16. Thông báo và tính cập nhật giữa hai người

Người test: các actor nhận thông báo ở U06/U10/U11/U15. Có thể chạy xen kẽ các mục đó.

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U16-H1 | S2 mở biểu tượng chuông/**Notifications** trước khi S1 gửi invitation hoặc thao tác phát sinh notification. Giữ màn hình, quan sát không F5. | Ghi thông báo có tự xuất hiện không và mất bao lâu. Chỉ nhận thông báo dành cho S2. Chưa tự cập nhật không có nghĩa dữ liệu không được lưu. | ☐ |
| U16-H2 | Mở Notifications → **Show** = Unread only → chọn thông báo chưa đọc để đánh dấu/mở theo UI → quay lại. | Thông báo chuyển đã đọc, số unread giảm đúng một lần, đường dẫn đích đúng nghiệp vụ nếu có. | ☐ |
| U16-H3 | **Mark all read** → F5 → bấm lại. | Unread về 0, không âm; danh sách All notifications vẫn có lịch sử. | ☐ |
| U16-N1 | S3/M2 mở Notifications trong phiên riêng. | Không nhìn thấy thông báo riêng của S2. | ☐ |

Tách kết quả **“danh sách/count đúng”** và **“tự cập nhật không F5”**. UI tự cập nhật có thể nhờ polling, không chứng minh STOMP truyền MESSAGE đúng. Tài liệu rà soát trước có cảnh báo đường broadcast WebSocket; nghiệm thu giao thức cần test kỹ thuật riêng, không đánh PASS chỉ bằng chuông thông báo.

## U17. Dashboard và TV Display

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U17-H1 | A: **Dashboard** → chọn bộ lọc kỳ/môn có trên màn hình → đối chiếu nhóm, project, mentor, lịch với dữ liệu vừa tạo. | Count và card không lẫn scope; không so tổng toàn hệ thống với riêng G1 nếu còn dữ liệu import/nhóm phụ. | ☐ |
| U17-H2 | S1: Dashboard → kiểm tra nhóm/project/tiến độ/milestone; đổi một task phụ từ To Do sang Done rồi quay lại/F5. | Tiến độ phản ánh task thực; không lấy điểm 8.60 thay phần trăm hoàn thành task. | ☐ |
| U17-H3 | I1: Dashboard; M1: Groups và phần tổng quan/lịch; so với I2/M2. | Đúng nhóm/milestone/cuộc hẹn trong phạm vi mỗi người. Không tự coi dữ liệu G1 là của I2/M2. | ☐ |
| U17-H4 | A: **TV Display** → xem project và recruitment; chọn card nhóm nếu có; quay lại dashboard. | Tên nhóm/project, đề tài, nhu cầu tuyển và trạng thái đúng dữ liệu. Màn hình TV không được mặc định coi là public không cần login. | ☐ |
| U17-N1 | Thử bộ lọc không có dữ liệu, trang tiếp/trước nếu có, thu nhỏ màn hình. | Hiện empty state hợp lý, không treo spinner; nút thao tác/modal không bị che không bấm được. | ☐ |

## U18. Phân quyền, thao tác đồng thời và độ bền dữ liệu

Chạy trong kỳ OPEN, trước U19. Chỉ dùng nhóm/task phụ khi tạo tình huống cạnh tranh.

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U18-N1 | S1 đang login gõ URL `/admin/users`, `/admin/backups`, `/instructor/grading`. M2 thử màn của admin. | Bị chặn/chuyển về workspace phù hợp; không lộ hoặc sửa dữ liệu quản trị. Đây chỉ chứng minh lớp giao diện trong bài test này, chưa thay test authorization API. | ☐ |
| U18-N2 | Chưa login mở `/student/groups` hoặc `/admin/tv-display`. | Bắt đăng nhập, không thực hiện thao tác bảo vệ. | ☐ |
| U18-N3 | I1/I2 ở hai browser profile cùng mở một nhóm phụ chưa instructor → cùng xác nhận Claim. | Chỉ một người trở thành instructor. Người còn lại nhận thông báo xung đột/dữ liệu đã được nhận và danh sách cập nhật. Không chạy trên G1/G2 đã gán. | ☐ |
| U18-N4 | S1/G1 và S3/G2 cùng mở một slot M1 AVAILABLE rồi bấm book gần đồng thời, khi cả hai nhóm còn quota. | Chỉ một booking thành công; nhóm thua nhận lỗi dễ hiểu, không hai nhóm chiếm cùng slot. Nếu hết quota, ghi BLOCKED và chuẩn bị nhóm phụ, không hủy cuộc COMPLETED. | ☐ |
| U18-N5 | S1 và S2 mở cùng task phụ, cùng vào sửa title trước khi người kia lưu. S1 lưu `UAT phiên A`, sau đó S2 lưu `UAT phiên B` từ form cũ. | Dữ liệu không bị ghi đè âm thầm bằng phiên cũ; nếu FE đã tự tải bản mới trước save thì không coi đây là đã tái hiện conflict. Ghi cả hai kết quả, F5 kiểm tra bản cuối. | ☐ |
| U18-H1 | Sau mỗi create/save, đóng modal → chuyển menu → quay lại → F5; kiểm thêm bằng actor liên quan. | Nội dung vẫn tồn tại, không chỉ cập nhật tạm trên màn hình. Không có success toast đi kèm dữ liệu thực không lưu. | ☐ |

Race condition, token refresh rotation, rollback DB và khóa transaction không thể chứng minh đầy đủ chỉ bằng vài thao tác UI; xem checklist kỹ thuật riêng nếu cần nghiệm thu những yêu cầu này.

## U19. Đóng kỳ rồi gửi feedback

**Điểm không quay lại trong luồng chính:** chỉ thực hiện sau khi đã test xong nhóm, đề tài, task, lịch, điểm và các nhánh lỗi cần ghi dữ liệu. Đóng kỳ không có nút reopen trong kịch bản này.

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U19-H1 | A: Terms → đúng TERM UAT → **Close term** → đọc tên kỳ trong modal → xác nhận. | Kỳ CLOSED, có người/thời gian đóng; feedback được tạo theo member và mentor/instructor được giao khi đóng. | ☐ |
| U19-H2 | S1: Groups/Problems/Tasks/Grades → G1; F5. | Có cảnh báo kỳ đã kết thúc/read-only; còn đọc được lịch sử; thao tác student ghi dữ liệu bị ẩn/khóa/từ chối theo nghiệp vụ. Không áp cùng kỳ vọng cấm mọi thao tác cho mọi role. | ☐ |
| U19-H3 | S1: **Feedback** (`/student/feedback`) → Academic term TERM → Status Pending → **Apply filters** → mở feedback của M1 → Rating 5, Comment `UAT mentor hỗ trợ tốt` → gửi. Làm feedback của I1 tương tự. | Hai feedback của S1 thành SUBMITTED; không gửi nhầm đánh giá cho người khác. | ☐ |
| U19-H4 | S1 mở lại feedback M1 → **Update feedback** → đổi Rating 4 và comment → lưu. S2 gửi feedback M1 Rating 5. | Sửa cùng feedback, không nhân đôi. Nếu chỉ S1/S2 đã gửi cho M1 thì average của hai bản này là 4.50; có phản hồi G2 khác thì phải tính thêm. | ☐ |
| U19-H5 | M1/I1: **Feedback** → lọc TERM/EXE101 → Apply filters. | Thấy điểm tổng hợp/distribution và **Anonymous comments** của bản thân; không hiển thị tên/email/MSSV người gửi. Quan sát UI chưa chứng minh JSON không rò thông tin. | ☐ |
| U19-H6 | A: **Feedback** → lọc TERM/target/status → đối chiếu các bản đã gửi → **Export selected term**. | Admin thấy danh tính đúng quyền. Excel tải được, có sheet Mentor Feedback/Instructor Feedback, chỉ các bản SUBMITTED. Export theo kỳ không nhất thiết theo mọi bộ lọc đang chọn trên bảng. | ☐ |
| U19-H7 | A trở lại Terms; S2 hoàn tất các feedback còn thiếu của mình. | Số đã gửi/dự kiến tiến triển đúng. G1 hai member và hai người nhận tạo 4 feedback dự kiến; toàn kỳ có G2/nhóm import thì tổng cao hơn 4. | ☐ |
| U19-N1 | Student mở form feedback, thử rating ngoài 1–5 nếu control cho phép; mở lại màn form ghi dữ liệu kỳ cũ rồi thử lưu. | Giá trị sai/ghi trái điều kiện bị chặn; không coi thao tác bị ẩn là đã gửi request BE thành công. | ☐ |

## U20. Archive student — tùy chọn, chạy cuối trên tài khoản UAT

Chỉ được dùng khi **tất cả sinh viên bị ảnh hưởng của kỳ này là tài khoản test**. Nếu đã import lớp thật vào cùng kỳ, không bấm Archive students; ghi BLOCKED do phạm vi dữ liệu không an toàn.

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U20-H1 | Sau U19, nếu muốn test nhánh giữ ACTIVE: A tạo kỳ OPEN UAT mới, S2 tham gia nhóm kỳ mới bằng thao tác FE, rồi mới archive kỳ cũ. | Có một student của kỳ cũ đang có membership kỳ OPEN mới để kiểm tra ngoại lệ. Không cần thay đổi G1 cũ. | ☐ |
| U20-H2 | A: Terms → kỳ UAT cũ CLOSED → **Archive students** → đọc phạm vi → xác nhận. | Modal **Student archive completed** có Archived students / Already inactive / Kept in the current open term. Tài khoản đủ điều kiện chuyển INACTIVE; S2 của H1 được giữ ACTIVE. | ☐ |
| U20-H3 | A: Users tìm sinh viên đã archive; actor đó Sign out rồi thử login. | INACTIVE và không login được; lịch sử kỳ cũ không bị xóa. Bước này sẽ làm các bài student tiếp theo không chạy được nếu không chuẩn bị actor khác. | ☐ |
| U20-N1 | Quan sát kỳ còn group/feedback: nút **Delete term** không xuất hiện như kỳ rỗng. Với kỳ UAT phụ thật sự rỗng, thử Delete term và Cancel trước khi xác nhận. | Không xóa dây chuyền dữ liệu nhóm/feedback. Chỉ xóa kỳ phụ rỗng khi không còn bài test phụ thuộc. | ☐ |

## U21. Backup qua giao diện Admin

Người test: A → **Backups** (`/admin/backups`). Có thể chạy phần tạo backup trước U19 để giữ mốc dữ liệu; việc có backup không có nghĩa nên restore DB hiện tại.

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U21-H1 | **Create backup now** → đợi job từ trạng thái đang xử lý đến cuối → **Download** khi SUCCEEDED. | Có file `.dump` không rỗng; job có tên file/thời gian/kích thước. Nếu FAILED, ghi lỗi và BLOCKED bài download; không đổi FAILED thành PASS vì đã thấy job. | ☐ |
| U21-H2 | Lọc Backup history theo status và chuyển trang nếu có đủ job. | Job đúng trạng thái, Download chỉ khả dụng khi thành công. | ☐ |
| U21-H3 | Ghi lại lịch hiện tại. **Automatic schedule** → để tắt Enable automatic backup → Run daily at 02:00, Timezone `Asia/Ho_Chi_Minh`, Retention days 14 → **Save schedule** → F5. | Giá trị giữ đúng, trạng thái tắt không tự chạy job. Sau test trả lại cấu hình ban đầu nếu trước đó khác. | ☐ |
| U21-N1 | Thử timezone không hợp lệ/retention ngoài khoảng được chấp nhận rồi lưu. | Có lỗi, cấu hình hợp lệ trước đó không bị ghi đè. | ☐ |

**Restore không nằm trong luồng test local chính.** Nếu có một stack UAT riêng, DB/volume riêng và đã xác nhận URL FE của stack đó: tạo backup trạng thái A → tạo user UAT đánh dấu B → Backups → Restore database chọn dump A → Restore from file → nhập `RESTORE_DATABASE` để xác nhận → chờ kết quả → đăng nhập lại → marker B không còn, dữ liệu A còn. Nếu chưa có stack riêng, ghi **BLOCKED: chưa có môi trường restore an toàn**, không thực hiện trên `localhost:3000` hiện tại chỉ vì thấy nút restore.

## U22. Màn hình submission cũ — chỉ xem

Hai đường dẫn tồn tại nhưng không nằm trong sidebar hiện tại: `/student/submissions` và `/instructor/submissions`. Mở bằng cách gõ URL sau khi đăng nhập đúng role.

| ID | Thao tác trên FE | Kết quả mong đợi | KQ |
|---|---|---|---|
| U22-H1 | S1 trước khi archive: mở `/student/submissions` → chọn G1. | Xem milestone/submission/average lịch sử hoặc empty state hợp lý. Không coi “No grade”/danh sách rỗng là đã test nộp bài. | ☐ |
| U22-H2 | I1 mở `/instructor/submissions` → chọn bộ lọc kỳ/môn/nhóm có trên form. | Chỉ đọc dữ liệu lịch sử phù hợp. Muốn kiểm đủ detail/grade/late cần fixture legacy đã tồn tại trong DB test riêng. | ☐ |
| U22-N1 | Nếu UI còn nút nộp bài/chấm bulk nhưng gọi chức năng đã bị bỏ trong Java, ghi lỗi FE–contract. | Không yêu cầu tự mở lại endpoint trái nghiệp vụ Java chỉ để nút hoạt động. | ☐ |

Chức năng tạo/sửa submission và legacy grade đã bị comment trong nguồn Java được đối chiếu. Bộ contribution/matrix mới ở **Grades/Grading** là bài U15, không thay thế dữ liệu legacy để kết luận U22 PASS.

## 4. Mẫu ghi lỗi / kết quả

Sao chép mẫu này cho từng case FAIL/BLOCKED. Đối với PASS, tối thiểu ghi case ID, người test, ngày giờ và bằng chứng sau F5.

```text
Case ID:
Kết quả: NOT RUN / PASS / FAIL / BLOCKED
Ngày giờ và trình duyệt:
Vai trò / tên tài khoản UAT (không mật khẩu):
URL màn hình:
Tên kỳ / nhóm / đề tài / task liên quan:
Tiền điều kiện đã đáp ứng:
Các bước thực tế đã bấm:
Kết quả mong đợi:
Kết quả thực tế / nguyên văn thông báo lỗi:
Sau F5 có còn dữ liệu không:
Tài khoản thứ hai nhìn thấy gì:
Ảnh chụp / tên file tải xuống:
Có ảnh hưởng case tiếp theo nào không:
```

Không ghi access token, refresh token, Google ID token, mật khẩu hoặc toàn bộ file chứa thông tin cá nhân vào báo cáo chia sẻ. Chỉ khi cần điều tra sâu mới bổ sung Network log đã che dữ liệu nhạy cảm; không phải yêu cầu để bấm theo kịch bản.

## 5. Giới hạn: test FE không đồng nghĩa phủ toàn bộ API

| Phần | FE hiện cho test | Chưa thể kết luận chỉ bằng các bước FE |
|---|---|---|
| Auth | Login/Google/password gate/logout/profile | Refresh-token rotation, blacklist token cũ, chữ ký/audience JWT đầy đủ |
| Group/booking | Invite/join/lock/claim/booking; thử hai phiên | Mọi thứ tự race, transaction rollback, mọi alias route |
| Board/task | Create board, task edit/move/archive, checklist tick/delete, comment add/delete | Chưa thấy control update/delete board, task restore, comment edit, checklist title/order edit trong component hiện tại |
| Meetings | Availability, booking, direct meeting, edit/cancel, evidence, report | Chưa thấy nút confirm meeting trong Meeting reports; không suy từ nút Save |
| Grade export | FE có Export CSV | Endpoint XLSX và các kiểu lỗi đầu vào không có nút riêng |
| Catalog/criteria | Dropdown recruitment role/domain, xem criteria | CRUD/giá trị đầu vào khác ngoài lựa chọn UI, nếu API có hỗ trợ |
| Legacy submission/grade | Route xem lịch sử | Không tạo dữ liệu legacy mới qua FE; cần fixture riêng |
| Notification | List/unread/mark read, quan sát tự cập nhật | STOMP thực sự phát MESSAGE, quyền subscribe, reconnect; polling không phải bằng chứng STOMP |
| Backup | Tạo/tải/lịch chạy và form restore | Khôi phục an toàn chỉ được nghiệm thu trên DB disposable |
| Authorization/validation | Nút ẩn/khóa, điều hướng role, lỗi form | FE chặn request không chứng minh BE chặn request giả mạo |

### Đối chiếu với 37 controller Java

Đây là **bản đồ kịch bản**, không phải bảng xác nhận controller đã hoàn thành hoặc PASS. Một controller xuất hiện trong bảng không có nghĩa mọi method/route của nó đã được test qua FE.

| # | Controller Java | Kịch bản / mức phủ UI |
|---|---|---|
| 01 | AcademicTermController | U04/U06: danh sách kỳ trong form |
| 02 | AdminFeedbackController | U19: tìm/lọc/export feedback |
| 03 | AdminGroupController | U08: danh sách/gán instructor, mentor |
| 04 | AdminProblemController | U09/U10: tạo/sửa/status/review |
| 05 | AdminTermController | U04/U19/U20: tạo/đóng/archive/xóa kỳ rỗng |
| 06 | AdminUserController | U01: CRUD/reset/search; không mặc định phủ mọi biến thể API reset |
| 07 | AuthController | U02/U03: UI auth; refresh/blacklist cần test kỹ thuật |
| 08 | BackupController | U21; restore có điều kiện môi trường riêng |
| 09 | CourseMilestoneController | U14/U15: timeline/detail; nghiệp vụ legacy không suy rộng |
| 10 | DashboardController | U17: màn hình theo role/TV, không phải mọi tổ hợp query |
| 11 | FeedbackController | U19: student submit/update, recipient summary |
| 12 | GroupController | U06–U08: nhóm/member/leader/lock/assignment |
| 13 | GroupInvitationController | U06/U07: invite/accept/reject/cancel |
| 14 | GroupJoinRequestController | U07: request/cancel/approve/reject |
| 15 | GroupMeetingController | U12/U13: book/direct/edit/cancel/evidence; confirm chưa có nút thấy được |
| 16 | GroupProblemController | U09/U10: select/clear/propose/edit/delete |
| 17 | GroupRecruitmentRoleController | U06: dropdown recruitment roles |
| 18 | GroupTaskController | U11/U18: task/checklist/comment/activity, có giới hạn UI nêu trên |
| 19 | ImportController | U05: mentor/student-group/batch/error/template |
| 20 | InstructorGroupBoardController | U08/U18: list/filter/claim |
| 21 | InstructorProblemController | U10: pending/review theo phạm vi |
| 22 | InstructorSubmissionController | U22: legacy read, phụ thuộc fixture |
| 23 | MentorAvailabilityController | U12: calendar/create/edit/duplicate/cancel |
| 24 | MentorMeetingReportController | U13: report/export |
| 25 | MilestoneGradeController | U22: legacy grade read, không được tính bằng grade matrix mới |
| 26 | MilestoneGradeMatrixController | U15: contribution/agreement/group grade/matrix/CSV |
| 27 | MilestoneSubmissionController | U22: legacy read, không có luồng tạo submission mới để test |
| 28 | NotificationController | U16: list/count/read; WebSocket kiểm riêng |
| 29 | ProblemController | U09: bank/detail/filter |
| 30 | ProblemCriteriaController | U09: đọc tiêu chí trong problem detail |
| 31 | ProblemDomainController | U09: Domain Manager/dropdown |
| 32 | ProblemImportController | U05: problem bank import/template |
| 33 | ProfileController | U02/U03: profile/password |
| 34 | StudentAccountImportController | U05: Student accounts khác Students |
| 35 | StudentController | U06: tìm student khi invite và thông tin profile student nếu UI mở |
| 36 | StudentGroupGradeController | U22: average legacy, không phải My Total Grade contribution |
| 37 | TaskBoardController | U11: list/create/view board; các thao tác thiếu UI kiểm riêng |

## 6. Tài liệu tham chiếu khi cần điều tra

- [Đặc tả nghiệp vụ Java](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md).
- [Đính chính lần rà soát 2](D:/Flowzy-api/docs/CONTROLLER_DOCUMENTATION_REVIEW_2.md).
- [Checklist từng API](D:/Flowzy-api/docs/LOCAL_UAT_API_CHECKLIST.md) — dùng bổ sung cho phần UI không bao phủ, không cần làm trong luồng bấm FE chính.
- [Playbook kỹ thuật local](D:/Flowzy-api/docs/LOCAL_UAT_PLAYBOOK.md).

Nguồn UI đã đọc khi soạn: `app-shell.tsx` (menu theo role); các component trong `src/modules/auth`, `users`, `imports`, `groups`, `problems`, `projects`, `mentoring`, `milestones`, `grading`, `feedback`, `notifications`, `backups`. Đường dẫn gốc là `D:/Flowzy-FE/Flowzy-frontend`. Khi FE đổi nhãn hoặc luồng, cập nhật kịch bản theo source mới; giữ Java/đặc tả đã đính chính làm căn cứ kết quả nghiệp vụ, không lấy hành vi lỗi hiện tại của FE làm chuẩn đúng.
