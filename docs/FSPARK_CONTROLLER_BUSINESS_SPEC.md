# Đặc tả nghiệp vụ controller F-Spark — chuẩn đối chiếu Flowzy

Ngày kiểm tra: 2026-09-25. Nguồn chuẩn: `D:/f-spark/f-spark-api/src/main/java/com/fspark/api`.

## Phạm vi và cách đọc

Tài liệu bao phủ **37 controller Java và 192 cặp HTTP method–route đang hoạt động**, tính riêng các alias. Mỗi mục ghi nghiệp vụ theo controller → service → repository/guard liên quan; không lấy tên endpoint hoặc Swagger làm bằng chứng về nghiệp vụ. Đây là baseline đọc source, không phải chứng nhận đã kiểm thử mọi nhánh.

- [Phụ lục HTTP và DTO](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md): chữ ký chính xác, path/query/body/multipart, default, response status/message/header và DTO được controller tham chiếu.
- [Kết quả kiểm tra Flowzy](D:/Flowzy-api/docs/FLOWZY_CONTROLLER_PARITY_AUDIT.md): đối chiếu đủ 37 controller, sai lệch có bằng chứng, phần chưa được chứng minh và checklist kiểm thử.
- [Dấu vân tay source](D:/Flowzy-api/docs/controller-audit-source-manifest.json): SHA-256 của Java production và C# source dùng tại thời điểm audit. Không chứa cấu hình secret.
- [Rà soát tài liệu lần 2](D:/Flowzy-api/docs/CONTROLLER_DOCUMENTATION_REVIEW_2.md): bảng kiểm 37 controller và các nhận định đã sửa/rút lại sau khi đọc lại DTO, service, guard và query.

Code đang chạy là chuẩn ưu tiên khi comment/annotation diễn tả khác. Ví dụ archive students không chờ feedback; POST/PATCH legacy submission/grade đã nằm trong block comment nên không được tính thành endpoint còn thiếu. Route outcomes/bulk cố ý trả 404 vẫn là route cần giữ. Tài liệu mô tả hành vi hiện hữu, không tự khẳng định tất cả hành vi đó là thiết kế tối ưu hoặc an toàn nhất.

## Quy tắc xuyên suốt

### Xác thực, phân quyền và khóa ghi

Quyền được xác nhận từ URL security và kiểm tra role/account/profile/ownership trong service. Production source đang đối chiếu không có `@EnableMethodSecurity`; không mặc định `@PreAuthorize` tự được thực thi chỉ vì annotation tồn tại. Các prefix controller có annotation hiện có URL matcher tương ứng; vẫn cần kiểm từng HTTP method và service. “Authenticated” không đồng nghĩa được truy cập mọi tài nguyên. Các trạng thái profile/account được kiểm tra theo từng service; không suy rộng một rule cho toàn bộ hệ thống.

[SecurityConfig](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/configs/SecurityConfig.java) cho phép public login/google/refresh, tài liệu API, health và handshake /ws; prefix admin/import dành ADMIN, instructor dành INSTRUCTOR; các route mentor/student có matcher riêng. TV showcase không nằm trong public allowlist. JWT dùng secret Base64, HMAC, sub/role/iat/exp; giữ BCrypt, rotation refresh và blacklist access token khi logout.

[PasswordChangeRequiredFilter](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/configs/PasswordChangeRequiredFilter.java) chặn phần lớn API khi phải đổi mật khẩu; giữ đúng các ngoại lệ auth/me, auth/logout, profile/me/password. [StudentTermWriteGuard](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/StudentTermWriteGuard.java) và [GroupMembershipLockGuard](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupMembershipLockGuard.java) phải áp dụng đúng từng nhánh: khóa kỳ không đồng nghĩa isLock của nhóm, ADMIN có các nhánh bypass được nêu riêng.

### HTTP, JSON và lỗi

[APIResponse](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/APIResponse.java) quy định envelope code/message/data; status HTTP và code trong envelope là hai giá trị cần so sánh độc lập. Tải file không bọc envelope. JSON field camelCase, enum giữ tên viết hoa, timestamp ISO-8601; không đổi enum key trong map thành camelCase. [PageResponse](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/PageResponse.java) quy định các trường pagination; page/size và giới hạn khác nhau theo API, không áp một cap chung.

[GlobalExceptionHandler](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/exceptions/GlobalExceptionHandler.java) cùng authentication entry point là chuẩn cho mã lỗi và message. DTO @Valid, @NotBlank, @Size, @Min/@Max, enum conversion và service guards cùng tạo contract. Phụ lục lưu nguyên chữ ký và DTO để tránh bỏ sót default hoặc message validation; source service liên kết ở từng mục là nơi tra message cụ thể của các nhánh lỗi nghiệp vụ.

### Giao dịch và tác động phụ

Một thao tác nghiệp vụ có thể cập nhật nhiều bảng và phát notification. Phải giữ ranh giới transaction, thứ tự row lock, optimistic version, cleanup membership/assignment và thời điểm gửi sau commit; trả cùng JSON nhưng không có các side effect này vẫn là clone sai. Các luồng cần kiểm tra cạnh tranh riêng: đóng kỳ, tham gia nhóm, book slot, task ordering/default board và contribution/grade.

Notification lưu DB và STOMP là hai phần độc lập. Gửi thành công một bản ghi không chứng minh frontend đã nhận MESSAGE; phải kiểm recipient, eventKey, action params và quyền subscribe. Import chạy background, có partial success theo dòng; backup/restore là tác động DB thật và chỉ kiểm thử trên DB disposable.

## Mục lục controller

