# Dữ liệu local đã chuẩn bị để test Flowzy

Ngày thực hiện: 28/09/2026, giờ Việt Nam. FE: [Login local](http://localhost:3000/login). BE local: cổng 8080; PostgreSQL local: cổng 5433, database `flowzy`.

## 1. Kết quả import file nhóm

File nguồn: `C:/Users/lenovo/Downloads/SU26_EXE101 _ Group List (mentor).xlsx`, sheet `SU26_EXE101 | Group List`. File gốc được giữ nguyên, không sửa email, tên, mã sinh viên hoặc tên sheet. Đã đọc cấu trúc workbook bằng kỹ năng bảng tính và import file gốc qua chức năng import của BE, không chèn trực tiếp danh sách nhóm bằng SQL.

| Hạng mục | Kết quả đã kiểm tra |
|---|---|
| Import batch | **3**, trạng thái COMPLETED |
| Dòng sinh viên | 1.292 |
| Account/profile tạo mới | **1.265** |
| Dòng không tạo được account mới | **27**: 19 email sai định dạng, 4 thiếu email, 4 trùng/xung đột danh tính |
| Nhóm nhập từ file | **228**, thuộc SU26 / EXE101 |
| Membership hợp lệ sau đối chiếu | **1.265** |
| Nhóm gán mentor | **219/228** sau gán bổ sung theo đúng mã trong file |
| Nhóm chưa gán được mentor | **9**, thiếu 4 mentor code hợp lệ trong DB |
| Nhóm dùng leader dự phòng | **10**, cần kiểm tra lại dữ liệu nguồn |
| Nhóm chưa có tên trong file | 9; importer dùng tên `EXE101 Group <STT>` |
| Instructor cho nhóm thật | Chưa gán; importer hiện không ánh xạ các cột tên/alias giảng viên trong workbook thành instructor account |
| Kỳ SU26 | **OPEN**, không đóng kỳ và không archive sinh viên |

COMPLETED nghĩa là job xử lý xong, **không phải tất cả các dòng đều thành công**. Vào Admin → **Imports** → Batch lookup → nhập `3` → **View status / View errors** để xem chi tiết.

### Các dòng cần chỉnh/xác nhận trong file nguồn

Số dưới đây là số dòng Excel thực, bao gồm hàng tiêu đề và các hàng trống; không phải số thứ tự sinh viên sau khi bỏ dòng trống.

| Vấn đề | Dòng Excel |
|---|---|
| INVALID_EMAIL (19) | 72, 146, 205, 223, 269, 487, 542, 565, 590, 802, 824, 849, 892, 988, 1021, 1094, 1245, 1259, 1304 |
| Thiếu email (4) | 452, 453, 520, 780 |
| Trùng mã sinh viên với dữ liệu demo cũ (3) | 30 — SE190005; 956 — SE190001; 1368 — SE190008 |
| Email đã có nhưng mã sinh viên khác (1) | 671 — file dùng SE193324; account email tương ứng đang có profile SE190002 |

Không tự đoán email đúng hoặc ghi đè danh tính hiện hữu. Ba tài khoản demo cũ bị importer nối vào nhóm chỉ do trùng mã đã được **gỡ khỏi membership vừa tạo sai** ở nhóm 5, 160, 228; không xóa account/profile cũ. Nhóm 160 đã chuyển leader dự phòng sang thành viên hợp lệ đầu tiên, **Ngô Chí Vỹ**, vì leader ghi trong file đang bị xung đột mã và chưa import được. Cần xác nhận lại trước khi sử dụng như dữ liệu chính thức.

Các nhóm có cảnh báo leader dự phòng: **25, 81, 92, 94, 99, 160, 183, 218, 226, 227**. Đây là quy tắc fallback của importer, không phải xác nhận người đó là leader theo danh sách gốc.

### Mentor còn thiếu

| Mentor code | Nhóm theo STT/groupNo |
|---|---|
| M013 | 18, 167 |
| M090 | 35, 53, 227 |
| M100 | 83, 215 |
| M033 | 140, 148 |

Bộ đọc ban đầu báo 29 cảnh báo mentor. Đối chiếu lại toàn chuỗi trong file tìm được mã Mxxx hợp lệ và đã gán bổ sung **20 nhóm** qua API quản trị: 22, 24, 26, 61, 78, 97, 103, 105, 112, 116, 121, 145, 158, 161, 191, 193, 206, 208, 212, 223. Lỗi là phần học vị trong ngoặc bị lấy làm mã thay vì Mxxx ở phía sau. Không thay đổi source importer trong lượt chuẩn bị dữ liệu này; lần import sau vẫn có thể gặp lỗi đó. Cảnh báo lịch sử trong batch 3 được giữ nguyên để truy vết, không xóa/sửa thành công giả.

**Không import lại toàn bộ file ngay:** sẽ tạo nhiều lỗi ALREADY_EXISTS và có thể tái gắn nhầm 3 account demo nếu chưa xử lý xung đột mã. Cần thống nhất các dòng sai và sửa quy tắc đối chiếu trước khi chạy lại.

## 2. Tài khoản test riêng

Đã tạo **11 tài khoản UAT mới**: 2 instructor, 2 mentor, 7 student. Tất cả ACTIVE, đã bỏ yêu cầu đổi mật khẩu cho bộ test này; không reset mật khẩu của người dùng thật.

Mở [thông tin đăng nhập UAT local](D:/Flowzy-api/.tools/local-demo-20260928/test-accounts.local.json), lấy giá trị trường `password` và email tương ứng. Password chỉ dùng cho các tài khoản UAT trong file, không phải mật khẩu admin hay tài khoản import. File nằm trong `.tools/`, được gitignore; không commit hoặc dùng các credential này khi deploy.

| Ký hiệu | Email | Vai trò và phạm vi |
|---|---|---|
| I1 | uat.i1.0928a@example.com | Instructor của Team A và Team B; có milestone để chấm |
| I2 | uat.i2.0928a@example.com | Instructor chưa được giao nhóm; test claim/phân quyền |
| M1 | uat.m1.0928a@example.com | Mentor của Team A/B/C; có availability và cuộc hẹn |
| M2 | uat.m2.0928a@example.com | Mentor chưa được giao nhóm; test phạm vi |
| S1 | uat.s1.0928a@example.com | Leader Team A |
| S2 | uat.s2.0928a@example.com | Member Team A |
| S3 | uat.s3.0928a@example.com | Leader Team B |
| S4 | uat.s4.0928a@example.com | Member Team B |
| S5 | uat.s5.0928a@example.com | Leader Team C |
| S6 | uat.s6.0928a@example.com | Chưa vào nhóm, có invitation PENDING từ Team C |
| S7 | uat.s7.0928a@example.com | Chưa vào nhóm, đã gửi join request PENDING đến Team C |

Đăng nhập bằng email/password, không dùng Google cho email `example.com`. Tài khoản thật import không có mật khẩu test chung: dùng Google với email đã nhập đúng hoặc cơ chế reset có chủ đích của admin; không reset hàng loạt.

Admin tiếp tục dùng tài khoản local hiện có. Tổng DB sau chuẩn bị: **1 ADMIN, 2 INSTRUCTOR, 103 MENTOR, 1.288 STUDENT**, tất cả đang ACTIVE tại thời điểm kiểm tra.

## 3. Ba nhóm UAT dùng ngay

| Nhóm | ID / groupNo | Dữ liệu có sẵn | Nên test |
|---|---|---|---|
| UAT Flowzy Team A | 229 / 229 | S1 + S2, I1/M1, official problem, board 5 task, checklist, comment, milestone và cuộc hẹn | Kanban, My Task List, xem điểm, chấm MS2, nộp evidence |
| UAT Flowzy Team B | 230 / 230 | S3 + S4, I1/M1, proposal chờ duyệt, board 5 task, contribution MS1 bị REQUEST_CHANGES | Instructor review, leader sửa revision, thành viên đồng thuận |
| UAT Flowzy Team C | 231 / 231 | S5 leader, M1 mentor, chưa instructor, mời S6, S7 xin vào; có nhu cầu tuyển | Accept/Decline, Approve/Reject, khóa nhóm, claim instructor |

Tổng DB có **231 nhóm** = 228 nhóm thật từ workbook + 3 nhóm UAT. Nhóm UAT dùng cùng kỳ **SU26/EXE101** vì hệ thống chỉ cho một kỳ OPEN. Phân biệt bằng tiền tố UAT; không thêm task, điểm hoặc mentor giả vào nhóm thật.

### Đề tài

- Domain: `UATD0928A` — UAT Software.
- Official ACTIVE: `UAT Campus Booking`, `UAT Smart Inventory`.
- Official INACTIVE: `UAT Inactive Topic`, dành cho kiểm tra lọc/quyền chọn.
- Team A đang chọn UAT Campus Booking.
- Team B có `UAT Proposal - chờ giảng viên duyệt`, PENDING_REVIEW; I1 vào Problems để xử lý.

### Task

Team A và Team B mỗi nhóm có board **UAT Sprint 1**, gồm đúng 5 task, một task ở mỗi cột: Backlog / To Do / In Progress / In Review / Done. Tổng cộng **10 task**, có assignee, priority và deadline tương lai. Task **UAT Xây dựng API** ở mỗi nhóm có hai checklist item, một item đã hoàn thành, và một comment mẫu.

Checklist chỉ leader hoặc assignee được sửa theo nghiệp vụ. S2/S4 không nhất thiết là assignee mọi task; nếu bị 403 khi sửa task ngoài quyền, kiểm tra assignee trước khi kết luận lỗi hệ thống.

### Milestone và điểm

Milestone do I1 phụ trách trong SU26/EXE101, không gán cho các nhóm nhập thật chưa có instructor:

| Mốc | ID | Trọng số / Max score | Team A | Team B |
|---|---|---|---|---|
| UAT Prototype | 1 | 40% / 10 | Contribution S1=100%, S2=80%; cả hai AGREE; đã chấm 8/10 | S3=100%, S4=70%; S4 REQUEST_CHANGES, chưa được chấm |
| UAT Final | 2 | 60% / 10 | S1=100%, S2=100%; cả hai AGREE; **chưa chấm**, sẵn sàng để bạn chấm | Chưa nhập contribution |

Điểm tổng hiện tại Team A: **S1=3.20, S2=2.56**, còn In Progress vì Final chưa chấm. Nếu bạn đăng nhập I1 và chấm Final **9/10**, tổng mong đợi thành **8.60 / 7.96** và hoàn tất bộ điểm.

Team B: S3 phải lưu revision contribution mới, sau đó **cả S3 lẫn S4 Agree** mới chấm lần đầu được. Không chỉ bấm lại Agree trên revision đã REQUEST_CHANGES.

### Lịch mentor — giờ Việt Nam

M1 có 3 slot ngày **29/09/2026**: 09:00–10:00, 11:00–12:00, 14:00–15:00. Slot 09:00 đã được S1 book cho Team A; hai slot còn lại AVAILABLE để Team B/C test booking.

Team A có một cuộc hẹn trực tiếp đã qua: **27/09/2026, 09:00–10:00**, trạng thái SCHEDULED, chưa evidence. S1 hoặc S2 vào Groups → Mentor meetings → Meeting history để nhập URL ảnh minh chứng của bạn, bấm **Submit evidence & complete**.

Team A hiện đã có 2 cuộc không bị hủy, nên không thể book cuộc thứ ba. Muốn test thêm booking dùng Team B/C, không coi quota đó là lỗi seed.

Link Meet được seed là chuỗi đúng định dạng để kiểm thử, **không phải phòng họp thật**. Khi muốn test tham gia cuộc họp, thay bằng link Google Meet thực do bạn tạo. Khi test vào ngày khác, tạo slot tương lai mới vì các mốc ngày trên có thể đã qua.

## 4. Lộ trình test ngay, không cần tự tạo lại setup

1. Đăng nhập **S2** → Tasks → My Task List; chuyển Group Kanban Board → chọn UAT Sprint 1. Mở task được giao, đổi trạng thái/comment. Đổi sang S1 để kiểm tra leader.
2. Đăng nhập **I1** → Problems → Team B → Review → chọn Approve hoặc Reject kèm lý do. Sang S3 kiểm tra kết quả.
3. Với **S3/S4**, vào Grades → Team B → Prototype: xem lý do yêu cầu sửa; S3 lưu lại contribution, rồi từng tài khoản Agree.
4. **I1** → Grading → SU26 / EXE101 → Team A / Final → chấm 9 → Save Grade. **S1/S2** xem tổng 8.60/7.96.
5. **S6** → Groups → Invitations: Accept/Decline lời mời Team C. **S5** → My Groups → Join Requests: Approve/Reject S7.
6. **I2** → Groups → tìm Team C → Claim this group. Trước đó Team C cố ý chưa có instructor.
7. **M1** → Availability xem lịch; **S3** → Team B → Mentor meetings để book slot còn trống. **S2** → Team A nộp evidence cho cuộc hẹn đã qua.
8. Các actor xem Notifications; Admin xem Dashboard / Groups / Problem Bank / Imports batch 3.

Dùng browser profile riêng hoặc Sign out trước khi đổi tài khoản. Hai tab cùng profile không phải hai phiên độc lập.

[Kịch bản FE đầy đủ 143 case](D:/Flowzy-api/docs/FE_MANUAL_UAT_SCENARIOS.md) vẫn dùng làm tài liệu tham khảo, nhưng **setup hiện đã có sẵn và TERM là SU26**, không phải UAT0928A. Không chạy lại phần tạo trùng account/group. Nếu muốn bài tạo mới, chọn hậu tố và nhóm UAT khác trong cùng SU26, chỉ dùng actor chưa có membership cùng môn.

## 5. Những phần chưa seed hoặc cần lưu ý

- **Không đóng SU26 để seed feedback.** Kỳ này có 228 nhóm nhập thật; đóng kỳ sẽ tác động tất cả. Không chạy các bước Close term / Archive students trong playbook trên kỳ này chỉ để thử nút. Muốn test feedback cuối kỳ cần DB UAT tách riêng hoặc quyết định đóng kỳ có chủ đích.
- Không bịa dữ liệu legacy submission/legacy grade vì luồng ghi đã bị bỏ trong Java. Không đánh dấu các test legacy là PASS dựa trên dữ liệu matrix mới.
- Chưa tạo lịch backup tự động hoặc chạy restore. Có bản sao lưu trước import tại [backup trước import](D:/Flowzy-api/.tools/local-demo-20260928/before-group-import.dump), 189.784 byte. Không restore lên DB hiện tại chỉ để thử vì sẽ mất dữ liệu sau mốc đó.
- Trong quá trình tạo meeting, phát hiện BE trả 500 khi nhận DateTime dạng offset `+07:00`: Npgsql yêu cầu UTC nhưng giá trị được bind Kind=Local. Dữ liệu seed đã gửi thời điểm tương đương dạng UTC `Z` và tạo thành công. **Chưa sửa production code cho lỗi này**; nếu FE gửi offset và gặp 500 cần xử lý riêng, không coi seed thành công là đã hết lỗi.
- Importer hiện có rủi ro lấy nhầm học vị làm mentor code và ghép student theo code khi email khác. Các liên kết sai của batch này đã được đối chiếu/gỡ như mục 1, nhưng nguyên nhân trong source chưa được sửa trong tác vụ dữ liệu.

## 6. Đã kiểm tra gì

**30 kiểm tra API/dữ liệu PASS**, gồm đăng nhập 11 actor đúng role và không bị password gate; membership ba nhóm; phạm vi instructor/mentor; năm cột task mỗi board; grade/contribution/agreement; cuộc hẹn/slot; pending invitation/join request; notification; FE `/login` HTTP 200.

Đối chiếu DB bổ sung: 228 nhóm import có leader là thành viên hợp lệ; không có membership trùng cùng sinh viên/kỳ/môn; không còn account `.demo@example.com` bị nối vào các nhóm import. Dữ liệu gốc/tài khoản cũ được giữ, chỉ gỡ 3 membership tạo sai trong lượt import này.

Đây là kiểm tra dữ liệu và các API phục vụ FE, **chưa phải chạy toàn bộ 143 case bằng trình duyệt**, chưa xác nhận parity toàn bộ controller và không bao gồm chứng nhận WebSocket realtime.
