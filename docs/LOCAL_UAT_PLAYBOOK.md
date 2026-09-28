# Flowzy — kịch bản kiểm thử nghiệp vụ local

Ngày soạn: 2026-09-27; kiểm tra khởi động hoàn tất lúc 00:00 ngày 2026-09-28 (giờ Việt Nam). Đối tượng: người kiểm thử qua frontend và Swagger/Postman.

## 1. Bắt đầu ở đâu

- Frontend: [http://localhost:3000/login](http://localhost:3000/login).
- Backend: [Swagger](http://localhost:8080/swagger-ui.html), [health](http://localhost:8080/actuator/health), [OpenAPI](http://localhost:8080/v3/api-docs).
- DB local: PostgreSQL tại localhost:5433, database flowzy. Không nhầm PostgreSQL khác trên 5432.
- RAG đã được tạm dừng theo yêu cầu để nhường cổng 8080. Không khởi động lại RAG trên cùng cổng khi đang test Flowzy.
- FE dùng `.env.local`, HTTP base `http://localhost:8080`, STOMP `ws://localhost:8080/ws`. Google Client ID đã cấu hình cùng giá trị ở FE/BE; vẫn cần Google Console cho phép origin localhost:3000.

Đọc tài liệu này theo thứ tự F01 → F18. Sau đó dùng [checklist từng API](D:/Flowzy-api/docs/LOCAL_UAT_API_CHECKLIST.md) để đánh dấu đủ **37 controller Java / 174 method controller / 192 HTTP method–route**, tính cả alias. Flowzy gộp một số controller catalog vào CatalogController; không dùng số file C# để kết luận thiếu controller.

**Kết quả trong tài liệu là kết quả mong đợi, không phải tuyên bố đã chạy và PASS.** Chỉ ghi PASS sau khi có HTTP response, quan sát giao diện và kiểm tra tác động dữ liệu. Swagger/OpenAPI tĩnh có chỗ khác source (ví dụ task create HTTP 201 dù schema snapshot ghi 200); ưu tiên Java controller đang hoạt động, đặc tả đã đính chính và bằng chứng thực tế.

Nguồn đối chiếu: [đặc tả nghiệp vụ](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md), [HTTP/DTO](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md), [đính chính](D:/Flowzy-api/docs/CONTROLLER_DOCUMENTATION_REVIEW_2.md), [tiến độ hiện tại](D:/Flowzy-api/docs/CONTROLLER_PARITY_IMPLEMENTATION_PROGRESS.md). Chưa có chứng nhận tương đương toàn bộ 37 controller; báo cáo 190 test trước đây không thay thế nghiệm thu thủ công lần này.

## 2. Quy tắc test và dữ liệu ban đầu

Snapshot đọc DB lúc chuẩn bị: **1 ADMIN, 101 MENTOR, 16 STUDENT**, đều ACTIVE; **0 INSTRUCTOR, 0 group, 0 academic term**. Các số này có thể đổi khi bạn thao tác.

- Batch mentor #1: 105 dòng, 101 thành công, 4 lỗi; batch student-account #2: 15/15 thành công. Không import lại chỉ để chuẩn bị tài khoản — sẽ phát sinh lỗi trùng.
- 15 student-account trong file template dùng email `@example.com`: chỉ dùng test bằng mật khẩu được admin đặt, không dùng Google login thật.
- Tạo tài khoản UAT mới thay vì đổi mật khẩu hoặc disable các mentor đã import. Không cần gửi mật khẩu, access token, refresh token hay Google ID token vào chat/báo cáo.
- Chỉ chuẩn bị kịch bản ở lượt này; chưa tự tạo account/term/group, chưa đổi mật khẩu, chưa thực hiện restore hay archive dữ liệu.
- Tách phiên đăng nhập bằng các browser profile hoặc cửa sổ riêng. Hai tab bình thường dùng cùng localStorage có thể ghi đè phiên của nhau. Postman giữ token riêng theo vai trò.
- Mỗi lần đổi role/token trong Swagger, bấm Authorize lại. Với Bearer scheme chỉ dán giá trị accessToken, không nhân đôi chữ Bearer.
- FE: mở Developer Tools → Network → Fetch/XHR, giữ log; ghi URL, method, request body, HTTP status và JSON response. Không đưa token vào ảnh chia sẻ.

### Bộ tài khoản riêng

| Ký hiệu | Vai trò | Email mẫu | Dùng để |
|---|---|---|---|
| A | ADMIN hiện hữu | Tài khoản admin local của bạn | Tạo dữ liệu, quản trị |
| I1, I2 | INSTRUCTOR | uat.i1@example.com, uat.i2@example.com | Owner và người không có quyền |
| M1, M2 | MENTOR | uat.m1@example.com, uat.m2@example.com | Lịch và phạm vi mentor |
| S1…S7 | STUDENT | uat.s1@example.com … uat.s7@example.com | Leader, thành viên, người ngoài nhóm, quota |

Dùng mã profile riêng UAT-I1/UAT-M1/UAT-S1…; nếu đã tồn tại, đổi suffix cho toàn bộ lượt chạy. Email mẫu không dùng nhận thư hoặc Google. Mật khẩu do bạn chọn, lưu riêng; không đưa vào tài liệu kết quả.

### Biến phải ghi lại

| Biến | Lấy từ đâu / lưu ý |
|---|---|
| TOKEN_A, TOKEN_I1, TOKEN_M1, TOKEN_S1… | `data.accessToken` khi login; refresh token lưu riêng |
| A_S1, A_M1, A_I1… | Account ID: `GET /api/admin/users`, `data.id` ở detail |
| S1_ID, S2_ID… | Student profile ID: `data.studentProfile.id`; KHÔNG phải account ID |
| M1_ID, I1_ID | Mentor/instructor profile ID, phân biệt account ID |
| TERM | Mã kỳ test, ví dụ `UAT0927A` (đổi suffix mỗi lượt) |
| G1, G2, G3 | ID nhóm từ POST group; tên Flowzy UAT A/B/C |
| P1, P2, D1 | Problem ID / proposal ID / domain code UAT-D1 |
| B1, T1, C1, K1 | Board ID, task ID, comment ID, checklist item ID |
| MS1, MS2 | Milestone ID trong đúng scope I1 + TERM + EXE101 |
| SLOT1, MEET1 | Slot ID và meeting ID: không thay thế cho nhau |
| FID, NID, BID | Feedback ID, notification ID, import/backup job ID theo từng API |
| VERSION | Luôn lấy từ GET task mới nhất trước thao tác ghi |

**Bẫy ID quan trọng:** PATCH `/api/groups/{groupId}/mentor` có field `mentorId` nhưng nghiệp vụ Java/Flowzy đang nhận **mentor account ID**. `instructorId` tại API gán instructor cũng nhận **account ID**. Các contribution/assignee/leader nhận **student profile ID**. Không suy từ tên field.

### Cách ghi kết quả

Mỗi case ghi: ngày giờ, role, ID test, URL/method, body đã ẩn secret, status HTTP, code/message/data thực tế, dữ liệu trước/sau, PASS/FAIL/BLOCKED và ảnh/log. NOT RUN khác BLOCKED; thiếu fixture legacy ghi BLOCKED, không PASS với mảng rỗng.

Response JSON thường có dạng:

```json
{"code":200,"message":"...","data":{}}
```

Không ép mọi response thành object: data có thể null, số, mảng hoặc PageResponse. Khi HTTP 201 tạo task/board/comment, `code` vẫn là 200 theo APIResponse.success Java. Import HTTP/code 202. File tải xuống là bytes, không envelope. Timestamps ISO-8601; enum viết hoa. Pagination kiểm `content,page,number,size,numberOfElements,totalElements,totalPages,hasNext,hasPrevious`.

## F01. Đăng nhập, Google, refresh, logout và profile

Controller: Auth (C07), Profile (C33). FE `/login`, `/change-password`, `/{role}/profile`.

1. A đăng nhập email/password: `POST /api/auth/login` với `{"email":"<email>","password":"<password>"}` → HTTP/code 200, message `Login successful`, data gồm accessToken/refreshToken/tokenType=Bearer/expiresIn. Không đòi profile trong response này.
2. `GET /api/auth/me` với token → đúng role/status/email/mustChangePassword; instructor có instructorProfile. `GET /api/profile/me` → profile đúng vai trò, không lộ passwordHash.
3. Sai password → 401 `Invalid email or password`; thiếu email/password → 400. API bảo vệ không token hoặc token hỏng → 401. Không dùng một lỗi 401 để kết luận đúng toàn bộ auth.
4. Tạo tài khoản UAT bằng F02; login lần đầu có mustChangePassword=true. Gọi API thông thường → 403 `Password change is required before using this feature`; me/logout/password change vẫn đi qua gate.
5. `PATCH /api/profile/me/password` body `{"currentPassword":"<mat-khau-cu>","newPassword":"<mat-khau-moi>"}`. DTO backend không có confirmPassword; ô xác nhận nếu có là kiểm tra của FE. Sai current password → lỗi; đúng →200, cờ phải đổi mật khẩu được bỏ, refresh cũ bị thu hồi. Login lại bằng mật khẩu mới; mật khẩu cũ bị từ chối. Không mong access token hiện tại bị blacklist chỉ vì đổi password.
6. `PATCH /api/profile/me` theo profile hiện có: đổi phone/fullName và đọc lại. Kiểm field không gửi giữ nguyên theo DTO/nhánh role; blank field được trim/null theo đặc tả. Không thay email/role qua endpoint self profile nếu DTO không hỗ trợ.
7. Refresh bằng `POST /api/auth/refresh` body `{"refreshToken":"<token cũ>"}` →200 `Token refreshed successfully`, cặp token mới. Gọi lại refresh cũ →401. Token mới không giống token cũ.
8. Logout với access token mới →200 `Logged out successfully`, data=null. Dùng token logout gọi me →401; refresh của account bị từ chối. Login lại để tiếp tục.
9. Google: dùng tài khoản Google thật có email đã tạo/import ACTIVE, chọn nút trên FE; Network có `POST /api/auth/google` body `{idToken}` →200 `Google login successful`, sau đó me đúng role. Không paste access token Flowzy vào idToken.
10. Google email chưa đăng ký →401 `Account is not registered in the system`; sai audience →401 `Google token audience is invalid`. Không tạo account mới tự động. Cấu hình Google Console không được kiểm chứng chỉ bằng health API.

Lưu ý clone: Java refresh không kiểm ACTIVE thêm trong controller, Flowzy có kiểm. Login account inactive có thể khác message Java. Ghi riêng sai lệch, không tự nới bảo mật để làm test xanh.

## F02. Admin quản lý tài khoản và tạo fixture

Controller AdminUser (C06). FE `/admin/users`.

Tạo I1 bằng `POST /api/admin/users`:

```json
{
  "email":"uat.i1@example.com",
  "role":"INSTRUCTOR",
  "initialPassword":"<mat-khau-test-tu-chon>",
  "instructorProfile":{"instructorCode":"UAT-I1","fullName":"Giang vien UAT 1","department":"UAT"}
}
```

Tạo S1 tương tự, role STUDENT và `studentProfile:{"studentCode":"UAT-S1","fullName":"Sinh vien UAT 1","className":"UAT101"}`. M1 dùng role MENTOR và `mentorProfile:{"mentorCode":"UAT-M1","fullName":"Mentor UAT 1"}`. Lặp cho các actor còn lại; chỉ gửi profile đúng role. Mật khẩu mẫu trong ngoặc phải được thay bằng giá trị thật (ít nhất 6 ký tự theo DTO).

| Case | Thao tác | Kết quả cần kiểm |
|---|---|---|
| U01 | POST tạo đúng dữ liệu | HTTP 200, ACTIVE, mustChangePassword=true; account+profile cùng tồn tại |
| U02 | GET users `?role=STUDENT&search=UAT&page=0&size=10`; GET detail | Lọc/paging đúng; profile ID khác account ID; không lộ hash |
| U03 | POST trùng email/code; thiếu profile đúng role | Bị từ chối, không sinh account mồ côi |
| U04 | PATCH user | Gửi email,status,mustChangePassword và profile đúng role; account/profile đồng bộ |
| U05 | POST `/{id}/reset-password` `{newPassword}` | 200; phải đổi password=true; refresh trước reset không dùng được |
| U06 | POST `/change-password` `{email,newPassword}` trên user UAT phụ | 200; mustChangePassword=false; refresh cũ bị thu hồi |
| U07 | DELETE một user UAT không dùng cho main flow | 200 data=null; account/profile INACTIVE, không xóa lịch sử |
| U08 | A tự disable/delete chính mình | Bị từ chối; A vẫn đăng nhập/quản trị được |
| U09 | S1/M1/I1 gọi admin users | 403, không thay dữ liệu |

Cho từng actor đổi mật khẩu theo F01 trước các bước tiếp theo. Không disable S1–S7/I1/M1 trước khi hoàn tất main flow.

## F03. Import — mentor, student account, student-group, problem bank

Controller Import (C19), StudentAccountImport (C34), ProblemImport (C32). FE `/admin/imports`.

1. A tải đủ bốn template: GET `/api/imports/templates/mentors`, `/student-accounts`, `/students`, `/problem-bank`. HTTP200, XLSX mở được, header attachment có filename, không phải JSON giả dạng XLSX.
2. Kiểm lịch sử sẵn có: GET `/api/imports/1`, `/api/imports/1/errors` →105/101/4; #2 →15/15/0. Với bộ file đã dùng, mentor lỗi dòng18/38 chứa hai email,95 email sai,105 trùng. Đây là fixture lịch sử, không lặp POST mong thành công lần nữa.
3. Khi test import mới, dùng bản sao template và mã/email riêng. POST đúng endpoint với multipart field **file**. Không tự đặt Content-Type multipart thiếu boundary.
4. Nhận HTTP202, lấy **batchId**, poll GET `/api/imports/{batchId}` đến COMPLETED/FAILED. HTTP202 không phải hoàn tất. Kiểm totalRows=successRows+failedRows; đối chiếu account/profile thực tế.
5. File có 1 dòng hợp lệ và 1 email sai: chỉ dòng hợp lệ được lưu; batch có thể COMPLETED nhưng failedRows>0. GET errors `?page=0&size=20&fieldName=email`, thử rowNumber/search/errorCode; page mặc định0, size1–100.
6. File trùng DB/file: lỗi từng dòng, không nhân account. File rỗng hoặc .txt →400 trước queue; header sai có thể chỉ FAILED sau queue. Gửi import thứ hai khi đang QUEUED/RUNNING →409, không chạy chồng.
7. Mentor import tạo account MENTOR+profile ACTIVE, không tạo group. Không có password thì sinh ngẫu nhiên, không được trả password trong response. Không dùng password phỏng đoán để login.
8. Student-account chỉ tạo/reactivate account+student; template chính xác `RollNumber,Fullname,Email,SubjectCode,GroupName`, SubjectCode EXE101/EXE201. **GroupName là lớp ở luồng này, không tạo student group hay leader/mentor.**
9. Student import `/api/imports/students` khác student-account: workbook group-list có term/course, nhóm/thành viên/leader; kiểm kế thừa ô nhóm, leader fallback warning, mentor code tìm đúng. Mentor phải import trước để gán được. Dùng bản sao template đúng sheet kỳ/môn; không đổi header tùy ý.
10. Problem-bank import `/api/imports/problem-bank`: domain được xử lý trước problem; kiểm create/upsert, lỗi difficulty, domain lạ và metadata; đọc lại qua API problems/domains. Không import file thật để thử ghi đè code đang sử dụng.
11. Sau import thử Google với email thật đã được cho phép. Import thành công không chứng minh email mẫu @example.com có tài khoản Google.

## F04. Học kỳ, catalog và sinh viên chưa có nhóm

Controller AdminTerm (C05), AcademicTerm (C01), RecruitmentRole (C17), Criteria (C30), ProblemDomain (C31), Student (C35).

1. Hệ thống chỉ cho một kỳ OPEN. Khi chưa có kỳ, A tạo một kỳ rỗng thử nghiệm bằng POST `/api/admin/terms` `{"code":"UAT-EMPTY"}`, rồi DELETE `/api/admin/terms/UAT-EMPTY` →200. Tiếp theo tạo kỳ chính với `{"code":"UAT0927A"}` →200 OPEN, ghi TERM. Đọc GET `/api/admin/terms?page=0&size=10` và GET `/api/terms/available`: kỳ chính hiện ở cả hai.
2. POST lại cùng mã kỳ đang OPEN sau trim/uppercase →200, trả kỳ hiện có, không nhân bản. Tạo một kỳ khác khi TERM còn OPEN →409; tạo lại mã đã CLOSED →409. Không đóng kỳ main flow trước F15. Code blank/quá dài →400 theo DTO; không tự giả định regex không có trong source.
3. GET `/api/group-recruitment-roles` →26 roles, đúng code/category/displayNameVi/displayNameEn/thứ tự. Chọn ROLE1 từ kết quả, không tự invent `BACKEND`/`FRONTEND` nếu không đúng enum.
4. GET `/api/problem-evaluation-criteria` →chỉ active, displayOrder, đủ câu hỏi/gợi ý/maxScore. GET `/api/problem-domains` rồi `?status=ACTIVE&search=UAT` →filter đúng; không tự loại INACTIVE khi không lọc.
5. S1 gọi GET `/api/students/ungrouped?term={TERM}&courseCode=EXE101&page=0&size=20&search=UAT` →S1… chưa tham gia scope hiện ra. Bỏ term hoặc courseCode →400. ID detail `/api/students/{S2_ID}` trả đúng student+account ACTIVE; không dùng A_S2.
6. M1 gọi API student →403; ID không tồn tại với caller hợp lệ →404. Sau khi S2 vào G1 ở F06, S2 không còn trong ungrouped cùng TERM/EXE101, nhưng kiểm scope môn khác độc lập.

## F05. Tạo nhóm, tìm nhóm, phân công giảng viên/mentor

Controller Group (C12), AdminGroup (C03), InstructorGroupBoard (C20). FE `/student/groups`, `/admin/groups`, `/instructor/groups`, `/mentor/groups`.

S1 POST `/api/groups`:

```json
{"term":"UAT0927A","courseCode":"EXE101","name":"Flowzy UAT A","projectName":"Flowzy UAT Project","ideaDescription":"Du an kiem thu","requiredGpa":2.5,"targetGrade":8,"recruitmentNeeds":[]}
```

1. HTTP200 `Group created successfully`, lấy G1. S1 là leader và member; default task board được tạo theo nghiệp vụ. S5 tạo G2, S6 tạo G3 cùng TERM/EXE101, tên khác nhau.
2. S1 tạo nhóm thứ hai cùng scope →conflict; tạo tên trùng sau normalize →conflict. Đọc GET `/api/groups/{G1}`: leader/members, termStatus, studentReadOnly=false, recruitmentNeeds, selectedProblem, mentor/instructor chưa gán.
3. GET `/api/groups`, S2 GET `/api/groups/discover?studentGpa=3&name=Flowzy`, GET `/api/groups/student/me`. S1 không được discover chính nhóm mình; GPA thấp hơn requiredGpa loại nhóm; enum/GPA không hợp lệ trả400.
4. S1 PATCH `/api/groups/{G1}` đổi tên/project/description; PATCH `/criteria` `{requiredGpa:3,targetGrade:9,recruitmentNeeds:[{role:ROLE1,quantity:2}]}`. Đọc lại nhãn role; quantity1–6, tổng≤6, GPA0–4, target0–10. S2 chưa là leader sửa →403; empty criteria →400.
5. I1 GET `/api/instructor/groups/board?term={TERM}&assignment=AVAILABLE` →thấy G1/G2/G3 khi ACTIVE/có member/kỳ OPEN. Search theo student code/name; summary không chỉ tính trang hiện tại.
6. I1 POST `/api/instructor/groups/{G1}/claim` →200, gán I1. Gọi lại chính I1 →200/no-op; I2 claim G1 →409. I1/I2 gửi đồng thời claim G2 →chỉ một owner; dùng nhóm phụ và ghi kết quả.
7. A PATCH `/api/groups/{G1}/instructor` `{"instructorId":A_I1}`; PATCH `/mentor` `{"mentorId":A_M1}`. **Thay A_I1/A_M1 bằng số account ID**, không copy ký hiệu như JSON literal. Gán G2/G3 cho I1 và M1 để test scope/booking cạnh tranh.
8. GET `/api/groups/instructor/me?term={TERM}&courseCode=EXE101` với I1 và `/api/groups/mentor/me` với M1 →đúng nhóm được giao; I2/M2 không thấy nhóm của I1/M1.
9. A GET `/api/admin/groups?status=ALL&search=Flowzy&page=0&size=20` →đúng paging, account/profile ID và labels. `status=INVALID` →400; size0/101 →400.
10. Trên nhóm phụ, A DELETE `/instructor`, DELETE `/mentor` →200, detail đã clear; gán lại. Nhóm inactive/không thành viên không được nhận assignment. Không gỡ I1 main flow giữa chấm điểm trừ khi đang test reassignment.

## F06. Invitation, join request, thành viên và khóa nhóm

Controller Invitation (C13), JoinRequest (C14), Group (C12). Giữ G1 tối thiểu S1+S2 để test grade về sau.

| Bước | Người gọi / API | Kết quả mong đợi |
|---|---|---|
| 1 | S1 POST `/api/groups/{G1}/invitations` `{studentCodeOrEmail:"UAT-S2",message:"Moi tham gia"}` | 200, PENDING, invitee=S2; ghi invitationId |
| 2 | S1 GET `/{G1}/invitations`; S2 GET `/api/groups/invitations/me` | Cùng lời mời PENDING; actor khác không đọc như owner |
| 3 | S2 POST `/api/groups/invitations/{id}/accept` | 200 data=null; membership thêm đúng1; invitation ACCEPTED; ungrouped cập nhật |
| 4 | Mời S3 rồi S3 `/decline` | DECLINED, không thêm membership |
| 5 | Mời S4 rồi S1 `/cancel` | CANCELED; không thêm membership; kiểm message `cancelled` theo source |
| 6 | S3 POST `/api/groups/{G1}/join-requests` `{message:"Xin vao nhom"}` | PENDING; đọc qua group list và `/api/groups/join-requests/me` |
| 7 | S1 POST `/api/groups/{G1}/join-requests/{id}/approve` | 200; status **ACCEPTED**, membership S3 được thêm |
| 8 | S4 xin vào rồi S1 `/reject`; lượt mới S4 `/cancel` | REJECTED/CANCELED, membership không tăng |
| 9 | Dùng API alias `/api/groups/join-requests/{id}/approve|reject|cancel` | Cùng nghiệp vụ; tạo request mới cho từng thử, không dùng request đã terminal |
| 10 | S1 PATCH `/api/groups/{G1}/lock` `{isLock:true}` | 200, isLock=true; invite/join/approve/remove/leave theo guard bị chặn |
| 11 | Mở khóa `{isLock:false}` | 200; group writable trở lại |
| 12 | S1 PATCH `/api/groups/{G1}/leader` `{studentId:S2_ID}` | 200; S2 là leader, role thành viên nhất quán; S1 mất quyền leader |
| 13 | S2 chuyển lại cho S1 | Main flow trở về S1 leader |
| 14 | S1 DELETE `/{G1}/members/{S3_ID}` | 200; S3 bị loại; task assignment của S3 được dọn theo nghiệp vụ |
| 15 | Cho S3 vào lại rồi S3 DELETE `/{G1}/members/me` | 200; rời nhóm, leader được notification |
| 16 | Cho S3 vào lại rồi S3 POST `/{G1}/leave` | Alias rời nhóm giống bước15; không làm trên S1/S2 |

Thử lỗi: accept invitation người khác; approve sai groupId; request terminal lần2; nhóm đầy6 người; member đã ở nhóm khác cùng scope; leader rời khi vẫn còn member; period CLOSED. Không tự áp isLock lên mọi API: Java transferLeader và cancel/reject có các ngoại lệ; đọc từng case trong checklist. Test race accept2group cùng scope phải chỉ tạo một membership; chạy trên S7 và nhóm phụ, tránh phá main flow.

## F07. Problem bank, đề xuất và duyệt

Controller AdminProblem (C04), Problem (C29), Domain (C31), Criteria (C30), GroupProblem (C16), InstructorProblem (C21). FE `/admin/problems`, `/student/problems`, `/instructor/problems`.

1. A POST `/api/admin/problem-domains` `{"code":"UAT-D1","name":"UAT Domain","description":"Mien test"}` →200 ACTIVE, code uppercase. PATCH `/{id}` đổi metadata, GET domains kiểm lại. Trùng code ignore-case →409.
2. A POST `/api/admin/problems` `{"code":"UAT-P1","title":"UAT Official","statement":"Mo ta bai toan UAT","difficultyLevel":"BEGINNER","domainCode":"UAT-D1"}` →200 OFFICIAL/ACTIVE, lưu P1. Đọc GET `/api/problems/{P1}` và list `?search=UAT&domainCode=UAT-D1&difficulty=BEGINNER&page=0&size=10`.
3. A PATCH `/api/admin/problems/{P1}` đổi title/expectedOutput; domainCode blank kiểm clear domain trên problem phụ. PATCH `/{P1}/status` `{status:"INACTIVE"}`, đọc lại, rồi bật ACTIVE. Domain optional khi create, nhưng nếu truyền thì phải ACTIVE/tồn tại.
4. S1 POST `/api/groups/{G1}/problems/select` `{problemId:P1}` →200, group.selectedProblem=P1. Member không leader →403. Chọn INACTIVE hoặc SELF_PROPOSED chưa duyệt →lỗi nghiệp vụ. DELETE cùng route →200, selection null.
5. S1 POST `/api/groups/{G1}/problems/propose` body title,statement,difficultyLevel,domainCode như dưới →200 SELF_PROPOSED/PENDING_REVIEW, mã SP tự sinh, tự chọn proposal cho G1.

```json
{"title":"UAT Proposal","statement":"Bai toan do nhom de xuat","difficultyLevel":"BEGINNER","domainCode":"UAT-D1","expectedOutput":"Prototype"}
```

6. GET `/api/groups/{G1}/problems/proposals`, PUT `/api/groups/{G1}/problems/proposals/{problemId}` với đầy đủ body như khi propose. Sửa khi pending; proposal của nhóm khác phải bị từ chối. Với proposal phụ chưa review, DELETE rồi kiểm proposal và selection.
7. I1 GET `/api/instructor/problems/pending` thấy proposal G1; I2 không thấy. I2 PATCH review của G1 →403; I1 PATCH `/api/instructor/problems/{P2}/review` `{status:"APPROVED",comment:"Dat"}` →200, kết quả **OFFICIAL/ACTIVE** (không còn status APPROVED).
8. Tạo proposal mới để thử REJECTED: thiếu comment bị từ chối; có comment →REJECTED và bỏ selection liên quan. Duyệt lại một proposal đã xử lý →lỗi, không đổi quyết định cũ.
9. A thử cùng nghiệp vụ qua PATCH `/api/admin/problems/{id}/review` trên proposal riêng; kiểm người duyệt/thời điểm. Đọc criteria active để kiểm màn hình đánh giá dùng đúng nhãn và maxScore.

## F08. Task board, task, checklist, comment, activity và version

Controller TaskBoard (C37), GroupTask (C18). FE `/student/tasks`.

1. S1 GET `/api/groups/{G1}/boards` và alias `/task-boards` →có default board. Nếu chưa có, GET có thể khởi tạo default; không đếm GET này là hoàn toàn không ghi.
2. POST `/boards` `{"name":"UAT Sprint 1"}` →HTTP201/code200, lưu B1. PATCH `/boards/{B1}` đổi name/position; đọc qua alias `/task-boards/{B1}` bằng PATCH và list. Không tự ép position rule của task lên board.
3. S1 POST `/api/groups/{G1}/tasks`:

```json
{"title":"UAT Task 1","description":"Kiem thu board","status":"TODO","priority":"HIGH","assigneeStudentIds":[123],"boardId":456}
```

Thay123 bằng S2_ID,456 bằng B1. HTTP201/code200, lấy T1/version/position. DueAt nếu gửi phải là ISO tương lai. Create chỉ BACKLOG/TODO; không tạo thẳng DONE.

4. GET `/api/groups/{G1}/board?boardId={B1}` và `/api/groups/{G1}/boards/{B1}` →cùng board, columns theo enum, active/overdue counts. Lọc priority/assigneeStudentId/search/includeArchived; GET task detail đúng assignee/checklist.
5. PATCH `/tasks/{T1}` `{"title":"UAT Task 1 revised","version":<VERSION>}` →200; đọc version mới. Gửi lại version cũ với thay đổi khác →409, dữ liệu mới không bị ghi đè. Thiếu version →400. Clear deadline bằng clearDueAt=true; omitted dueAt không có nghĩa xóa.
6. PUT `/tasks/{T1}/assignees` `{assigneeStudentIds:[S1_ID,S2_ID],version:VERSION}` →đúng tập assignment. Gán student ngoài nhóm →lỗi; không nhầm account ID. GET `/api/tasks/me?groupId={G1}` với S2 thấy T1.
7. PATCH `/tasks/{T1}/move` `{status:"IN_PROGRESS",position:0,version:VERSION}` →200, thứ tự liên tục trong cột. Tạo T2 rồi kéo qua REVIEW/DONE, kiểm board và activity. POST `/tasks/reorder` `{taskId:T1,targetStatus:"TODO",targetIndex:0}` →200 data=null. Không thêm version nếu DTO reorder không có field đó.
8. POST `/tasks/{T1}/checklist-items` `{title:"Kiem tra API"}` →201/code200, lấy K1. PATCH `/{K1}` `{completed:true}` →completedAt/completedBy, progress đổi; sửa title/position rồi DELETE →200 data=null. Sai checklist ID thuộc task khác →không truy cập chéo.
9. POST `/tasks/{T1}/comments` `{content:"UAT comment"}` →201/code200, lấy C1. GET comments có paging; author PATCH `/{C1}` đổi content →editedAt. Người khác không được sửa; delete chỉ author/student leader theo quyền nguồn. Body blank →400.
10. GET `/tasks/{T1}/activities?page=0&size=20` →newest-first, actor/details đúng các thao tác. Java vẫn ghi TASK_MOVED cho no-op move, nhưng entity version không tự tăng nếu không đổi và chỉ notify khi đổi status. Không áp một quy tắc no-op chung cho move và comment; đối chiếu riêng từng method trong checklist.
11. DELETE `/tasks/{T1}` →archive, không xóa vật lý. Board mặc định ẩn; includeArchived=true thấy archivedAt; task archived không sửa như active. POST `/restore` →active trở lại.
12. Xóa board còn task (kể cả archive theo quy tắc nguồn) →409. Tạo board phụ rỗng để DELETE thành công; default board không được xóa. Chạy cả alias boards/task-boards trên fixture mới.
13. S7 ngoài nhóm đọc/ghi task →403. M1 được gán có quyền read/comment; không mặc định cấp quyền đó cho ADMIN hoặc INSTRUCTOR vì Java kiểm student member/assigned mentor. Hai client PATCH cùng version →một thành công, một conflict; không mất nội dung hoặc trùng position.

## F09. Mentor availability, booking, evidence và báo cáo

Controller MentorAvailability (C23), GroupMeeting (C15), MentorMeetingReport (C24). FE `/mentor/availability`, `/mentor/meetings`, `/student/groups`.

1. M1 POST `/api/mentor/availability` với startAt/endAt ngày tương lai, đúng60phút, phút bắt đầu00 hoặc30, giây0, cùng ngày Việt Nam. Ví dụ thay ngày bằng ngày bạn test:

```json
{"startAt":"2026-10-01T09:00:00+07:00","endAt":"2026-10-01T10:00:00+07:00","meetLink":"https://meet.google.com/abc-defg-hij","note":"UAT booking"}
```

Link trên chỉ là chuỗi đúng định dạng để test validation, không khẳng định phòng Google Meet tồn tại. API không tự tạo phòng Google Meet.

2. HTTP200 AVAILABLE, lấy SLOT1. GET `/api/mentor/availability` và `/me` cùng dữ liệu M1; M2 không thấy. PATCH slot đổi note/time; overlap với slot chưa CANCELED bị từ chối. 45phút, bắt đầu09:15, qua ngày Việt Nam hoặc link sai regex →400.
3. S1 GET `/api/groups/{G1}/mentor/availability` →chỉ AVAILABLE tương lai của mentor được gán. S2 cùng nhóm đọc được; người ngoài nhóm không được.
4. S1 POST `/api/groups/{G1}/mentor/meetings` `{"slotId":SLOT1}` →200 `Meeting booked successfully`, meeting SCHEDULED, slot BOOKED. S2 không phải leader book →403; book lại slot/nhầm mentor/slot quá khứ →lỗi nghiệp vụ. Nhóm tối đa2 meeting SCHEDULED/COMPLETED.
5. Hai leader G1/G2 gửi booking cùng slot khác →chỉ một được, một409; GET chứng minh chỉ một meeting và slot BOOKED. Không dùng cùng phiên browser token.
6. GET `/api/groups/{G1}/mentor/meetings` và `/{MEET1}` cho member/M1/I1; M2/I2 không được đọc. Mentor PATCH meeting cần meetLink theo DTO dù chỉ sửa note; null note giữ cũ, blank note xóa.
7. Trước startAt, PATCH `/{MEET1}/confirm` bị chặn. Sau startAt, S1 và M1 confirm lần lượt; timestamp đầu tiên giữ khi retry. Confirm không thay cho evidence và không tự kết luận COMPLETED.
8. Sau endAt, member S2 PUT `/{MEET1}/evidence` `{"imageUrl":"https://example.com/uat-evidence.png"}` →200, COMPLETED, ghi người/thời điểm/evidence URL. Đây là URL test, không có upload file trong API này. Trước endAt, thiếuURL, gửi evidence lần2 hoặc CLOSED term →bị chặn.
9. Để không chờ lịch tương lai, M1 có thể tạo **direct meeting test đã kết thúc** bằng POST cùng route với startAt/endAt/meetLink, không slotId. Java không cấm giờ quá khứ cho direct create. Dùng giờ không trùng, đúng60phút, nằm trong quota. Direct meeting không hỗ trợ confirm qua slot nhưng dùng được evidence sau endAt.
10. M1 cancel một meeting phụ chưa completed bằng PATCH `/{id}/cancel` `{reason:"UAT cancel"}` →CANCELED, slot CANCELED (không AVAILABLE). Student không được cancel; completed/evidence không được cancel.
11. Mentor DELETE availability phụ chưa BOOKED →200; BOOKED không được sửa/hủy trực tiếp qua slot endpoint. Không nhầm hủy slot với hủy meeting.
12. M1 GET `/api/mentor/meeting-reports/terms`; GET `/api/mentor/meeting-reports/export.xlsx?term={TERM}` →200 file2sheet đúng scope mentor, giờ Việt Nam, có lịch sử trạng thái. Sai/kỳ không có quyền kiểm theo checklist; M2 không được lấy báo cáo của M1.

## F10. Timeline và milestone

Controller CourseMilestone (C09), Group timeline (C12). FE `/instructor/milestones`.

I1 phải được gán G1 trong TERM/EXE101 trước khi tạo:

```json
{"term":"UAT0927A","courseCode":"EXE101","title":"UAT Checkpoint 1","description":"Ban dau","weight":40,"maxScore":10,"deadlineAt":"2026-10-10T17:00:00+07:00"}
```

1. POST `/api/instructor/milestones` →HTTP200 ACTIVE/typeTIMELINE, position tự tăng, ghi MS1; tạo MS2 weight60, maxScore10, title khác. Đổi ngày nếu đã qua tại thời điểm chạy.
2. GET `/api/instructor/milestones?term={TERM}&courseCode=EXE101`; GET `/{MS1}`; S1 GET `/api/course-milestones?term={TERM}&courseCode=EXE101`; GET `/api/groups/{G1}/milestones` →cùng scope, sortterm/course/position/id. S1 gọi prefix instructor →403.
3. Tạo title trùng ignore-case hoặc tổng ACTIVE weight>100 →lỗi; thiếu deadline→400. I2 không assigned group trong scope không được create. maxScore0/weight101/position-1 →400.
4. PATCH và PUT qua cả hai prefix phải cùng nghiệp vụ owner; I2 sửa MS1 →403. Update cần title theo DTO; field description/weight bị bỏ có thể trở thành null, không phải PATCH giữ tất cả omitted fields.
5. Kiểm alias dueDate tương đương deadlineAt. Deadline đổi phải tính lại late của legacy submission theo `submittedAt > deadline` (bằngdeadline khônglate); cần fixture legacy ở F12.
6. Tạo milestone phụ weight0 để thử DELETE: status ARCHIVED, không xóa lịch sử. Student list ẩn ARCHIVED/INACTIVE; student detail INACTIVE khác ARCHIVED theo đặc tả. Không archive MS1/MS2 trước F11.
7. Bốn alias GET/POST `/{milestoneId}/outcomes` →**410 Gone** với caller đã qua quyền, không coi là chức năng thiếu. **Phần mở đầu đặc tả cũ có câu gộp outcomes/bulk404 không chính xác; method hiện tại outcomes410, bulk404.**

## F11. Contribution, agreement, grade matrix và export điểm

Controller GradeMatrix (C26). FE `/student/grades`, `/instructor/grading`.

Giữ G1 đúng hai member ACTIVE S1/S2; nếu đã thêm member khác, phải đưa đủ mọi member ACTIVE vào request. Contribution là tỷ lệ áp dụng cho từng người, **không phải chia chiếc bánh có tổng100**.

1. S1 PUT `/api/groups/{G1}/milestones/{MS1}/contributions`:

```json
{"items":[{"studentId":123,"contributionPercent":100},{"studentId":124,"contributionPercent":80}]}
```

Thay123/124 bằng S1_ID/S2_ID. HTTP200; tạo revision mới, clear agreement trước, calculatedScore chưa có. Thiếu/trùng/thừa member, percent<0/>100 →400. Tổng180 là hợp lệ trong nghiệp vụ Java.

2. S1/S2 GET `/api/groups/{G1}/grades`: đúng revision, requiredCount2, approvedCount0. S1 không tự được AGREE vì là leader.
3. S2 PUT `/contribution-agreement` `{"decision":"REQUEST_CHANGES","reason":"Can dieu chinh"}` →200; grade bị chặn. Response AGREE tiếp khi có REQUEST_CHANGES cũng bị chặn cho tới revision mới.
4. S1 gửi lại contributions →revision tăng, xóa agreement cũ. S1 và S2 mỗi người PUT agreement `{"decision":"AGREE"}` →approvedCount2, trạng thái đủ đồng thuận.
5. I1 PUT `/api/instructor/milestones/{MS1}/groups/{G1}/grade` `{"score":8,"feedback":"Dat checkpoint 1"}` →200. Score>maxScore hoặc score<0 →400; I2 không owner/không assigned →bị chặn; chưa unanimity khi chấm lần đầu →409.
6. Kiểm số: group score8; S1=8×100%=8; S2=8×80%=6.4. Phần đóng góp Final của MS1: S1=8/10×10×40%=3.2, S2=2.56. Chưa MS2 nên matrix.complete=false.
7. MS2 contributions100/100, cả hai AGREE, I1 grade9. Kết quả Final **S1=8.6; S2=7.96**, matrix.complete=true. Kiểm công thức snapshot và làm tròn HALF_UP scale8 rồi4; không lấy trung bình đơn giản (8+9)/2.
8. I1 sửa score đã có →không bắt lại first-grade agreement; snapshot và calculatedScore cập nhật. Gửi lại score/feedback không đổi →không tạo notification trùng. Không tự suy rằng mọi số JSON phải có cùng số0 thập phân; so giá trị và precision contract.
9. GET `/api/instructor/grades/export.csv?term={TERM}&courseCode=EXE101&groupId={G1}` →CSV UTF-8 BOM, CRLF, attachment filename. XLSX tương tự `/export.xlsx` →sheet RAW, cột milestone đúng scope, số thật, Final trống nếu incomplete, không phải0 giả.
10. GET matrix trước khi assigned instructor →409; scope owner cũ/new owner phải kiểm theo điều kiện nguồn. Race contribution/grade/membership chạy nhóm phụ; không nhận điểm nửa transaction hoặc grade bỏ qua agreement.

## F12. Legacy submissions, legacy grades và average

Controller MilestoneSubmission (C27), InstructorSubmission (C22), MilestoneGrade (C25), StudentGroupGrade (C36). FE `/student/submissions`, `/instructor/submissions`.

**Các API tạo/sửa submission và legacy grade đã bị comment trong Java. Không tự invent POST để có dữ liệu, không mở lại endpoint.** Cần database test riêng có fixture legacy được chuẩn bị/khôi phục an toàn. DB local hiện tại không có bộ dữ liệu này; ca có dữ liệu ghi BLOCKED nếu chưa có fixture.

1. Với G1 hợp lệ chưa submission: GET `/api/milestone-submissions/groups/{G1}` có thể200 mảngrỗng; đó chỉ là ca empty, không phải pass luồng nộp bài.
2. Khi có fixture SUB1 thuộc G1/MS1: GET `/api/milestone-submissions/{SUB1}`, `/groups/{G1}`, `/milestones/{MS1}` →đúng nội dung/status/late/grade metadata. Member đúng nhóm và instructor hiện được giao có quyền; người ngoài không đọc được.
3. I1 GET `/api/instructor/submissions?term={TERM}&courseCode=EXE101&groupId={G1}&milestoneId={MS1}&late=true` →đòi cả owner milestone và assigned instructor hiện tại. Reassign trên fixture phụ phải loại dữ liệu ra khỏi scope owner cũ.
4. GET `/api/milestone-submissions/{SUB1}/grades`: không có grade→404; có→đúng score/maxScore/feedback/instructor/gradedAt. GET `/api/milestone-submissions/groups/{G1}/grades` →list legacy, không phải matrix mới.
5. POST `/api/milestone-submissions/bulk-grade` với caller hợp lệ →404 `Bulk grading not supported`, không phải bug triển khai thiếu.
6. GET `/api/student-groups/{G1}/average-grade`: ví dụ legacy score8 weight40 vàscore9 weight60 →8.60; loại milestoneINACTIVE vàweightnull; tổngweight0→0.00; HALF_UP2. Không dùng Final contribution matrix F11 thay cho kết quả này.
7. Nếu UI vẫn cung cấp nút submit/bulk-grade tới API đã bị bỏ, ghi lỗi FE–contract; không mark backend phải tạo mới endpoint trái Java.

## F13. Notification và STOMP realtime

Controller Notification (C28), WebSocket `/ws`. FE icon chuông và `/{role}/notifications`.

1. S2 mở thông báo trước khi S1 gửi invitation hoặc I1 chấm điểm trên fixture phụ. GET `/api/notifications?unreadOnly=true&page=0&size=20`, `/unread-count` →chỉ recipient hiện tại; status/page/count chính xác.
2. PATCH `/api/notifications/{NID}/read` →read=true, readAt; gọi lại không giảm count lần2. NID của người khác không được sửa.
3. PATCH `/api/notifications/read-all` →data là **số unread còn lại**, thường0; không phải số dòng vừa đổi. GET lại count/list phải nhất quán.
4. Developer Tools →Network→WS: kết nối `ws://localhost:8080/ws`, STOMP CONNECT gửi Authorization Bearer, nhận CONNECTED; SUBSCRIBE `/user/queue/notifications`. Không dùng giao thức SignalR.
5. Actor khác tạo event: phải nhận STOMP MESSAGE đúng recipient/event/action params mà không F5/poll. Kiểm người ngoài không nhận; không subscribe queue của người khác; client SEND trái phép phải bị từ chối.
6. **Rủi ro đã thấy trong source Flowzy:** handler hiện xử lý CONNECT/SUBSCRIBE/RECEIPT nhưng chưa thấy đường phát MESSAGE cho notification sau commit. Việc GET list có dữ liệu hoặc UI polling đổi count không chứng minh realtime PASS. Ghi FAIL/BLOCKED theo bằng chứng khi chạy.

## F14. Dashboard theo vai trò và TV

Controller Dashboard (C10), đủ15 API. FE `/admin/dashboard`, `/mentor/groups`, `/instructor/dashboard`, `/student/dashboard`, `/admin/tv-display`.

| Vai trò | GET API | Đối chiếu |
|---|---|---|
| A | `/api/dashboard/admin/groups` | Tiến độ theo task thực, không lấy số điểm thay task |
| A | `/api/dashboard/admin/projects` | Nhóm/problem/project name fallback theo đặc tả |
| A | `/api/dashboard/admin/mentors` | Count nhóm/meeting của từng mentor |
| A | `/api/dashboard/admin/timeline` | Meeting đúng giờ/trạng thái/phạm vi |
| A | `/api/dashboard/admin/execution-status` | Counts từng trạng thái, đủ enum keys viết HOA |
| A | `/api/dashboard/admin/overview?term={TERM}&courseCode=EXE101&limit=5` | Tổng/hàngtop đúng scope; empty scope không lẫn dữ liệu khác |
| A đăng nhập | `/api/dashboard/tv-showcase/projects?term={TERM}&page=0&size=20` | Showcase đúng filter/phân trang; không mặc định public |
| A đăng nhập | `/api/dashboard/tv-showcase/recruitments?term={TERM}&page=0&size=20` | Nhu cầu tuyển và labels; nhóm locked theo rule nguồn |
| M1 | `/api/dashboard/mentor/groups` | Chỉ nhóm M1 được giao |
| M1 | `/api/dashboard/mentor/meetings?status=ALL` | So với meeting list; thử từng enum filter trong checklist |
| I1 | `/api/dashboard/instructor/milestones?term={TERM}&groupId={G1}` | Scope owner/assigned vàtimeline visibility |
| S1 | `/api/dashboard/student/groups` | Chỉ nhóm mình tham gia |
| S1 | `/api/dashboard/student/progress` | Tổng task theo group, overdue/status/priority |
| S1 | `/api/dashboard/student/projects` | Project/selection đúng nhóm của mình |
| S1 | `/api/dashboard/student/milestones?groupId={G1}` | Timeline đúngcurrent instructor; group ngoài quyền→lỗi |

Trước/sau khi chuyển task TODO→DONE, archive task, cancel meeting và đổi selectedProblem: tính tay tập dữ liệu liên quan rồi reload dashboard. Không mong tổng toàn hệ thống bằng fixture riêng khi có dữ liệu khác. Thử filter rỗng, trang cuối, size sai, role khác; TV khôngtoken→401. So sánh JSON keys, không chỉ xem đồ thị có hiển thị.

## F15. Đóng kỳ và feedback

Controller AdminTerm (C05), Feedback (C11), AdminFeedback (C02). FE `/admin/terms`, `/student/feedback`, `/mentor/feedback`, `/instructor/feedback`, `/admin/feedback`.

**Chỉ làm sau khi xong group/task/meeting/grade. Đóng kỳ main flow không có API reopen được đặc tả.** Nếu muốn test tiếp ghi student, tạo TERM mới.

1. A PATCH `/api/admin/terms/{TERM}/close` →200 CLOSED, closedAt/closedBy; snapshot feedback cho từng student và mentor/instructor đã được gán. G1 có2members và2targets thì kỳ test riêng có4 feedback dự kiến; nếu có G2/G3 thì cộng đúng các thành viên/targets liên quan.
2. GET `/api/terms/available` không còn TERM; admin term list vẫn có. Group detail studentReadOnly=true. Student ghi task/group/contribution/booking trong kỳ bị409; không áp chung lệnh cấm cho mọi read hoặc mọi mutation instructor.
3. S1 GET `/api/feedback/me?term={TERM}` thấy feedback của S1, ban đầuPENDING. PUT `/api/feedback/{FID}` `{"rating":5,"comment":" Mentor ho tro tot "}` →200 SUBMITTED, comment trim, submittedAt lần đầu.
4. Sửa cùngFID rating4/comment khác →200, submittedAt giữ lần đầu. Rating0/6/thiếu→400; S2 sửaFID củaS1→403; IDkhôngtồn tại→404.
5. M1/I1 GET `/api/feedback/received?term={TERM}&courseCode=EXE101` →chỉ SUBMITTED của mình; distribution1–5 có cảcount0, average2chữsố; **không có tên/MSSV/email của sinh viên**. Kiểm JSON, không chỉ UI ẩn cột.
6. A GET `/api/admin/feedback?term={TERM}&status=SUBMITTED&page=0&size=20` và filter targetType,targetId,targetSearch →có danh tính student/target. M1/S1 gọi admin feedback→403.
7. A GET `/api/admin/feedback/export.xlsx?term={TERM}` →2sheet Mentor Feedback/Instructor Feedback,15cột, chỉSUBMITTED, giờICT, filename theo kỳ; thiếuterm→400, kỳkhôngtồn tại→404.
8. Kiểm close retry và feedback idempotency trên kỳ phụ: không sinh snapshot trùng. Concurrency hai request feedback cập nhật phải tuân optimistic version nguồn; ghi thực tế nếu Flowzy xử lý khác.

## F16. Archive sinh viên và xóa kỳ — chạy cuối trên fixture riêng

1. Sau khi đóng kỳ chính, tạo riêng TERM_ARCHIVE và nhóm có student UAT phụ, không dùng sinh viên thật.
2. POST `/api/admin/terms/{TERM_ARCHIVE}/archive-students` khi OPEN →409. Đóng kỳ rồi gọi →200; chỉ archive student đủ điều kiện theo membership. Để kiểm trường hợp được giữ ACTIVE: đóng TERM_ARCHIVE, tạo kỳ OPEN mới, cho một student của kỳ cũ tham gia nhóm kỳ mới rồi mới archive kỳ cũ. Không đợi hoàn thành feedback vì Java không có quy tắc đó.
3. Kiểm account và student INACTIVE, refresh bị thu hồi, lịch sử còn nguyên; kiểm membership/assignment theo source. Student inactive không Google login được.
4. DELETE `/api/admin/terms/{TERM}` còn group/feedback →409, không xóa dây chuyền dữ liệu. Chỉ kỳ thực sự rỗng mới xóa được.
5. Không dùng DELETE account/term hay SQL truncate để dọn toàn hệ thống. Giữ record UAT để điều tra lỗi; xóa/restore chỉ theo phạm vi người vận hành xác nhận.

## F17. Backup, lịch chạy và restore an toàn

Controller Backup (C08). FE `/admin/backups`.

1. A GET `/api/admin/backups/schedule` →các field enabled,cronExpression,timezone,backupDir,retentionDays,lastTriggeredAt,nextRunAt,updatedBy...; GET có thể tạo cấu hình mặc định.
2. PUT `/schedule` `{"enabled":false,"cronExpression":"0 0 2 * * *","timezone":"Asia/Ho_Chi_Minh","retentionDays":14}` →200, nextRunAt=null. Cron là **6 trường kiểu Spring**, không phải5trường crontab. Thử cron/timezone sai, retention0/3651→400. Chỉ bật scheduler trên DBtest và tắt lại khi xong; cập nhật lịch không tạo job ngay.
3. POST `/api/admin/backups` không body →**HTTP 200**, job QUEUED. Lưu backupJobId; GET `/{jobId}` tới SUCCEEDED/FAILED. GET list với status/page/size đối chiếu job; đang backup/restore gửi thêm →409.
4. SUCCEEDED: GET `/{jobId}/download` →application/octet-stream, filename .dump, bytes không rỗng, không JSON. Job thiếu →404; chưa SUCCEEDED không download thành công.
5. **Restore chỉ chạy trên một stack UAT disposable có database/volume riêng, không dùng flowzy-postgres hiện tại.** Xác nhận URL/port/database/tên volume trước khi POST. Giữ dump ở ngoài stack và một account admin trong dump có mật khẩu bạn biết.
6. Tại stack disposable: backup trạng thái A, tạo user UAT đánh dấu trạng thái B, POST `/api/admin/backups/restore` multipart `file=<dump>` và `confirmation=RESTORE_DATABASE`. Kỳ vọng 200 với filename/fileSizeBytes/restoredAt, trạng thái A trở lại, marker B không còn; login lại vì dữ liệu xác thực có thể đổi. Kiểm API không 500 và DB nhất quán.
7. Trên stack test: confirmation sai, file rỗng, .txt, dump hỏng →400; khóa ngăn job chạy chồng phải được giải phóng sau lỗi. Không upload dump thật vào API đang chạy chỉ để thử confirmation.
8. Nếu chưa có stack disposable, đánh dấu **BLOCKED: chưa có môi trường restore an toàn**, không bỏ qua hoặc PASS.

## F18. Kiểm tra chéo bảo mật, transaction, alias và frontend

Áp dụng các biến thể sau cho từng API phù hợp, cùng checklist 192 routes:

- Happy path với role/resource đúng; ghi HTTP status, code, message, data và file headers.
- Không token/token hết hạn/blacklisted →401; role sai →403; đúng role nhưng resource người khác phải không rò dữ liệu hoặc ghi trái phép.
- Thiếu field bắt buộc, enum lạ, ID không phải số, page âm, size 0, JSON hỏng →4xx, không 500. ID không tồn tại thường 404 nhưng cần kiểm thứ tự phân quyền trước lookup; không ép mọi trường hợp thành 404.
- Gọi readback sau mỗi write, reload FE; kiểm thay đổi DB khi cần. POST nhận 200/202 không đủ để PASS cả luồng.
- Timestamp cùng một instant khi đổi timezone; UTC/local không làm deadline/late/evidence lệch 7 giờ.
- Hai request cùng version, slot, membership hoặc claim: không ghi đè âm thầm, không vượt quota, không tạo bản ghi con mồ côi. Ghi cả response và trạng thái cuối.
- Alias PUT/PATCH milestone, boards/task-boards, leave DELETE/POST, join request có/không groupId đều cần chạy; tạo fixture mới cho thao tác kết thúc trạng thái.
- Download file kiểm MIME/Content-Disposition/tên file/BOM/sheet/cell type, không so binary ZIP từng byte.
- Đăng nhập nhiều role, refresh trang, chuyển route, logout, token hết hạn: FE phải xử lý đúng envelope/message và không hiện dữ liệu phiên trước.
- Trên Network, mọi HTTP nghiệp vụ phải đến `localhost:8080`, WS đến `/ws`; không vô tình gọi `api-fspark.kusl.io.vn`.

## 3. Những điểm chưa được phép đánh dấu hoàn thành

| Điểm | Cách xử lý khi test |
|---|---|
| Realtime notification | Kiểm MESSAGE, không lấy REST/polling làm bằng chứng |
| Legacy submission/grade không có API tạo | Cần fixture DB riêng; ghi BLOCKED nếu thiếu |
| Google login | Người dùng tự chọn tài khoản Google và consent; không giả lập ID token |
| Restore | Chỉ disposable stack đã xác nhận; không thử trên dữ liệu đã import |
| Account ID vs profile ID | Ghi biến riêng; ảnh HTTP phải chứng minh ID đúng |
| Catalog labels, enum map keys, dashboard counts | Đối chiếu JSON với nghiệp vụ Java; UI có dữ liệu không có nghĩa clone đúng |
| Tài liệu outcomes lỗi cũ | outcomes 410, bulk-grade 404; source hoạt động ưu tiên |
| Source review chưa hoàn tất nghiệm thu | 37 controller trong checklist là phạm vi, không phải 37 controller PASS |

## 4. Kiểm tra khởi động đã thực hiện

Ghi nhận lúc 00:00 ngày 2026-09-28, giờ Việt Nam; đây chỉ là smoke test, không thay checklist nghiệp vụ:

| Kiểm tra thực tế | Kết quả |
|---|---|
| FE `/login` | HTTP 200 |
| BE `/actuator/health` | UP |
| Swagger, OpenAPI | HTTP 200; title Flowzy API |
| Admin login và `/api/auth/me` | Thành công, account ACTIVE |
| `/api/admin/users?page=0&size=1` | HTTP/code 200; totalElements 118 |
| Admin terms, admin groups | HTTP/code 200; mỗi danh sách totalElements 0 |
| Available terms, recruitment roles, criteria | HTTP/code 200 |
| Import batch 1 và 2 | HTTP/code 200; COMPLETED |
| CORS từ localhost:3000 | Preflight 204, cho phép origin |
| Admin users không token | HTTP 401 |

Không thay dữ liệu nghiệp vụ để thực hiện smoke test này; login có cập nhật lastLoginAt và rotation refresh token theo hành vi auth. Các bài test tạo nhóm/chấm điểm/Google/restore chưa được chạy ở lượt chuẩn bị này.

## 5. Mẫu biên bản cho mỗi lỗi

```text
Case / API:
Thời điểm:
Vai trò (không ghi token):
Fixture / ID:
Các bước tái hiện:
Request đã ẩn secret:
HTTP status thực tế:
Response thực tế:
Kết quả mong đợi theo checklist:
Trạng thái dữ liệu trước / sau:
PASS / FAIL / BLOCKED / NOT RUN:
Ảnh hoặc log đã ẩn thông tin nhạy cảm:
```

Không sửa kết quả mong đợi thành hành vi Flowzy đang sai chỉ để đánh dấu PASS. Nếu nguồn Java có hành vi cần cải tiến, ghi thành quyết định nghiệp vụ/bảo mật riêng.