| # | Controller | HTTP operations, gồm alias |
|---|---|---:|
| 1 | [AcademicTermController](#c01) | 1 |
| 2 | [AdminFeedbackController](#c02) | 2 |
| 3 | [AdminGroupController](#c03) | 1 |
| 4 | [AdminProblemController](#c04) | 6 |
| 5 | [AdminTermController](#c05) | 5 |
| 6 | [AdminUserController](#c06) | 7 |
| 7 | [AuthController](#c07) | 5 |
| 8 | [BackupController](#c08) | 7 |
| 9 | [CourseMilestoneController](#c09) | 16 |
| 10 | [DashboardController](#c10) | 15 |
| 11 | [FeedbackController](#c11) | 3 |
| 12 | [GroupController](#c12) | 19 |
| 13 | [GroupInvitationController](#c13) | 6 |
| 14 | [GroupJoinRequestController](#c14) | 9 |
| 15 | [GroupMeetingController](#c15) | 8 |
| 16 | [GroupProblemController](#c16) | 6 |
| 17 | [GroupRecruitmentRoleController](#c17) | 1 |
| 18 | [GroupTaskController](#c18) | 19 |
| 19 | [ImportController](#c19) | 7 |
| 20 | [InstructorGroupBoardController](#c20) | 2 |
| 21 | [InstructorProblemController](#c21) | 2 |
| 22 | [InstructorSubmissionController](#c22) | 1 |
| 23 | [MentorAvailabilityController](#c23) | 5 |
| 24 | [MentorMeetingReportController](#c24) | 2 |
| 25 | [MilestoneGradeController](#c25) | 3 |
| 26 | [MilestoneGradeMatrixController](#c26) | 6 |
| 27 | [MilestoneSubmissionController](#c27) | 3 |
| 28 | [NotificationController](#c28) | 4 |
| 29 | [ProblemController](#c29) | 2 |
| 30 | [ProblemCriteriaController](#c30) | 1 |
| 31 | [ProblemDomainController](#c31) | 1 |
| 32 | [ProblemImportController](#c32) | 1 |
| 33 | [ProfileController](#c33) | 3 |
| 34 | [StudentAccountImportController](#c34) | 2 |
| 35 | [StudentController](#c35) | 2 |
| 36 | [StudentGroupGradeController](#c36) | 1 |
| 37 | [TaskBoardController](#c37) | 8 |

<a id="c01"></a>

## 1. AcademicTermController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AcademicTermController.java). Luồng xử lý: [AcademicTermServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/AcademicTermServiceImpl.java).

Mọi tài khoản đã xác thực được đọc. Chỉ trả kỳ OPEN, sắp createdAt giảm dần rồi id giảm dần; mỗi kỳ kèm groupCount theo mã kỳ (không phân biệt hoa/thường), tổng feedback dự kiến và đã SUBMITTED.

### 1.1. listAvailableTerms

- `GET /api/terms/available`

Đọc danh sách kỳ đang mở; trả mảng AcademicTermResponseDto, không phân trang; không tự tạo kỳ.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c01-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AcademicTermController.java:26).

<a id="c02"></a>

## 2. AdminFeedbackController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminFeedbackController.java). Luồng xử lý: [FeedbackServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/FeedbackServiceImpl.java).

Chỉ ADMIN. Danh sách có danh tính sinh viên và mentor/instructor; khác với feedback received ẩn danh. Export cần mã kỳ tồn tại, trim/uppercase; thiếu kỳ → 400 "Academic term is required", không có kỳ → 404.

### 2.1. listFeedbacks

- `GET /api/admin/feedback`

Lọc đồng thời term, courseCode, targetType, targetId, targetSearch, status; page mặc định 0, size 20 (1–100); createdAt DESC, id DESC. Không chỉ lấy SUBMITTED khi status không truyền.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c02-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminFeedbackController.java:34).

### 2.2. exportFeedbacks

- `GET /api/admin/feedback/export.xlsx`

Xuất chỉ feedback SUBMITTED của kỳ, hai sheet Mentor Feedback/Instructor Feedback. 15 cột gồm student/target/email/rating/comment/thời điểm ICT. Sắp mã người nhận, môn, số nhóm, MSSV, submittedAt, id. Filename feedback-{safeTerm}.xlsx; không bọc JSON.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c02-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminFeedbackController.java:65).

<a id="c03"></a>

## 3. AdminGroupController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminGroupController.java). Luồng xử lý: [GroupServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupServiceImpl.java).

Chỉ ADMIN; status mặc định ACTIVE, ALL bỏ lọc; INACTIVE vẫn có thể truy vấn. Enum không hợp lệ → 400 "Status must be ACTIVE, INACTIVE, or ALL". Phân trang trên id trước khi fetch các collection để không nhân số nhóm.

### 3.1. listGroups

- `GET /api/admin/groups`

Lọc search theo tên nhóm/project/course/term; page=0,size=20, giới hạn 1–100. createdAt DESC,id DESC. Trả PageResponse<GroupSummaryDto> gồm kỳ/readOnly, leader, số thành viên, tài khoản và profile mentor/instructor, selectedProblem, recruitmentNeeds đầy đủ nhãn.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c03-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminGroupController.java:34).

<a id="c04"></a>

## 4. AdminProblemController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java). Luồng xử lý: [ProblemBankServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemBankServiceImpl.java).

Chỉ ADMIN. Domain có thể không truyền khi tạo problem; nếu truyền phải tồn tại và ACTIVE. Code problem là unique theo repository, không tự gán code. DTO enum/validation phải được áp dụng trước service.

### 4.1. createProblemDomain

- `POST /api/admin/problem-domains`

Tạo domain: code uppercase, từ chối trùng ignore-case (409); name/description và metadata lĩnh vực; macroDomain ưu tiên giá trị truyền, fallback name; subDomain fallback description; metadata blank → null; mặc định ACTIVE.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c04-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:25).

### 4.2. updateProblemDomain

- `PATCH /api/admin/problem-domains/{id}`

Sửa từng field non-null; code trùng 409; không tự xóa field khi không truyền. Tra id không có → 404.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c04-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:35).

### 4.3. createOfficialProblem

- `POST /api/admin/problems`

Tạo OFFICIAL; domain optional ACTIVE, title/statement/difficulty từ DTO; code blank → null; status mặc định ACTIVE. Code trùng → 400.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c04-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:46).

### 4.4. updateProblem

- `PATCH /api/admin/problems/{id}`

PATCH trường non-null; domainCode blank xóa domain; domain hiện tại không đổi thì không kiểm tra lại ACTIVE; code đổi phải không trùng.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c04-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:56).

### 4.5. updateProblemStatus

- `PATCH /api/admin/problems/{id}/status`

Cập nhật status theo ProblemStatus hợp lệ; đây là thao tác riêng, không tự thực hiện quy trình duyệt proposal.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c04-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:67).

### 4.6. reviewProblem

- `PATCH /api/admin/problems/{id}/review`

Chỉ SELF_PROPOSED/PENDING_REVIEW; quyết định APPROVED hoặc REJECTED; REJECTED cần comment không trắng. APPROVED chuyển sourceType=OFFICIAL,status=ACTIVE. REJECTED bỏ selectedProblem nếu nhóm đang chọn proposal đó. Lưu reviewComment, reviewedByAccount, reviewedAt.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c04-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:78).

<a id="c05"></a>

## 5. AdminTermController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminTermController.java). Luồng xử lý: [AcademicTermServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/AcademicTermServiceImpl.java).

Chỉ ADMIN. Chuẩn hóa term trim/uppercase, bắt buộc, tối đa 30 ký tự. Các thao tác ghi có transaction; close khóa nhóm theo id ổn định rồi khóa kỳ để phối hợp các student write.

### 5.1. listTerms

- `GET /api/admin/terms`

Liệt kê tất cả kỳ với count nhóm/feedback, page=0,size=10 (1–100), createdAt DESC,id DESC.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c05-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminTermController.java:35).

### 5.2. createTerm

- `POST /api/admin/terms`

Tạo kỳ OPEN. Nếu cùng code đã OPEN thì trả lại kỳ hiện hữu (idempotent), nếu CLOSED trả409. Nếu kỳ khác đang OPEN, buộc đóng kỳ đó trước. Đua unique/open constraint được chuyển thành409.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c05-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminTermController.java:56).

### 5.3. closeTerm

- `PATCH /api/admin/terms/{term}/close`

Nếu chưa có AcademicTerm nhưng có group lịch sử cùng mã, đăng ký kỳ rồi đóng. Nếu đã CLOSED trả lại thành công. Khóa nhóm/kỳ; lưu người đóng, thời gian; CANCELED toàn bộ pending invitation/join request trong kỳ; snapshot feedback từng thành viên × mentor/instructor được gán (không trùng); gửi TERM_FEEDBACK_AVAILABLE chỉ khi có feedback mới.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c05-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminTermController.java:65).

### 5.4. archiveStudents

- `POST /api/admin/terms/{term}/archive-students`

Kỳ phải CLOSED; khóa kỳ và nhóm, chuyển mọi nhóm trong kỳ INACTIVE. Sinh viên còn thuộc kỳ OPEN khác được giữ/đưa lại ACTIVE cùng account. Còn lại chuyển student+account INACTIVE, revoke refresh tokens; bỏ qua khi cả hai đã inactive hoặc thiếu account. Code thực tế KHÔNG chờ feedback; skippedPendingFeedbackStudents luôn0. Đây là điểm khác với mô tả annotation cũ.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c05-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminTermController.java:77).

### 5.5. deleteEmptyTerm

- `DELETE /api/admin/terms/{term}`

Chỉ xóa kỳ không có group và không có feedback; có lịch sử →409. Khóa kỳ, xóa+flush; không xóa dây chuyền dữ liệu học tập.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c05-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminTermController.java:88).

<a id="c06"></a>

## 6. AdminUserController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java). Luồng xử lý: [AdminUserServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/AdminUserServiceImpl.java).

Chỉ ADMIN theo SecurityConfig. Create/update chuẩn hóa email trim/lowercase, trùng ignore-case →400; change-password theo email có ngoại lệ mô tả riêng bên dưới. Account gắn đúng một profile theo role; ADMIN không được kèm profile; STUDENT/MENTOR/INSTRUCTOR phải có đúng loại và từ chối profile khác loại. Không đổi role qua update.

### 6.1. listUsers

- `GET /api/admin/users`

Page0,size10 (1–100), search email/tên/mã profile, role/status; createdAt DESC,id DESC; sinh viên có groupMemberships.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c06-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:38).

### 6.2. getUserById

- `GET /api/admin/users/{id}`

Đọc account+profile+membership bằng account id; thiếu →404.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c06-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:61).

### 6.3. createUser

- `POST /api/admin/users`

Tạo ACTIVE, BCrypt(initialPassword), mustChangePassword=true; mã student/mentor/instructor phải duy nhất; account/profile trong cùng transaction.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c06-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:68).

### 6.4. updateUser

- `PATCH /api/admin/users/{id}`

Cập nhật email,status,mustChangePassword và profile đủ theo role hiện tại; đồng bộ status profile; không cho người gọi vô hiệu hóa chính mình. Khi chuyển sang INACTIVE revoke refresh tokens.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c06-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:77).

### 6.5. deleteUser

- `DELETE /api/admin/users/{id}`

Soft-delete account và profile thành INACTIVE, revoke refresh tokens; không xóa lịch sử; cấm tự disable/delete.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c06-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:88).

### 6.6. resetPassword

- `POST /api/admin/users/{id}/reset-password`

Theo account id: BCrypt mật khẩu mới, mustChangePassword=true, revoke tất cả refresh tokens.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c06-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:98).

### 6.7. changePasswordByEmail

- `POST /api/admin/users/change-password`

Tra `findByEmailIgnoreCase(request.email())` trực tiếp, không trim/lowercase qua helper của create/update; DTO vẫn validate email. BCrypt mật khẩu mới, mustChangePassword=false, revoke tất cả refresh tokens.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c06-m7); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:108).

<a id="c07"></a>

## 7. AuthController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java). Luồng xử lý: [CustomUserDetailsService](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/CustomUserDetailsService.java), [GoogleAuthServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GoogleAuthServiceImpl.java), [GoogleTokenVerifierImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GoogleTokenVerifierImpl.java), [JwtServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/JwtServiceImpl.java), [RefreshTokenServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/RefreshTokenServiceImpl.java), [TokenBlacklistServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/TokenBlacklistServiceImpl.java).

Login/google/refresh public; me/logout cần JWT. Login thường dùng authentication manager/BCrypt; mọi exception khi authenticate được trả401 "Invalid email or password". TokenResponse gồm accessToken,refreshToken,tokenType=Bearer,expiresIn (giây). JWT secret Base64; sub=email,role,iat,exp. Refresh raw UUID, chỉ lưu SHA-256/Base64; tạo token mới xóa token cũ cùng account.

### 7.1. login

- `POST /api/auth/login`

Email/password hợp lệ, account được phép authenticate; cập nhật lastLoginAt rồi phát cặp token. Không trả profile trong TokenResponse.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c07-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java:58).

### 7.2. loginWithGoogle

- `POST /api/auth/google`

Gửi ID token tới Google token-info endpoint; verifier Java kiểm client-id đã cấu hình, audience khớp, email_verified=true và email không rỗng, sau đó trim/lowercase email. Java không có nhánh tự kiểm issuer/exp riêng trong verifier; không suy diễn thêm từ tên “verify”. Chỉ account ACTIVE đã đăng ký, không tự đăng ký tài khoản; cập nhật lastLoginAt, phát token.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c07-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java:90).

### 7.3. refresh

- `POST /api/auth/refresh`

Tra hash refresh, kiểm tồn tại/hết hạn/revoked; tạo JWT mới và thay refresh token toàn account. Controller Java hiện không gọi lại kiểm ACTIVE trong nhánh này; cần phân biệt hành vi thực tế với cải tiến bảo mật mong muốn.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c07-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java:97).

### 7.4. me

- `GET /api/auth/me`

Tra email principal; trả id,email,role,status,mustChangePassword và instructorProfile chỉ khi role INSTRUCTOR và có profile.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c07-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java:117).

### 7.5. logout

- `POST /api/auth/logout`

Revoke tất cả refresh của account nếu tìm được; blacklist bearer token đến hạn; clear SecurityContext; trả200 "Logged out successfully", data=null.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c07-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java:152).

<a id="c08"></a>

## 8. BackupController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java). Luồng xử lý: [BackupServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/BackupServiceImpl.java).

Chỉ ADMIN. Backup/restore là thao tác thật trên PostgreSQL; không chạy thử restore vào DB người dùng. Job QUEUED→RUNNING→SUCCEEDED/FAILED; ngăn job/restore chồng nhau trong instance Java. Startup đánh dấu job cũ đang chạy/đợi FAILED. Scheduler fixedDelay 60 giây, cron Spring 6 trường và timezone.

### 8.1. createBackup

- `POST /api/admin/backups`

Tạo MANUAL job và xếp hàng background, HTTP200 (không phải202). Chụp backupDir/retention từ lúc queue. Đang restore/job →409. pg_dump tạo custom-format dump; lưu filename/path/size, finishedAt hoặc lỗi giới hạn4000 ký tự.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c08-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:44).

### 8.2. restoreBackup

- `POST /api/admin/backups/restore`

Multipart file + confirmation=RESTORE_DATABASE; không rỗng, đuôi .dump; ngăn job/restore đang chạy. Lưu upload, chạy pg_restore, đánh dấu job active trong dump FAILED; trả filename,size,restoredAt. Lỗi xử lý →400 có tiền tố Database restore failed; luôn giải phóng gate.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c08-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:50).

### 8.3. listBackups

- `GET /api/admin/backups`

Page0,size10 (1–100), status optional enum; createdAt DESC,id DESC; trả trạng thái, thời gian, người yêu cầu, file metadata.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c08-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:60).

### 8.4. getSchedule

- `GET /api/admin/backups/schedule`

Khởi tạo settings mặc định khi chưa có; DTO dùng các field `enabled`, `cronExpression`, `timezone`, `backupDir`, `retentionDays`, `lastTriggeredAt`, `nextRunAt`, `updatedByAccountId`, `updatedByEmail`, `updatedAt`. Mặc định disabled, 02:00 Asia/Ho_Chi_Minh, giữ14ngày; GET có thể cập nhật backupDir theo cấu hình.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c08-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:80).

### 8.5. updateSchedule

- `PUT /api/admin/backups/schedule`

Validate enabled,cron,timezone,retention1–3650; retention null→14; directory do config quản lý. enabled=false→nextRun=null; bật→tính lần tiếp theo. Không tạo job ngay.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c08-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:87).

### 8.6. getBackup

- `GET /api/admin/backups/{jobId}`

Tra job theo id; thiếu404.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c08-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:96).

### 8.7. downloadBackup

- `GET /api/admin/backups/{jobId}/download`

Chỉ SUCCEEDED; metadata/file phải có, file regular tồn tại. Trả application/octet-stream, Content-Disposition attachment; filename="{job.fileName}", không JSON.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c08-m7); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:103).

<a id="c09"></a>

## 9. CourseMilestoneController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java). Luồng xử lý: [CourseMilestoneServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/CourseMilestoneServiceImpl.java).

Hai prefix /api/course-milestones và /api/instructor/milestones trỏ cùng controller, nhưng prefix instructor bị SecurityConfig giới hạn INSTRUCTOR; prefix cũ cho authenticated rồi service phân quyền. Milestone thuộc instructor + term + course; maxScore mặc định10, >0; weight0–100, tổng ACTIVE≤100; position≥0. deadlineAt nhận alias dueDate.

### 9.1. createCourseMilestone

- `POST /api/course-milestones`
- `POST /api/instructor/milestones`

INSTRUCTOR active, có group được giao trong term/course; kỳ tồn tại OPEN; title unique ignore-case trong scope; deadline bắt buộc; tự position=max+1; typeTIMELINE,statusACTIVE; gửi TIMELINE_ITEM_CREATED cho leader các nhóm đúng scope.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c09-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:29).

### 9.2. getCourseMilestones

- `GET /api/course-milestones`
- `GET /api/instructor/milestones`

Instructor xem timeline của mình với filter tùy chọn, kể cả archived. Student không filter xem hợp nhất timeline nhóm mình; nếu có filter phải đủ cả term/course, phải thuộc group scope đó; list không chứa ARCHIVED/INACTIVE. Role khác403.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c09-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:41).

### 9.3. getCourseMilestoneById

- `GET /api/course-milestones/{id}`
- `GET /api/instructor/milestones/{id}`

Instructor phải sở hữu milestone; student thuộc group cùng scope/instructor, không ARCHIVED. Lưu ý kiểm detail Java chỉ loại ARCHIVED, khác list loại cả INACTIVE.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c09-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:54).

### 9.4. updateCourseMilestone

- `PUT /api/course-milestones/{id}`
- `PATCH /api/course-milestones/{id}`
- `PUT /api/instructor/milestones/{id}`
- `PATCH /api/instructor/milestones/{id}`

PUT/PATCH cùng nghiệp vụ: cần title/deadline, kiểm trùng và maxScore không thấp hơn điểm đã có; tổng weightACTIVE≤100; đổi deadline cập nhật late của submissions; chỉ notify TIMELINE_ITEM_UPDATED nếu trạng thái mới ACTIVE. Description và weight được gán kể cả null; position/status/maxScore null giữ cũ. Update/delete không gọi requireOpenAcademicTerm như create; không áp thêm chặn kỳ CLOSED nếu đang mô tả bản Java.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c09-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:66).

### 9.5. deleteCourseMilestone

- `DELETE /api/course-milestones/{id}`
- `DELETE /api/instructor/milestones/{id}`

Chỉ instructor sở hữu; archive mềm, không xóa grade/submission.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c09-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:79).

### 9.6. deprecatedOutcomes

- `GET /api/course-milestones/{milestoneId}/outcomes`
- `POST /api/course-milestones/{milestoneId}/outcomes`
- `GET /api/instructor/milestones/{milestoneId}/outcomes`
- `POST /api/instructor/milestones/{milestoneId}/outcomes`

GET/POST outcomes là route chủ động deprecated: trả404 với envelope của Java, không chạy MilestoneOutcomeService. Không được dựng lại thành success.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c09-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:91).

<a id="c10"></a>

## 10. DashboardController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java). Luồng xử lý: [DashboardServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/DashboardServiceImpl.java).

Phân quyền theo prefix admin/mentor/instructor/student; TV showcase vẫn cần đăng nhập theo anyRequest. Đây là dữ liệu tổng hợp thật, không dùng số mẫu. Tiến độ=round(DONE/activeTasks*100), zero task→0; activeTask loại archived; overdue là dueAt<now và chưaDONE. Phân biệt stats loại archived và taskStatusCounts Java đếm nguồn allTasks. nextDueAt của group progress có thể nằm trong quá khứ; TV projects chỉ lấy deadline >= now. Completeness của milestone dashboard dùng tất cả membership có student, không lọc student/account ACTIVE mặc dù biến có tên activeMemberIds; đây không phải cùng cách lọc với grade matrix.

### 10.1. getAdminGroups

- `GET /api/dashboard/admin/groups`

ADMIN: tiến độ từng nhóm, mentor/member counts, tasks done/in-progress/overdue,nextDueAt,updatedAt.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:38).

### 10.2. getAdminProjects

- `GET /api/dashboard/admin/projects`

ADMIN: project metadata và tiến độ từng nhóm; không nhất thiết chỉ nhóm có projectName.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:44).

### 10.3. getAdminMentors

- `GET /api/dashboard/admin/mentors`

ADMIN: danh sách mentor, số nhóm/cuộc họp theo mapMentor; không rút gọn mất fields DTO.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:50).

### 10.4. getAdminTimeline

- `GET /api/dashboard/admin/timeline`

ADMIN: timeline meeting startAt ASC, dữ liệu từ meeting và fallback slot nếu trường thời gian chưa có.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:56).

### 10.5. getAdminExecutionStatus

- `GET /api/dashboard/admin/execution-status`

ADMIN: stats toàn hệ thống + groupStatusCounts + taskStatusCounts theo enum + tiến độ từng nhóm. Giữ đúng cách đếm task archived của từng trường.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:62).

### 10.6. getAdminOverview

- `GET /api/dashboard/admin/overview`

ADMIN: active student/mentor totals, top problem/domain đã được nhóm chọn; term/course optional, limit1–50. Tie-break theo repository.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:68).

### 10.7. getTvShowcaseProjects

- `GET /api/dashboard/tv-showcase/projects`

TV projects: group ACTIVE thuộc kỳ OPEN mới nhất, lọc term/course; không có kỳ trả trang rỗng. projectName ưu tiên tên nhập, rồi selectedProblem.title, rồi group.name; chỉ lấy tên cuối cùng không trắng. Sắp updatedAt DESC, id DESC trước phân trang.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m7); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:83).

### 10.8. getTvShowcaseRecruitments

- `GET /api/dashboard/tv-showcase/recruitments`

TV recruitments: cùng scope TV, chỉ nhóm có recruitmentNeeds; code Java không tự loại nhóm locked. Mỗi nhu cầu gồm role, nhãn VI/EN, quantity; có totalOpenings. Trang kèm refreshedAt và activeTermCode, sắp updatedAt DESC, id DESC.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m8); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:95).

### 10.9. getMentorGroups

- `GET /api/dashboard/mentor/groups`

MENTOR: chỉ nhóm gán mentor hiện tại, cùng công thức tiến độ.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m9); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:107).

### 10.10. getMentorMeetings

- `GET /api/dashboard/mentor/meetings`

MENTOR: chỉ meeting do mentor đó và group vẫn gán mentor đó; status enum optional/ALL; startAt ASC.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m10); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:113).

### 10.11. getInstructorMilestones

- `GET /api/dashboard/instructor/milestones`

INSTRUCTOR: milestone status đúng scope instructor/term/course/group; groupId không thuộc scope→404; bao gồm submitted/late/graded/contribution completeness.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m11); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:121).

### 10.12. getStudentGroups

- `GET /api/dashboard/student/groups`

STUDENT: chỉ các nhóm người gọi là thành viên; tiến độ theo task nhóm.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m12); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:133).

### 10.13. getStudentProgress

- `GET /api/dashboard/student/progress`

STUDENT: stats các nhóm của mình, mentor distinct, checkpoints task có dueAt và không archived; kèm overdue/status/priority.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m13); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:139).

### 10.14. getStudentProjects

- `GET /api/dashboard/student/projects`

STUDENT: projects của các nhóm mình; dùng chung map project/tiến độ.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m14); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:145).

### 10.15. getStudentMilestones

- `GET /api/dashboard/student/milestones`

STUDENT: groupId bắt buộc, phải membership; rows timeline đúng instructor/term/course; không instructor→mảng rỗng.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m15); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:151).

<a id="c11"></a>

## 11. FeedbackController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/FeedbackController.java). Luồng xử lý: [FeedbackServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/FeedbackServiceImpl.java).

GET me/PUT chỉ STUDENT; GET received chỉ MENTOR/INSTRUCTOR. Bản ghi feedback được snapshot khi đóng kỳ; không tự tạo khi sinh viên submit. DTO rating bắt buộc1–5, giới hạn comment theo DTO; entity @Version cho concurrency.

### 11.1. getOwnFeedbacks

- `GET /api/feedback/me`

Lấy feedback của student hiện tại, filter term (chuẩn hóa) và status; không lấy của người khác.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c11-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/FeedbackController.java:28).

### 11.2. submitOrUpdateFeedback

- `PUT /api/feedback/{id}`

Tra record→kiểm sở hữu→kỳ không OPEN; cập nhật rating,comment trim/blank→null,statusSUBMITTED; submittedAt chỉ đặt lần đầu; optimistic version do ORM. Cho sửa lại feedback đã gửi.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c11-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/FeedbackController.java:42).

### 11.3. getReceivedFeedbacks

- `GET /api/feedback/received`

Chỉ feedback SUBMITTED gửi đến profile người gọi, filter kỳ/môn; average làm tròn2 số theo Math.round, distribution đủ1–5 kể cả0; entries không chứa tên/MSSV/email sinh viên.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c11-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/FeedbackController.java:54).

<a id="c12"></a>

## 12. GroupController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java). Luồng xử lý: [GroupServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupServiceImpl.java), [CourseMilestoneServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/CourseMilestoneServiceImpl.java), [StudentTermWriteGuard](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/StudentTermWriteGuard.java), [GroupMembershipLockGuard](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupMembershipLockGuard.java).

Quyền theo từng route và service, không phải một quyền chung cho controller. Create/discover/student-me/lock: STUDENT; mentor-me: MENTOR; instructor-me: INSTRUCTOR; gán/gỡ người phụ trách: ADMIN. Sửa thông tin/criteria/member/leader cho leader hoặc ADMIN. Student ghi cần kỳ chưa đóng; ADMIN có nhánh bypass. Khóa thành viên (isLock) và kỳ CLOSED là hai điều kiện khác nhau.

### 12.1. getGroups

- `GET /api/groups`

Lọc search/status/neededRole/roleCategory; giá trị enum sai trả 400. Recruitment filter bỏ nhu cầu nhóm locked. Sau filter chỉ giữ nhóm không có academicTerm liên kết hoặc academicTerm OPEN; loại nhóm kỳ CLOSED. Sắp createdAt DESC, id DESC.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:34).

### 12.2. discoverGroups

- `GET /api/groups/discover`

Student tìm group ACTIVE trong kỳ OPEN, loại nhóm mình đã tham gia; lọc neededRole chỉ nhận nhóm unlocked. GPA 0–4 và requiredGpa≤GPA hoặc null. Page 0, size 12.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:46).

### 12.3. getGroupDetail

- `GET /api/groups/{id}`

Đọc group detail cùng leader/members/mentor/instructor, selectedProblem, recruitmentNeeds có category/nhãn VI/EN, trạng thái kỳ và studentReadOnly; thiếu group trả 404.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:78).

### 12.4. updateGroupCriteria

- `PATCH /api/groups/{id}/criteria`

Khóa group, leader/admin; ít nhất một field GPA/targetGrade/recruitmentNeeds. GPA 0–4, target 0–10; role đúng enum, không trùng, quantity 1–6 mỗi role và tổng≤6. Thay collection khi truyền recruitmentNeeds.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:87).

### 12.5. getMyAssignedGroups

- `GET /api/groups/mentor/me`

Các nhóm được gán mentor người gọi, mới nhất trước.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:99).

### 12.6. getMyStudentGroup

- `GET /api/groups/student/me`

Các nhóm có membership của student người gọi, mới nhất trước.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:107).

### 12.7. createGroup

- `POST /api/groups`

Kỳ OPEN tồn tại; chuẩn hóa term/course/name; tên unique trong kỳ; sinh viên chưa có nhóm cùng kỳ/môn. groupNo là số nguyên dương nhỏ nhất chưa dùng, parse "01" như 1. Tạo group, board Default và membership LEADER trong một transaction.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m7); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:115).

### 12.8. updateGroup

- `PATCH /api/groups/{id}`

Leader/admin, khóa group; sửa field non-null; kiểm tên trùng và GPA/target/recruitment như criteria. Không tự xóa field không truyền.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m8); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:126).

### 12.9. removeMember

- `DELETE /api/groups/{groupId}/members/{studentId}`

Group unlocked; leader/admin không được xóa leader. Xóa assignment task active và ghi activity system; xóa membership, nhóm rỗng→INACTIVE/leader=null/unlocked; notify người bị xóa.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m9); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:138).

### 12.10. leaveGroup

- `DELETE /api/groups/{groupId}/members/me`

Student thành viên rời nhóm unlocked, kỳ writable. Leader chỉ rời được khi là thành viên cuối cùng; còn người khác trả 400. Dọn task assignments, deactivate nhóm rỗng; notify leader khi thành viên thường rời.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m10); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:149).

### 12.11. leaveGroupPost

- `POST /api/groups/{groupId}/leave`

Alias POST cho leave, cùng quyền và transaction/side effects như DELETE members/me.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m11); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:159).

### 12.12. transferLeader

- `PATCH /api/groups/{groupId}/leader`

Leader/admin; người nhận phải là thành viên; nhánh student cần kỳ writable. Khóa hàng group trong giao dịch nhưng KHÔNG kiểm cờ isLock bằng requireUnlocked. Chuyển lại chính leader hiện tại là no-op. Đổi memberRole cũ/mới, leader của group; notify cả leader cũ/mới.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m12); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:169).

### 12.13. assignInstructor

- `PATCH /api/groups/{groupId}/instructor`

ADMIN truyền instructorId là ACCOUNT ID. Group ACTIVE có thành viên; account phải INSTRUCTOR và có profile; khóa group rồi gán profile.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m13); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:180).

### 12.14. unassignInstructor

- `DELETE /api/groups/{groupId}/instructor`

ADMIN gỡ instructor dưới group lock; không xóa milestone/grade lịch sử.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m14); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:193).

### 12.15. assignMentor

- `PATCH /api/groups/{groupId}/mentor`

ADMIN truyền mentorId là ACCOUNT ID, account MENTOR có profile; group ACTIVE có thành viên; khóa group và gán.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m15); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:205).

### 12.16. unassignMentor

- `DELETE /api/groups/{groupId}/mentor`

ADMIN gỡ mentor; không xóa meeting lịch sử.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m16); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:218).

### 12.17. getMyAssignedGroups

- `GET /api/groups/instructor/me`

INSTRUCTOR: nhóm gán cho mình, filter term/course optional trim, ignore-case; createdAt DESC, id DESC.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m17); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:230).

### 12.18. getGroupMilestones

- `GET /api/groups/{groupId}/milestones`

Chỉ INSTRUCTOR được gán nhóm hoặc STUDENT có membership được xem timeline; ADMIN/MENTOR không được service này cho phép. Lấy milestone thuộc instructor hiện tại, cùng term/course, bỏ ARCHIVED/INACTIVE; chưa gán instructor trả mảng rỗng.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m18); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:245).

### 12.19. updateGroupLock

- `PATCH /api/groups/{groupId}/lock`

Student leader, group ACTIVE, kỳ writable; đổi isLock; notify các thành viên khác với GROUP_LOCK_UPDATED và params groupId/locked.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m19); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:259).

<a id="c13"></a>

## 13. GroupInvitationController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java). Luồng xử lý: [GroupInvitationServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupInvitationServiceImpl.java).

Trạng thái PENDING/ACCEPTED/DECLINED/CANCELED; hết hạn sau 72 giờ. Tạo/xem nhóm/hủy dành leader hoặc ADMIN; nhận/decline chỉ invitee. Có kiểm khóa thành viên và kỳ đóng theo từng nhánh student. Tối đa 6 thành viên và một nhóm trong mỗi kỳ/môn.

### 13.1. createInvitation

- `POST /api/groups/{groupId}/invitations`

Khóa group; ADMIN dùng leader của nhóm làm inviter, thiếu leader trả 400. Resolve người nhận bằng MSSV/email; chặn tự mời, thành viên hiện tại, người có nhóm cùng scope, nhóm đủ 6, invitation pending trùng; tạo PENDING và notify invitee.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c13-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:27).

### 13.2. getGroupInvitations

- `GET /api/groups/{groupId}/invitations`

Chỉ leader/admin xem invitation của group; không cho mọi thành viên.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c13-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:38).

### 13.3. getMyPendingInvitations

- `GET /api/groups/invitations/me`

Chỉ PENDING gửi đến student hiện tại và chưa hết hạn.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c13-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:48).

### 13.4. acceptInvitation

- `POST /api/groups/invitations/{invitationId}/accept`

PENDING, chưa hết hạn, đúng invitee; khóa student rồi group; kiểm ACTIVE, writable, unlocked, count<6 và chưa có nhóm cùng scope. Tạo MEMBER/set ACCEPTED; decline invitation khác và cancel join requests cùng scope; notify leader.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c13-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:55).

### 13.5. declineInvitation

- `POST /api/groups/invitations/{invitationId}/decline`

PENDING, chưa hết hạn, đúng invitee; khóa group và kiểm student write; set DECLINED/respondedAt, notify leader.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c13-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:65).

### 13.6. cancelInvitation

- `POST /api/groups/invitations/{invitationId}/cancel`

PENDING, chưa hết hạn; leader/admin. Nhánh student khóa hàng group, kiểm leader và kỳ writable nhưng không kiểm isLock; ADMIN không đi qua nhánh khóa hàng/kiểm kỳ này. Set CANCELED/respondedAt.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c13-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:75).

<a id="c14"></a>

## 14. GroupJoinRequestController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java). Luồng xử lý: [GroupJoinRequestServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupJoinRequestServiceImpl.java).

Enum chính xác là PENDING/ACCEPTED/REJECTED/CANCELED, không phải APPROVED/CANCELLED. Student tạo/hủy request mình; leader/admin approve/reject. Group listing cho ADMIN, thành viên/leader hoặc assigned mentor. Alias có/không groupId dùng cùng logic.

### 14.1. getMyJoinRequests

- `GET /api/groups/join-requests/me`

Student xem mọi request mình đã gửi, giữ cả lịch sử đã xử lý.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c14-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:27).

### 14.2. createJoinRequest

- `POST /api/groups/{groupId}/join-requests`

Group ACTIVE/unlocked, kỳ writable; student chưa có nhóm cùng kỳ/môn, chưa là thành viên, nhóm dưới 6, không có invitation PENDING cùng group. Pending request dưới 72h bị từ chối; hết hạn thì cancel và tạo mới; notify leader.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c14-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:34).

### 14.3. getJoinRequests

- `GET /api/groups/{groupId}/join-requests`

ADMIN, thành viên/leader hoặc assigned mentor xem request của nhóm; instructor không có quyền mặc định.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c14-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:45).

### 14.4. approveJoinRequest

- `POST /api/groups/{groupId}/join-requests/{requestId}/approve`
- `POST /api/groups/join-requests/{requestId}/approve`

Khóa student và group; PENDING, chưa hết hạn 72h, groupId nếu truyền phải đúng. Leader/admin; nhóm ACTIVE, unlocked cho cả ADMIN và student, dưới 6 và student chưa có nhóm cùng scope. Kỳ writable chỉ bắt buộc với non-admin. Tạo membership, set ACCEPTED/responder/time (responder student có thể null với ADMIN); cancel requests và decline invitations khác cùng scope, notify requester.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c14-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:55).

### 14.5. rejectJoinRequest

- `POST /api/groups/{groupId}/join-requests/{requestId}/reject`
- `POST /api/groups/join-requests/{requestId}/reject`

PENDING và groupId đúng; leader/admin; khóa group, chỉ non-admin chịu student write gate, không kiểm isLock. Set REJECTED/responder/time, notify requester. Java không áp expiration check giống approve ở nhánh này.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c14-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:69).

### 14.6. cancelJoinRequest

- `POST /api/groups/{groupId}/join-requests/{requestId}/cancel`
- `POST /api/groups/join-requests/{requestId}/cancel`

Chỉ requester, PENDING và groupId đúng; khóa group, student write gate nhưng không kiểm isLock; set CANCELED/respondedAt.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c14-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:83).

<a id="c15"></a>

## 15. GroupMeetingController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java). Luồng xử lý: [MeetingServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MeetingServiceImpl.java), [MentorScheduleTimePolicy](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MentorScheduleTimePolicy.java).

POST có slotId: leader đặt slot; không slotId: assigned mentor tạo trực tiếp. Tối đa 2 cuộc SCHEDULED+COMPLETED mỗi nhóm. Lịch bắt đầu phút 00/30, không giây/nano, kéo dài đúng 60 phút. Link meeting được trim, bắt đầu http:// hoặc https://, dài tối đa 500 ký tự; không nhầm với regex Google Meet nghiêm ngặt của availability. Quyền đọc gồm member, assigned mentor và assigned instructor; ADMIN không mặc nhiên được đọc.

### 15.1. getAvailableMentorSlots

- `GET /api/groups/{groupId}/mentor/availability`

Chỉ member; group phải có mentor. Trả slots AVAILABLE của mentor, startAt>now, tăng dần.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c15-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:34).

### 15.2. createMeeting

- `POST /api/groups/{groupId}/mentor/meetings`

Booking khóa group+slot; leader và kỳ writable; kiểm quota 2, slot future/đúng mentor/AVAILABLE/time policy/link. Set slot BOOKED, tạo SCHEDULED, copy lịch/link/note; notify mentor+members trừ actor. Tạo trực tiếp chỉ assigned mentor, kiểm lịch/overlap/link/quota nhưng KHÔNG kiểm startAt ở tương lai; note trim/blank→null; notify members. DTO CreateOrBook bắt buộc chọn slotId hoặc cặp startAt/endAt, không đồng thời; nhánh direct bắt buộc meetLink, note/link tối đa 500 ký tự.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c15-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:45).

### 15.3. updateMeeting

- `PATCH /api/groups/{groupId}/mentor/meetings/{meetingId}`

Assigned mentor, meeting thuộc group và mutable; startAt/endAt không truyền giữ cũ; revalidate lịch/overlap/link, không kiểm future. HTTP bắt buộc meetLink do UpdateMentorMeetingRequest có @NotBlank và controller có @Valid; fallback null trong service không làm field này optional ở HTTP. Note chỉ sửa khi non-null; notify members.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c15-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:66).

### 15.4. submitEvidence

- `PUT /api/groups/{groupId}/mentor/meetings/{meetingId}/evidence`

Bất kỳ active student member, không chỉ leader. Kỳ writable; meeting SCHEDULED, chưa evidence, đã kết thúc. Trim URL; lưu người/thời gian, chuyển COMPLETED; notify mentor và instructor. Không cho gửi lần hai.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c15-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:74).

### 15.5. cancelMeeting

- `PATCH /api/groups/{groupId}/mentor/meetings/{meetingId}/cancel`

CANCELED trả lại hiện hữu; COMPLETED trả 400, có evidence trả 409. Assigned mentor hủy, reason trim; slot liên kết chuyển CANCELED, không AVAILABLE; notify members trừ actor.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c15-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:82).

### 15.6. confirmMeeting

- `PATCH /api/groups/{groupId}/mentor/meetings/{meetingId}/confirm`

CANCELED trả 400, COMPLETED trả hiện hữu; phải có slot và slot đã bắt đầu. Leader hoặc assigned mentor xác nhận, student cần kỳ writable. Chỉ đặt timestamp lần đầu. Không tự hoàn thành dù cả hai xác nhận; vẫn cần evidence.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c15-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:95).

### 15.7. getGroupMeetings

- `GET /api/groups/{groupId}/mentor/meetings`

Group tồn tại; member/assigned mentor/assigned instructor được đọc. DTO đầy đủ, legacy thời gian thiếu fallback từ slot.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c15-m7); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:107).

### 15.8. getGroupMeetingDetail

- `GET /api/groups/{groupId}/mentor/meetings/{meetingId}`

Meeting phải thuộc group, cùng quyền đọc; sai group trả 404.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c15-m8); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:118).

<a id="c16"></a>

## 16. GroupProblemController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java). Luồng xử lý: [GroupProblemServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupProblemServiceImpl.java).

Prefix chỉ STUDENT. Leader mới được select/clear/propose/update/delete; GET proposals cho mọi thành viên của nhóm. Các thao tác ghi áp dụng kiểm tra kỳ writable; đối chiếu transaction/khóa theo service gốc. Proposal mới tự trở thành selectedProblem.

### 16.1. selectProblem

- `POST /api/groups/{groupId}/problems/select`

Leader, kỳ writable; problem tồn tại, ACTIVE và OFFICIAL. Hai lỗi trạng thái/type riêng biệt; lưu selection, trả GroupDetail.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c16-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:27).

### 16.2. clearProblem

- `DELETE /api/groups/{groupId}/problems/select`

Leader, kỳ writable; đặt selectedProblem=null, trả data=null.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c16-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:42).

### 16.3. proposeProblem

- `POST /api/groups/{groupId}/problems/propose`

Leader, kỳ writable; domain bắt buộc ACTIVE; tạo SELF_PROPOSED/PENDING_REVIEW với chủ sở hữu group/student và trường DTO. Code tự sinh `SP-{groupId}-{8 ký tự đầu UUID}`; title/statement được lấy nguyên request, không trim trong service. Chọn proposal trong cùng transaction.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c16-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:56).

### 16.4. updatePendingProposal

- `PUT /api/groups/{groupId}/problems/proposals/{problemId}`

Leader, kỳ writable; sai owner group→404, sai SELF_PROPOSED→400, không PENDING_REVIEW→409; domain ACTIVE; sửa fields.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c16-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:71).

### 16.5. deletePendingProposal

- `DELETE /api/groups/{groupId}/problems/proposals/{problemId}`

Cùng kiểm owner/type/status; clear selection nếu đang chọn; xóa proposal thật.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c16-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:87).

### 16.6. getGroupProposals

- `GET /api/groups/{groupId}/problems/proposals`

Student có membership của group được đọc các proposal do group đề xuất; không cần là leader. Không có group/profile/membership trả 403 theo service gốc.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c16-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:102).

<a id="c17"></a>

## 17. GroupRecruitmentRoleController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupRecruitmentRoleController.java).

Authenticated. Nguồn là enum RecruitmentRole theo thứ tự khai báo: 26 roles thuộc TECHNOLOGY, DESIGN, BUSINESS, COMMUNICATION, LANGUAGE_LEGAL. Không query DB, không có CRUD.

### 17.1. getRecruitmentRoles

- `GET /api/group-recruitment-roles`

Trả mảng code/category/displayNameVi/displayNameEn đúng enum; giữ nguyên nhãn tiếng Việt, tiếng Anh và thứ tự.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c17-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupRecruitmentRoleController.java:22).

<a id="c18"></a>

## 18. GroupTaskController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java). Luồng xử lý: [GroupTaskServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupTaskServiceImpl.java), [StudentTermWriteGuard](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/StudentTermWriteGuard.java).

Đọc/comment: student member hoặc assigned mentor; không mặc định cho admin/instructor. Create/update/assign/archive/restore/reorder: leader. Đổi cột/checklist: leader hoặc assignee còn membership. Student ghi cần kỳ writable; mentor comment không áp student gate. Năm trạng thái BACKLOG/TODO/IN_PROGRESS/REVIEW/DONE; bốn priority LOW/MEDIUM/HIGH/URGENT. Row lock và transaction đảm bảo thứ tự từng board/cột; stale version trả 409.

### 18.1. getBoard

- `GET /api/groups/{groupId}/board`
- `GET /api/groups/{groupId}/boards/{boardId}`

Đọc default board hoặc boardId; kiểm member/mentor. Lazy-create Default dưới lock khi thiếu; archived board trả 400. Lọc priority/assignee/search/includeArchived; trả 5 cột theo thứ tự cố định. Hai thống kê đếm toàn board, không chịu filter hiển thị.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:34).

### 18.2. createTask

- `POST /api/groups/{groupId}/tasks`

Leader; title trim 1–255; board cùng group/chưa archive. Initial status chỉ BACKLOG/TODO, mặc định BACKLOG; priority mặc định MEDIUM; dueAt future. List assignees null được xem như rỗng; các phần tử không được null/trùng và phải là thành viên hiện tại của group, không có kiểm tra riêng student/account ACTIVE ở bước này. Append position, ghi activities và notify assignees.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:53).

### 18.3. getTaskDetail

- `GET /api/groups/{groupId}/tasks/{taskId}`

Read access và đúng group/task; trả assignees/checklist/time/version/counters. Legacy task thiếu board có thể được gán Default.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:65).

### 18.4. updateTask

- `PATCH /api/groups/{groupId}/tasks/{taskId}`

Leader, task chưa archive; version bắt buộc (@NotNull) và phải khớp. PATCH non-null; title/priority hợp lệ; dueAt và clearDueAt không đồng thời. Chỉ kiểm dueAt future khi giá trị khác dueAt đang lưu; gửi lại cùng deadline đã quá hạn không bị rule này chặn. Chỉ ghi change activity khi có thay đổi.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:77).

### 18.5. replaceAssignees

- `PUT /api/groups/{groupId}/tasks/{taskId}/assignees`

Leader, version và list bắt buộc; list rỗng để xóa hết. Kiểm membership hiện tại/trùng/null, không tự thêm điều kiện student/account ACTIVE; diff added/removed, activity; notify người mới được gán, bỏ actor.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:90).

### 18.6. moveTask

- `PATCH /api/groups/{groupId}/tasks/{taskId}/move`

Task chưa archive, version bắt buộc và khớp, target status hợp lệ/position≥0. Reorder cùng cột chỉ leader; đổi cột cho leader/assignee. Khóa các cột liên quan, clamp vị trí, chuẩn hóa positions; luôn ghi TASK_MOVED activity, notification chỉ khi đổi status. No-op vẫn có activity nhưng không tự tăng entity version nếu entity không đổi.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:103).

### 18.7. archiveTask

- `DELETE /api/groups/{groupId}/tasks/{taskId}`

Leader, chưa archive; set archivedAt, compact cột, ghi activity; giữ assignment/history.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m7); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:116).

### 18.8. restoreTask

- `POST /api/groups/{groupId}/tasks/{taskId}/restore`

Leader, phải archived; append cuối cột, normalize; xóa assignment của người đã rời nhóm và ghi system activity; clear archivedAt, TASK_RESTORED.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m8); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:128).

### 18.9. addChecklistItem

- `POST /api/groups/{groupId}/tasks/{taskId}/checklist-items`

Leader/assignee; checklist title trim 1–500; append position, completed=false, activity.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m9); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:140).

### 18.10. updateChecklistItem

- `PATCH /api/groups/{groupId}/tasks/{taskId}/checklist-items/{itemId}`

Leader/assignee; item đúng task; optional title/completed/position, position≥0; reorder liên tục và completion metadata.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m10); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:153).

### 18.11. deleteChecklistItem

- `DELETE /api/groups/{groupId}/tasks/{taskId}/checklist-items/{itemId}`

Leader/assignee; xóa đúng item, compact position, activity.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m11); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:167).

### 18.12. getComments

- `GET /api/groups/{groupId}/tasks/{taskId}/comments`

Read access; page≥0,size>0, size>100 được clamp 100; mặc định theo chữ ký ở phụ lục, sort comment newest-first.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m12); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:180).

### 18.13. createComment

- `POST /api/groups/{groupId}/tasks/{taskId}/comments`

Member/assigned mentor; task chưa archive, content trim không rỗng; student kỳ đóng bị chặn. Lưu comment/activity, notify leader/assignees trừ actor.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m13); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:194).

### 18.14. updateComment

- `PATCH /api/groups/{groupId}/tasks/{taskId}/comments/{commentId}`

Chỉ author có quyền truy cập task, task mutable; content trim không rỗng; chỉ cập nhật editedAt và ghi activity khi content thực sự thay đổi.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m14); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:207).

### 18.15. deleteComment

- `DELETE /api/groups/{groupId}/tasks/{taskId}/comments/{commentId}`

Author hoặc student leader; task mutable, student write gate; xóa và ghi activity.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m15); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:221).

### 18.16. getActivities

- `GET /api/groups/{groupId}/tasks/{taskId}/activities`

Read access; page≥0,size>0, size>100 được clamp 100; activity newest-first, actor có thể null cho system, details JSON.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m16); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:234).

### 18.17. getMyAssignedTasks

- `GET /api/tasks/me`

Student: lấy task theo assignment.studentId của người gọi; lọc groupId/status/priority/overdue/dueBefore, không có search. Query không join kiểm lại membership; không mô tả thành bảo đảm “chỉ nhóm hiện còn tham gia” độc lập với cleanup assignment. Bỏ archived; dueBefore strict<; page≥0,size>0, clamp size về 100. Sắp dueAt ASC nulls-last rồi updatedAt DESC.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m17); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:248).

### 18.18. reorderTask

- `POST /api/groups/{groupId}/tasks/reorder`

Reorder wrapper truyền taskId/status/position/version vào Move; trả 200 data=null.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m18); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:265).

<a id="c19"></a>

## 19. ImportController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java). Luồng xử lý: [ImportServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ImportServiceImpl.java), [ImportValidationServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ImportValidationServiceImpl.java), [ImportRowExecutorImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ImportRowExecutorImpl.java), [ImportJobSupport](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ImportJobSupport.java).

Chỉ ADMIN. Multipart file CSV/XLSX; lỗi file rỗng/tên/type trả 400 trước queue. Một job QUEUED/RUNNING tại một thời điểm, HTTP202. Worker xử lý và dọn file tạm. Dòng lưu bằng REQUIRES_NEW, một dòng lỗi không rollback dòng thành công. Batch FAILED nếu tất cả dòng fail hoặc lỗi xử lý; còn lại COMPLETED, vẫn có row errors/warnings.

### 19.1. importStudents

- `POST /api/imports/students`

Queue STUDENT. Validate email/code/name/password nếu có≥8/gender/date; trùng file/DB thành row errors. Identity email+code cùng student inactive có thể reactivate giữ hash. Workbook dùng sheet kỳ/môn, kế thừa group context; group identity (term,normalized name), leader so tên bỏ dấu/fallback warning, mentor parse code/warning, membership không trùng scope, Default board khi tạo group.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c19-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:43).

### 19.2. importMentors

- `POST /api/imports/mentors`

Queue MENTOR; code/email/name bắt buộc; experience nhận số/nhãn Việt. BCrypt password nhập hoặc random; ACTIVE,mustChangePassword=false; không tạo group.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c19-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:54).

### 19.3. getBatchStatus

- `GET /api/imports/{batchId}`

Đọc batch theo public id: target/file/type/status/counts/start/finish; thiếu 404, không trả password.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c19-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:65).

### 19.4. getBatchErrors

- `GET /api/imports/{batchId}/errors`

Page0,size20 (1–100), rowNumber≥1; filter search/rowNumber/fieldName/errorCode; rowNumber ASC,id ASC; trả field/message/error code/row number.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c19-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:74).

### 19.5. getStudentTemplate

- `GET /api/imports/templates/students`

Tải template student với format và filename/header/content type ở phụ lục; không JSON.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c19-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:110).

### 19.6. getMentorTemplate

- `GET /api/imports/templates/mentors`

Tải template mentor, đúng cột/alias/format; không tạo job.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c19-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:119).

### 19.7. getProblemBankTemplate

- `GET /api/imports/templates/problem-bank`

Tải template problem bank theo format CSV/XLSX, đúng sheet/header parser.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c19-m7); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:128).

<a id="c20"></a>

## 20. InstructorGroupBoardController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorGroupBoardController.java). Luồng xử lý: [InstructorGroupBoardServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/InstructorGroupBoardServiceImpl.java).

Chỉ INSTRUCTOR có account và profile ACTIVE. Danh sách chỉ gồm group ACTIVE, có thành viên, thuộc kỳ OPEN. assignment nhận ALL/AVAILABLE/MINE/OTHER; page mặc định 0, size 12 (1–100). Summary và course counts không chỉ tính trong trang hiện tại.

### 20.1. getBoard

- `GET /api/instructor/groups/board`

Lọc term/course đã trim, không phân biệt hoa thường. Search gồm tên nhóm, groupNo, project và tên/MSSV/email/className thành viên. Summary áp term/course, không áp search/assignment; courses chỉ áp term, không áp courseCode. Trang sắp createdAt DESC, id DESC; members ưu tiên leader rồi joinedAt/id.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c20-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorGroupBoardController.java:36).

### 20.2. claimGroup

- `POST /api/instructor/groups/{groupId}/claim`

Khóa kỳ rồi group; kỳ OPEN, group ACTIVE và có thành viên. Chưa gán thì gán instructor hiện tại; đã gán chính mình trả nguyên trạng; gán người khác trả 409. Khác API admin assignment dùng accountId.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c20-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorGroupBoardController.java:63).

<a id="c21"></a>

## 21. InstructorProblemController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorProblemController.java). Luồng xử lý: [ProblemBankServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemBankServiceImpl.java).

Chỉ INSTRUCTOR có account/profile ACTIVE. Không được duyệt proposal của nhóm do instructor khác phụ trách.

### 21.1. getPendingProblems

- `GET /api/instructor/problems/pending`

Chỉ lấy SELF_PROPOSED/PENDING_REVIEW có proposedByGroup.instructor là người gọi.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c21-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorProblemController.java:33).

### 21.2. reviewProblem

- `PATCH /api/instructor/problems/{problemId}/review`

Kiểm type/status và nhóm được giao. APPROVED chuyển OFFICIAL/ACTIVE; REJECTED cần comment và bỏ selection nếu đang chọn. Lưu người duyệt, thời gian và comment như luồng admin review.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c21-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorProblemController.java:40).

<a id="c22"></a>

## 22. InstructorSubmissionController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorSubmissionController.java). Luồng xử lý: [MilestoneSubmissionServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneSubmissionServiceImpl.java).

Chỉ INSTRUCTOR; account ACTIVE và có profile. Đây là đọc submission legacy, không mở lại các API submit đã bị comment.

### 22.1. getSubmissions

- `GET /api/instructor/submissions`

Chỉ lấy submission khi cả milestone.owner và group.assignedInstructor đều là người gọi. Lọc term/course đã trim, không phân biệt hoa thường; milestoneId/groupId/status/late tùy chọn. Trả mảng, không paging. gradeMaxScore fallback milestone.maxScore khi grade.maxScore null.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c22-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorSubmissionController.java:29).

<a id="c23"></a>

## 23. MentorAvailabilityController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorAvailabilityController.java). Luồng xử lý: [MentorAvailabilityServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MentorAvailabilityServiceImpl.java), [MentorScheduleTimePolicy](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MentorScheduleTimePolicy.java).

Chỉ MENTOR, resolve profile qua email. Slot đúng 60 phút, bắt đầu phút 00/30, không giây/nano, cùng ngày Asia/Ho_Chi_Minh; link dạng https://meet.google.com/xxx-xxxx-xxx bằng chữ thường. Không tự thêm điều kiện future vào create: booking kiểm tra future riêng.

### 23.1. createSlot

- `POST /api/mentor/availability`

Kiểm time/link, không overlap slot chưa CANCELED của cùng mentor; tạo AVAILABLE, HTTP 200.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c23-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorAvailabilityController.java:36).

### 23.2. listSlots

- `GET /api/mentor/availability`

Chỉ slot của mình, startAt DESC, có cả lịch sử trạng thái.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c23-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorAvailabilityController.java:48).

### 23.3. listMySlots

- `GET /api/mentor/availability/me`

Alias /me gọi cùng listSlots.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c23-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorAvailabilityController.java:57).

### 23.4. updateSlot

- `PATCH /api/mentor/availability/{id}`

Slot phải thuộc mentor; BOOKED không được sửa. Time không truyền giữ cũ; link truyền vào phải hợp lệ; kiểm overlap loại chính slot; note chỉ cập nhật khi non-null.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c23-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorAvailabilityController.java:65).

### 23.5. cancelSlot

- `DELETE /api/mentor/availability/{id}`

Slot phải thuộc mentor và không BOOKED. Chuyển CANCELED, không xóa vật lý; trả data=null.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c23-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorAvailabilityController.java:78).

<a id="c24"></a>

## 24. MentorMeetingReportController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorMeetingReportController.java). Luồng xử lý: [MentorMeetingReportServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MentorMeetingReportServiceImpl.java).

Chỉ MENTOR. Bao gồm nhóm hiện được gán hoặc nhóm có meeting lịch sử của mentor; giữ lịch sử khi nhóm đổi mentor. Thời gian báo cáo theo ICT.

### 24.1. listReportTerms

- `GET /api/mentor/meeting-reports/terms`

Union mã kỳ từ nhóm và meeting của mentor; chỉ academic term tồn tại, sắp createdAt DESC, id DESC; trả termCode/status.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c24-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorMeetingReportController.java:33).

### 24.2. exportReport

- `GET /api/mentor/meeting-reports/export.xlsx`

Kỳ bắt buộc và phải tồn tại. Xuất hai sheet Group Summary (9 cột), Meeting Details (18 cột), gồm link, minh chứng, người gửi, xác nhận, hủy; giữ nhóm chưa có meeting. Sắp course/groupNo, meeting start/id; cell có kiểu dữ liệu, hyperlink, freeze/filter. Tên file mentor-meeting-report-{safeTerm}.xlsx.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c24-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorMeetingReportController.java:42).

<a id="c25"></a>

## 25. MilestoneGradeController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeController.java). Luồng xử lý: [MilestoneGradeServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneGradeServiceImpl.java).

gradeSubmission/updateGrade nằm trong block comment Java: không phải API đang hoạt động. Ba route thực tế gồm hai đọc grade legacy và một bulk trả 404. Read cho ADMIN, leader/member hoặc instructor được gán; mentor/người ngoài trả 403.

### 25.1. getGradeBySubmissionId

- `GET /api/milestone-submissions/{submissionId}/grades`

Tra submission, kiểm quyền group rồi tra grade; thiếu submission/grade trả 404; trả MilestoneGradeDto.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c25-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeController.java:62).

### 25.2. getGradesByGroupId

- `GET /api/milestone-submissions/groups/{groupId}/grades`

Tra group và quyền; trả grade của các submission thuộc nhóm, không phải grade matrix mới.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c25-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeController.java:74).

### 25.3. bulkGrade

- `POST /api/milestone-submissions/bulk-grade`

Luôn trả 404, envelope code=404, message="Bulk grading not supported" sau security; không chấm điểm.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c25-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeController.java:86).

<a id="c26"></a>

## 26. MilestoneGradeMatrixController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java). Luồng xử lý: [MilestoneGradeMatrixServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneGradeMatrixServiceImpl.java).

Chấm theo group/milestone và contribution của từng active member. Không yêu cầu tổng contribution bằng 100: mỗi thành viên có phần trăm 0–100. Grade scope cần milestone ACTIVE, weight>0, group ACTIVE, cùng term/course và instructor. Student write áp dụng kỳ readonly. Service có transaction; revision FOR UPDATE ở luồng contribution/agreement và kiểm agreement khi chấm lần đầu, không suy ra mọi lần sửa grade đều khóa revision/group.

### 26.1. upsertGroupGrade

- `PUT /api/instructor/milestones/{milestoneId}/groups/{groupId}/grade`

INSTRUCTOR sở hữu milestone và group; score trong 0..maxScore. Phải có đúng tập contribution active members, không thiếu/thừa; lần chấm đầu cần tất cả AGREE, lần sửa không yêu cầu lại. Lưu maxScore/weight snapshots, người chấm, thời gian, feedback. Individual score: groupScore × percent ÷ 100 làm tròn HALF_UP scale 8, sau đó HALF_UP scale 4. Notify khi điểm/feedback thay đổi.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c26-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:42).

### 26.2. upsertContributions

- `PUT /api/groups/{groupId}/milestones/{milestoneId}/contributions`

Leader nộp contribution cho đủ active members, mỗi người đúng một lần; trùng/thiếu/người ngoài trả 400. Đã có grade trả 409. Upsert scores, xóa calculated score/snapshots; tăng revision, xóa agreement cũ, notify revision; trả member scores sắp MSSV.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c26-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:54).

### 26.3. getGroupGradeMatrix

- `GET /api/groups/{groupId}/grades`

Read cho ADMIN, instructor được gán hoặc student member/leader. Chưa gán instructor trả 409; trả 409 do owner lệch chỉ khi KHÔNG có milestone visible của instructor hiện tại nhưng CÓ milestone visible của instructor khác trong scope. Columns loại ARCHIVED/INACTIVE, kèm agreement và completeness. Mỗi hạng tử weighted = (individualScore ÷ maxScore, HALF_UP scale 8) × 10 × weight ÷ 100, HALF_UP scale 4; tổng scale 4. Matrix complete cần ít nhất một milestone và mọi column gradeComplete. Zero columns: member row có thể complete=true/total=0.0000 nhưng matrix complete=false. Zero active members: các phép allMatch về member scores là true; không tự thêm điều kiện non-empty. Đã có grade thì agreement summary trả AGREED, approved=required.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c26-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:66).

### 26.4. exportInstructorGradesCsv

- `GET /api/instructor/grades/export.csv`

INSTRUCTOR export CSV theo term/course/group và các milestone đúng scope. UTF-8 BOM, CRLF; cột group/student, contribution và individual score mỗi milestone, finalTotal/complete. Java dùng csvCell nhưng header milestone được escape title trước rồi mới nối hậu tố contribution/score ngoài cell đã quote; cần test title có dấu phẩy/nháy, không khẳng định mọi header đều là CSV chuẩn. Tên file grades-{term hoặc all-terms}-{course hoặc all-courses}.csv.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c26-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:76).

### 26.5. upsertContributionAgreement

- `PUT /api/groups/{groupId}/milestones/{milestoneId}/contribution-agreement`

Active member, chưa grade, đã có revision. Nếu đã có REQUEST_CHANGES thì chặn phản hồi tiếp đến revision mới. REQUEST_CHANGES cần reason trim; AGREE đặt reason null. Upsert và notify; đếm active members, trạng thái NOT_SUBMITTED/PENDING/CHANGES_REQUESTED/AGREED.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c26-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:95).

### 26.6. exportInstructorGradesXlsx

- `GET /api/instructor/grades/export.xlsx`

INSTRUCTOR export XLSX có layout riêng: sheet RAW, Tên Nhóm, Họ & Tên, MSSV, Email (đuôi FPT), một cột điểm mỗi milestone, Final. Cell số định dạng 0.0###, header bold/frozen, autofilter/autosize; không có merge cell. Final chỉ ghi khi member row complete, ngược lại để trống. Không dùng nguyên layout CSV.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c26-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:106).

<a id="c27"></a>

## 27. MilestoneSubmissionController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneSubmissionController.java). Luồng xử lý: [MilestoneSubmissionServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneSubmissionServiceImpl.java).

POST/PATCH submission đã bị comment trong Java; chỉ ba GET đang hoạt động. DTO chứa grade legacy, maxScore null fallback milestone.maxScore.

### 27.1. getSubmissionById

- `GET /api/milestone-submissions/{submissionId}`

Submission phải tồn tại. ADMIN, leader/member hoặc instructor HIỆN ĐANG được gán group được đọc; không chỉ dựa vào owner cũ của milestone.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c27-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneSubmissionController.java:61).

### 27.2. getSubmissionsByGroupId

- `GET /api/milestone-submissions/groups/{groupId}`

Group phải tồn tại, áp cùng quyền đọc; trả submissions của group.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c27-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneSubmissionController.java:73).

### 27.3. getSubmissionsByMilestoneId

- `GET /api/milestone-submissions/milestones/{milestoneId}`

Chỉ INSTRUCTOR sở hữu milestone; lọc thêm group.instructor hiện tại là người gọi, kể cả sau khi nhóm được chuyển sang instructor khác.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c27-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneSubmissionController.java:85).

<a id="c28"></a>

## 28. NotificationController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/NotificationController.java). Luồng xử lý: [NotificationServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/NotificationServiceImpl.java).

Mọi role authenticated với account ACTIVE, chỉ dữ liệu của recipient hiện tại. Khi gửi: distinct recipients ACTIVE, dedup theo recipient/eventKey; lưu DB rồi push STOMP sau commit tới /user/queue/notifications. Realtime lỗi không rollback nghiệp vụ. action.key/params có fallback dữ liệu legacy.

### 28.1. getNotifications

- `GET /api/notifications`

Page mặc định 0, size 20; page<0 hoặc size≤0 trả 400; size>100 được clamp 100. unreadOnly tùy chọn; sắp createdAt DESC, id DESC.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c28-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/NotificationController.java:25).

### 28.2. getUnreadCount

- `GET /api/notifications/unread-count`

Đếm bản ghi readAt=null của recipient.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c28-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/NotificationController.java:37).

### 28.3. markAsRead

- `PATCH /api/notifications/{id}/read`

Tra notification và ownership (sai người trả 403); chỉ đặt readAt lần đầu, gọi lại không đổi thời gian.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c28-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/NotificationController.java:44).

### 28.4. markAllAsRead

- `PATCH /api/notifications/read-all`

Mark tất cả của recipient; trả số unread còn lại, không phải số bản ghi vừa cập nhật.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c28-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/NotificationController.java:54).

<a id="c29"></a>

## 29. ProblemController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemController.java). Luồng xử lý: [ProblemBankServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemBankServiceImpl.java).

Authenticated, service yêu cầu account tồn tại. Java không ẩn problem theo role; không tự lọc OFFICIAL/ACTIVE khi không truyền status/sourceType.

### 29.1. getProblems

- `GET /api/problems`

Page mặc định 0, size 10, giới hạn 1–100. Search code/title/statement, lọc domainCode/difficulty/expectedOutput/sourceType/status. Sắp createdAt DESC, id DESC; trả PageResponse.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c29-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemController.java:36).

### 29.2. getProblemById

- `GET /api/problems/{id}`

Tra id; thiếu trả 404 "Problem not found with id: {id}". Trả chi tiết domain/proposer/reviewer/time, không tự chặn SELF_PROPOSED/INACTIVE.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c29-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemController.java:80).

<a id="c30"></a>

## 30. ProblemCriteriaController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemCriteriaController.java). Luồng xử lý: [ProblemBankServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemBankServiceImpl.java).

Authenticated. Danh mục tiêu chí evaluation hiện chỉ đọc, không có CRUD.

### 30.1. getActiveCriteria

- `GET /api/problem-evaluation-criteria`

Chỉ active=true, displayOrder ASC; trả id/code/category/question/suggestion/maxScore/displayOrder/active/createdAt/updatedAt.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c30-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemCriteriaController.java:27).

<a id="c31"></a>

## 31. ProblemDomainController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemDomainController.java). Luồng xử lý: [ProblemBankServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemBankServiceImpl.java).

Authenticated theo security fallback. status có kiểu ProblemStatus enum, không phải chuỗi tự do.

### 31.1. getProblemDomains

- `GET /api/problem-domains`

Lọc status tùy chọn; search trim/lowercase trên code/name/description. Không truyền status thì giữ INACTIVE. Sắp createdAt DESC, id DESC; trả mảng không paging.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c31-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemDomainController.java:28).

<a id="c32"></a>

## 32. ProblemImportController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemImportController.java). Luồng xử lý: [ProblemImportServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemImportServiceImpl.java), [ProblemImportRowExecutorImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemImportRowExecutorImpl.java), [ImportJobSupport](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ImportJobSupport.java).

Chỉ ADMIN. Dùng queue/admission/background chung với imports. CSV unified hoặc XLSX có domain/problem sheets và alias header. Validate domain trước problem; code trùng trong file không phân biệt hoa thường là lỗi, mã đã có ở DB thì upsert.

### 32.1. importProblemBank

- `POST /api/imports/problem-bank`

Multipart file, HTTP 202, target PROBLEM_BANK. Domain code≤50/name≤255; problem code≤100/title≤255, statement/difficulty bắt buộc. Difficulty nhận BEGINNER/INTERMEDIATE/ADVANCED và alias easy/medium/hard/beginer; status chỉ ACTIVE/INACTIVE. Domain không tồn tại và không valid trong batch: code thực tế clear reference, không tự fail row. Mỗi dòng save REQUIRES_NEW, problem luôn OFFICIAL, metadata optional blank thành null; errors/counts đọc qua imports/{batchId}.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c32-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemImportController.java:27).

<a id="c33"></a>

## 33. ProfileController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProfileController.java). Luồng xử lý: [ProfileServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProfileServiceImpl.java).

Authenticated. Khi mustChangePassword=true, gate chỉ miễn auth/me, auth/logout, profile/me/password; GET profile/me không tự được miễn. DTO không lộ password hash.

### 33.1. getMyProfile

- `GET /api/profile/me`

Tra account ACTIVE; trả account và profile theo role, memberships cho student. Không có account trả 401, inactive trả 403 trong service.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c33-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProfileController.java:31).

### 33.2. updateMyProfile

- `PATCH /api/profile/me`

Sửa profile đúng role; ADMIN trả 403, thiếu profile trả 404. Chỉ cập nhật field non-null; fullName trim không trắng; phone/text optional trim, blank thành null. Giữ maxlength và yearsOfExperience≥0 của DTO. Không đổi email/role/code/status.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c33-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProfileController.java:38).

### 33.3. changeMyPassword

- `PATCH /api/profile/me/password`

Kiểm currentPassword bằng BCrypt; sai trả 400. Hash mật khẩu mới, đặt mustChangePassword=false, revoke mọi refresh token; không blacklist access token hiện tại.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c33-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProfileController.java:48).

<a id="c34"></a>

## 34. StudentAccountImportController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentAccountImportController.java). Luồng xử lý: [StudentAccountImportServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/StudentAccountImportServiceImpl.java), [StudentAccountImportValidationService](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/StudentAccountImportValidationService.java), [ImportJobSupport](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ImportJobSupport.java).

Chỉ ADMIN. Luồng roster riêng, không tạo group/membership. Cột GroupName map student.className. Đúng năm header RollNumber/Fullname/Email/SubjectCode/GroupName; SubjectCode chỉ EXE101/EXE201.

### 34.1. importStudentAccounts

- `POST /api/imports/student-accounts`

File CSV/XLSX, HTTP 202, target STUDENT_ACCOUNT. Validate required/length/email/trùng. Email và MSSV cùng khớp một student có cả student.status và account.status INACTIVE, account.role=STUDENT mới được đưa vào danh sách reactivate, giữ hash; duplicate ACTIVE là lỗi. Code uppercase, email lowercase; tài khoản mới dùng random password mạnh, mustChangePassword=false; không trả password.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c34-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentAccountImportController.java:39).

### 34.2. getTemplate

- `GET /api/imports/templates/student-accounts`

Download template CSV/XLSX có đúng năm cột roster; filename/content type ở phụ lục; không tạo job.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c34-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentAccountImportController.java:56).

<a id="c35"></a>

## 35. StudentController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentController.java). Luồng xử lý: [StudentServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/StudentServiceImpl.java).

Chỉ STUDENT; API detail ẩn target student/account inactive bằng 404. API ungrouped chỉ lọc student.status ACTIVE, không kiểm account.status; không áp quy tắc detail cho list. Ungrouped là chưa có nhóm trong term/course, không phải chưa thuộc nhóm nào trên hệ thống.

### 35.1. getStudentById

- `GET /api/students/{id}`

Student được đọc profile ACTIVE của student khác theo profile id. Role khác trả 403; target inactive/thiếu account trả 404.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c35-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentController.java:25).

### 35.2. getUngroupedStudents

- `GET /api/students/ungrouped`

term/course optional ở chữ ký HTTP nhưng service bắt buộc; truyền nguyên giá trị tới query, không trim/uppercase trong service. Page≥0, size>0, Java không cap 100. Query chỉ yêu cầu student ACTIVE, không có điều kiện account ACTIVE; không có membership cùng scope; search code/fullName/email; sắp createdAt DESC, id DESC. Không suy rộng bộ lọc của getStudentById sang endpoint này.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c35-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentController.java:39).

<a id="c36"></a>

## 36. StudentGroupGradeController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentGroupGradeController.java). Luồng xử lý: [MilestoneGradeServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneGradeServiceImpl.java).

Average từ legacy submission grades, không phải member total của grade matrix. ADMIN/leader/member/instructor được gán được đọc; mentor không được đọc.

### 36.1. calculateAverageGradeForGroup

- `GET /api/student-groups/{groupId}/average-grade`

Group phải tồn tại. Tính sum(score × weight)/sum(weight), bỏ milestone INACTIVE hoặc weight null; denominator=0 trả 0.00; HALF_UP hai số. ARCHIVED không bị loại bởi điều kiện INACTIVE. averageGrade và finalScore cùng giá trị.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c36-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentGroupGradeController.java:24).

<a id="c37"></a>

## 37. TaskBoardController

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/TaskBoardController.java). Luồng xử lý: [TaskBoardServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/TaskBoardServiceImpl.java), [StudentTermWriteGuard](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/StudentTermWriteGuard.java).

Các alias boards/task-boards tương đương. GET cho student member/mentor được gán; ghi chỉ student leader, kỳ writable. Service có transaction; get/update/delete khóa hàng group, create chỉ findById, không FOR UPDATE. Mỗi nhóm chỉ một default, có thể nhiều board.

### 37.1. getBoards

- `GET /api/groups/{groupId}/boards`
- `GET /api/groups/{groupId}/task-boards`

List non-archived theo position ASC; repository không cam kết id tie-break. Không có board active và chưa có default thì lazy-create Default sau lock/recheck; GET có side effect có chủ ý.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c37-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/TaskBoardController.java:38).

### 37.2. createBoard

- `POST /api/groups/{groupId}/boards`
- `POST /api/groups/{groupId}/task-boards`

Name bắt buộc, trim, tối đa 255; tạo board không default, position=max+1, createdBy là leader. HTTP 201; code trong envelope vẫn theo APIResponse Java.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c37-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/TaskBoardController.java:53).

### 37.3. updateBoard

- `PATCH /api/groups/{groupId}/boards/{boardId}`
- `PATCH /api/groups/{groupId}/task-boards/{boardId}`

PATCH name/description/position/archived/defaultBoard; name không trắng và≤255. Position không có @Min hay kiểm range trong service, không mặc định loại số âm. Không archive default; promote mới phải demote default cũ. Không unset default trực tiếp (400).

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c37-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/TaskBoardController.java:69).

### 37.4. deleteBoard

- `DELETE /api/groups/{groupId}/boards/{boardId}`
- `DELETE /api/groups/{groupId}/task-boards/{boardId}`

Không xóa default (400). Có bất kỳ task nào, kể cả archived, trả 409. Board rỗng được xóa, HTTP 200 data=null.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c37-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/TaskBoardController.java:87).

## Cách dùng để nghiệm thu

Với từng method ở trên: dựng fixture → gọi Java và Flowzy với cùng role/dữ liệu → so HTTP/JSON/header → đọc DB kiểm trạng thái → kiểm notification/file → lặp với dữ liệu không hợp lệ, role sai, kỳ đóng, retry và cạnh tranh. Chỉ đánh dấu đạt khi có test chứng minh, không vì method tồn tại hoặc test route coverage pass. Các sai lệch đã phát hiện và trường hợp còn thiếu được ghi riêng trong báo cáo audit.
