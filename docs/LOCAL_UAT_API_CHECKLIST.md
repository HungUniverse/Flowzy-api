# Flowzy — checklist nghiệm thu từng controller và API

Ngày soạn: 2026-09-27. Đọc cùng [luồng chạy và dữ liệu mẫu](D:/Flowzy-api/docs/LOCAL_UAT_PLAYBOOK.md).

## Phạm vi và cách thực hiện

37 controller Java; 174 method đang hoạt động; 192 tổ hợp HTTP method–route tính riêng alias. Các checkbox đều **NOT RUN** khi phát hành. Đây là expected contract lấy từ Java/đặc tả, không phải response Flowzy đã được kiểm thử.

Với mỗi mục: (1) hoàn tất fixture và quyền trong luồng F tương ứng; (2) thay path ID/query/body bằng dữ liệu thật; (3) gọi API, ghi HTTP/code/message/data; (4) kiểm readback và side effect trong phần nghiệp vụ; (5) chạy nhánh lỗi đã mô tả và kiểm dữ liệu không đổi ngoài ý muốn; (6) ghi PASS/FAIL/BLOCKED. Alias ghi kết quả riêng, không đánh dấu cả cụm sau một request.

Body và query dùng đúng camelCase. Ô `mặc định` lấy từ annotation Java; service có thể bắt buộc query dù annotation cho optional, ví dụ student ungrouped/feedback export. Đừng suy quyền chỉ từ annotation: nguồn security/service và phần điều kiện chung ưu tiên. Các trường JSON trong phần cuối lấy từ snapshot schema để nhập liệu; DTO Java liên kết là chuẩn nếu khác.

Lỗi chung: token thiếu/hỏng/đã logout →401; sai role/resource →403 theo guard; validation →400; conflict/concurrency →409 khi được nêu; missing resource →404 theo thứ tự kiểm quyền. Không ép mọi lỗi nghiệp vụ vào cùng một mã. Mỗi lỗi phải đối chiếu message trong DTO/service tại link nguồn. File trả bytes và headers, không envelope.

**An toàn:** close term/archive/delete/reset-password chỉ trên fixture UAT. Restore chỉ trên stack disposable có DB/volume riêng. Legacy submissions/grades cần fixture riêng vì không có API tạo đang hoạt động. Google cần người dùng xác thực thật.

## Mục lục kiểm soát độ phủ

| Mã | Controller | Luồng | HTTP routes | Đã pass / tổng |
|---|---|---|---:|---|
| C01 | [AcademicTermController](#c01) | F04 | 1 | __ / 1 |
| C02 | [AdminFeedbackController](#c02) | F15 | 2 | __ / 2 |
| C03 | [AdminGroupController](#c03) | F05 | 1 | __ / 1 |
| C04 | [AdminProblemController](#c04) | F07 | 6 | __ / 6 |
| C05 | [AdminTermController](#c05) | F04, F15, F16 | 5 | __ / 5 |
| C06 | [AdminUserController](#c06) | F02 | 7 | __ / 7 |
| C07 | [AuthController](#c07) | F01 | 5 | __ / 5 |
| C08 | [BackupController](#c08) | F17 | 7 | __ / 7 |
| C09 | [CourseMilestoneController](#c09) | F10 | 16 | __ / 16 |
| C10 | [DashboardController](#c10) | F14 | 15 | __ / 15 |
| C11 | [FeedbackController](#c11) | F15 | 3 | __ / 3 |
| C12 | [GroupController](#c12) | F05, F06, F10 | 19 | __ / 19 |
| C13 | [GroupInvitationController](#c13) | F06 | 6 | __ / 6 |
| C14 | [GroupJoinRequestController](#c14) | F06 | 9 | __ / 9 |
| C15 | [GroupMeetingController](#c15) | F09 | 8 | __ / 8 |
| C16 | [GroupProblemController](#c16) | F07 | 6 | __ / 6 |
| C17 | [GroupRecruitmentRoleController](#c17) | F04 | 1 | __ / 1 |
| C18 | [GroupTaskController](#c18) | F08 | 19 | __ / 19 |
| C19 | [ImportController](#c19) | F03 | 7 | __ / 7 |
| C20 | [InstructorGroupBoardController](#c20) | F05 | 2 | __ / 2 |
| C21 | [InstructorProblemController](#c21) | F07 | 2 | __ / 2 |
| C22 | [InstructorSubmissionController](#c22) | F12 | 1 | __ / 1 |
| C23 | [MentorAvailabilityController](#c23) | F09 | 5 | __ / 5 |
| C24 | [MentorMeetingReportController](#c24) | F09 | 2 | __ / 2 |
| C25 | [MilestoneGradeController](#c25) | F12 | 3 | __ / 3 |
| C26 | [MilestoneGradeMatrixController](#c26) | F11 | 6 | __ / 6 |
| C27 | [MilestoneSubmissionController](#c27) | F12 | 3 | __ / 3 |
| C28 | [NotificationController](#c28) | F13 | 4 | __ / 4 |
| C29 | [ProblemController](#c29) | F07 | 2 | __ / 2 |
| C30 | [ProblemCriteriaController](#c30) | F04, F07 | 1 | __ / 1 |
| C31 | [ProblemDomainController](#c31) | F04, F07 | 1 | __ / 1 |
| C32 | [ProblemImportController](#c32) | F03 | 1 | __ / 1 |
| C33 | [ProfileController](#c33) | F01 | 3 | __ / 3 |
| C34 | [StudentAccountImportController](#c34) | F03 | 2 | __ / 2 |
| C35 | [StudentController](#c35) | F04 | 2 | __ / 2 |
| C36 | [StudentGroupGradeController](#c36) | F12 | 1 | __ / 1 |
| C37 | [TaskBoardController](#c37) | F08 | 8 | __ / 8 |

<a id="c01"></a>

## C01. AcademicTermController

Luồng và fixture: **F04** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AcademicTermController.java). Luồng xử lý: [AcademicTermServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/AcademicTermServiceImpl.java).

Mọi tài khoản đã xác thực được đọc. Chỉ trả kỳ OPEN, sắp createdAt giảm dần rồi id giảm dần; mỗi kỳ kèm groupCount theo mã kỳ (không phân biệt hoa/thường), tổng feedback dự kiến và đã SUBMITTED.

### C01-01. listAvailableTerms

- [ ] C01-01: `GET /api/terms/available` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Available academic terms retrieved successfully`; data: `List<AcademicTermResponseDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `code`, `status`, `closedAt`, `closedByEmail`, `groupCount`, `totalExpectedFeedbacks`, `totalSubmittedFeedbacks`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Đọc danh sách kỳ đang mở; trả mảng AcademicTermResponseDto, không phân trang; không tự tạo kỳ.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c01-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AcademicTermController.java:26).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c02"></a>

## C02. AdminFeedbackController

Luồng và fixture: **F15** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminFeedbackController.java). Luồng xử lý: [FeedbackServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/FeedbackServiceImpl.java).

Chỉ ADMIN. Danh sách có danh tính sinh viên và mentor/instructor; khác với feedback received ẩn danh. Export cần mã kỳ tồn tại, trim/uppercase; thiếu kỳ → 400 "Academic term is required", không có kỳ → 404.

### C02-01. listFeedbacks

- [ ] C02-01: `GET /api/admin/feedback` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| page | query | int | Không | `0` |
| size | query | int | Không | `20` |
| term | query | String | Không | — |
| courseCode | query | String | Không | — |
| targetType | query | FeedbackTargetType | Không | — |
| targetId | query | Long | Không | — |
| targetSearch | query | String | Không | — |
| status | query | FeedbackStatus | Không | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Feedbacks retrieved successfully`; data: `PageResponse<AdminFeedbackResponseDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): PageResponse: content/page/number/size/numberOfElements/totalElements/totalPages/hasNext/hasPrevious. Mỗi content item: `id`, `academicTerm`, `group`, `student`, `targetType`, `mentor`, `instructor`, `rating`, `comment`, `status`, `submittedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Lọc đồng thời term, courseCode, targetType, targetId, targetSearch, status; page mặc định 0, size 20 (1–100); createdAt DESC, id DESC. Không chỉ lấy SUBMITTED khi status không truyền.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c02-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminFeedbackController.java:34).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C02-02. exportFeedbacks

- [ ] C02-02: `GET /api/admin/feedback/export.xlsx` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| term | query | String | Không | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; file nhị phân, không JSON. Kiểm MIME/Content-Disposition và nội dung ở phần dưới.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Xuất chỉ feedback SUBMITTED của kỳ, hai sheet Mentor Feedback/Instructor Feedback. 15 cột gồm student/target/email/rating/comment/thời điểm ICT. Sắp mã người nhận, môn, số nhóm, MSSV, submittedAt, id. Filename feedback-{safeTerm}.xlsx; không bọc JSON.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c02-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminFeedbackController.java:65).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c03"></a>

## C03. AdminGroupController

Luồng và fixture: **F05** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminGroupController.java). Luồng xử lý: [GroupServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupServiceImpl.java).

Chỉ ADMIN; status mặc định ACTIVE, ALL bỏ lọc; INACTIVE vẫn có thể truy vấn. Enum không hợp lệ → 400 "Status must be ACTIVE, INACTIVE, or ALL". Phân trang trên id trước khi fetch các collection để không nhân số nhóm.

### C03-01. listGroups

- [ ] C03-01: `GET /api/admin/groups` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| page | query | int | Không | `0` |
| size | query | int | Không | `20` |
| search | query | String | Không | — |
| status | query | String | Không | `ACTIVE` |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Groups retrieved successfully`; data: `PageResponse<GroupSummaryDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): PageResponse: content/page/number/size/numberOfElements/totalElements/totalPages/hasNext/hasPrevious. Mỗi content item: `id`, `term`, `termStatus`, `termClosedAt`, `studentReadOnly`, `courseCode`, `groupNo`, `name`, `projectName`, `leaderName`, `memberCount`, `requiredGpa`, `targetGrade`, `status`, `mentorId`, `mentorAccountId`, `mentorCode`, `mentorName`, `instructorId`, `instructorAccountId`, `instructorCode`, `instructorName`, `isLock`, `selectedProblem`, `recruitmentNeeds`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Lọc search theo tên nhóm/project/course/term; page=0,size=20, giới hạn 1–100. createdAt DESC,id DESC. Trả PageResponse<GroupSummaryDto> gồm kỳ/readOnly, leader, số thành viên, tài khoản và profile mentor/instructor, selectedProblem, recruitmentNeeds đầy đủ nhãn.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c03-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminGroupController.java:34).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c04"></a>

## C04. AdminProblemController

Luồng và fixture: **F07** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java). Luồng xử lý: [ProblemBankServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemBankServiceImpl.java).

Chỉ ADMIN. Domain có thể không truyền khi tạo problem; nếu truyền phải tồn tại và ACTIVE. Code problem là unique theo repository, không tự gán code. DTO enum/validation phải được áp dụng trước service.

### C04-01. createProblemDomain

- [ ] C04-01: `POST /api/admin/problem-domains` — kết quả: NOT RUN.

Body JSON: [CreateProblemDomainRequest](#dto-createproblemdomainrequest); payload chính minh họa trong luồng F07.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Problem domain created successfully`; data: `ProblemDomainDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `code`, `name`, `description`, `macroDomain`, `subDomain`, `typicalExamples`, `primaryDiscipline`, `supportingDisciplines`, `bestSources`, `studentCapabilities`, `potentialOutputs`, `notes`, `status`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tạo domain: code uppercase, từ chối trùng ignore-case (409); name/description và metadata lĩnh vực; macroDomain ưu tiên giá trị truyền, fallback name; subDomain fallback description; metadata blank → null; mặc định ACTIVE.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c04-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:25).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C04-02. updateProblemDomain

- [ ] C04-02: `PATCH /api/admin/problem-domains/{id}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body JSON: [UpdateProblemDomainRequest](#dto-updateproblemdomainrequest); payload chính minh họa trong luồng F07.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Problem domain updated successfully`; data: `ProblemDomainDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `code`, `name`, `description`, `macroDomain`, `subDomain`, `typicalExamples`, `primaryDiscipline`, `supportingDisciplines`, `bestSources`, `studentCapabilities`, `potentialOutputs`, `notes`, `status`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Sửa từng field non-null; code trùng 409; không tự xóa field khi không truyền. Tra id không có → 404.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c04-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:35).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C04-03. createOfficialProblem

- [ ] C04-03: `POST /api/admin/problems` — kết quả: NOT RUN.

Body JSON: [CreateProblemRequest](#dto-createproblemrequest); payload chính minh họa trong luồng F07.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Official problem created successfully`; data: `ProblemDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `code`, `title`, `statement`, `strategicTheme`, `researchArea`, `difficultyLevel`, `expectedOutput`, `ownerLab`, `suggestedCourses`, `driveFolderLink`, `sourceType`, `status`, `domain`, `proposedByGroup`, `proposedByStudent`, `reviewComment`, `reviewedBy`, `reviewedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tạo OFFICIAL; domain optional ACTIVE, title/statement/difficulty từ DTO; code blank → null; status mặc định ACTIVE. Code trùng → 400.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c04-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:46).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C04-04. updateProblem

- [ ] C04-04: `PATCH /api/admin/problems/{id}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body JSON: [UpdateProblemRequest](#dto-updateproblemrequest); payload chính minh họa trong luồng F07.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Problem updated successfully`; data: `ProblemDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `code`, `title`, `statement`, `strategicTheme`, `researchArea`, `difficultyLevel`, `expectedOutput`, `ownerLab`, `suggestedCourses`, `driveFolderLink`, `sourceType`, `status`, `domain`, `proposedByGroup`, `proposedByStudent`, `reviewComment`, `reviewedBy`, `reviewedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

PATCH trường non-null; domainCode blank xóa domain; domain hiện tại không đổi thì không kiểm tra lại ACTIVE; code đổi phải không trùng.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c04-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:56).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C04-05. updateProblemStatus

- [ ] C04-05: `PATCH /api/admin/problems/{id}/status` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body JSON: [UpdateProblemStatusRequest](#dto-updateproblemstatusrequest); payload chính minh họa trong luồng F07.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Problem status updated successfully`; data: `ProblemDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `code`, `title`, `statement`, `strategicTheme`, `researchArea`, `difficultyLevel`, `expectedOutput`, `ownerLab`, `suggestedCourses`, `driveFolderLink`, `sourceType`, `status`, `domain`, `proposedByGroup`, `proposedByStudent`, `reviewComment`, `reviewedBy`, `reviewedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Cập nhật status theo ProblemStatus hợp lệ; đây là thao tác riêng, không tự thực hiện quy trình duyệt proposal.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c04-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:67).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C04-06. reviewProblem

- [ ] C04-06: `PATCH /api/admin/problems/{id}/review` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body JSON: [ReviewProblemRequest](#dto-reviewproblemrequest); payload chính minh họa trong luồng F07.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Proposed problem reviewed successfully`; data: `ProblemDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `code`, `title`, `statement`, `strategicTheme`, `researchArea`, `difficultyLevel`, `expectedOutput`, `ownerLab`, `suggestedCourses`, `driveFolderLink`, `sourceType`, `status`, `domain`, `proposedByGroup`, `proposedByStudent`, `reviewComment`, `reviewedBy`, `reviewedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Chỉ SELF_PROPOSED/PENDING_REVIEW; quyết định APPROVED hoặc REJECTED; REJECTED cần comment không trắng. APPROVED chuyển sourceType=OFFICIAL,status=ACTIVE. REJECTED bỏ selectedProblem nếu nhóm đang chọn proposal đó. Lưu reviewComment, reviewedByAccount, reviewedAt.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c04-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:78).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c05"></a>

## C05. AdminTermController

Luồng và fixture: **F04, F15, F16** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminTermController.java). Luồng xử lý: [AcademicTermServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/AcademicTermServiceImpl.java).

Chỉ ADMIN. Chuẩn hóa term trim/uppercase, bắt buộc, tối đa 30 ký tự. Các thao tác ghi có transaction; close khóa nhóm theo id ổn định rồi khóa kỳ để phối hợp các student write.

### C05-01. listTerms

- [ ] C05-01: `GET /api/admin/terms` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| page | query | int | Không | `0` |
| size | query | int | Không | `10` |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Academic terms retrieved successfully`; data: `PageResponse<AcademicTermResponseDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): PageResponse: content/page/number/size/numberOfElements/totalElements/totalPages/hasNext/hasPrevious. Mỗi content item: `id`, `code`, `status`, `closedAt`, `closedByEmail`, `groupCount`, `totalExpectedFeedbacks`, `totalSubmittedFeedbacks`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Liệt kê tất cả kỳ với count nhóm/feedback, page=0,size=10 (1–100), createdAt DESC,id DESC.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c05-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminTermController.java:35).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C05-02. createTerm

- [ ] C05-02: `POST /api/admin/terms` — kết quả: NOT RUN.

Body JSON: [CreateAcademicTermRequest](#dto-createacademictermrequest); payload chính minh họa trong luồng F04, F15, F16.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Academic term created successfully`; data: `AcademicTermResponseDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `code`, `status`, `closedAt`, `closedByEmail`, `groupCount`, `totalExpectedFeedbacks`, `totalSubmittedFeedbacks`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tạo kỳ OPEN. Nếu cùng code đã OPEN thì trả lại kỳ hiện hữu (idempotent), nếu CLOSED trả409. Nếu kỳ khác đang OPEN, buộc đóng kỳ đó trước. Đua unique/open constraint được chuyển thành409.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c05-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminTermController.java:56).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C05-03. closeTerm

- [ ] C05-03: `PATCH /api/admin/terms/{term}/close` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| term | path | String | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Academic term closed successfully`; data: `AcademicTermResponseDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `code`, `status`, `closedAt`, `closedByEmail`, `groupCount`, `totalExpectedFeedbacks`, `totalSubmittedFeedbacks`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Nếu chưa có AcademicTerm nhưng có group lịch sử cùng mã, đăng ký kỳ rồi đóng. Nếu đã CLOSED trả lại thành công. Khóa nhóm/kỳ; lưu người đóng, thời gian; CANCELED toàn bộ pending invitation/join request trong kỳ; snapshot feedback từng thành viên × mentor/instructor được gán (không trùng); gửi TERM_FEEDBACK_AVAILABLE chỉ khi có feedback mới.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c05-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminTermController.java:65).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C05-04. archiveStudents

- [ ] C05-04: `POST /api/admin/terms/{term}/archive-students` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| term | path | String | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Eligible students archived successfully`; data: `ArchiveTermStudentsResponseDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `termCode`, `archivedStudents`, `skippedPendingFeedbackStudents`, `skippedActiveInOpenTerm`, `alreadyInactiveStudents`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Kỳ phải CLOSED; khóa kỳ và nhóm, chuyển mọi nhóm trong kỳ INACTIVE. Sinh viên còn thuộc kỳ OPEN khác được giữ/đưa lại ACTIVE cùng account. Còn lại chuyển student+account INACTIVE, revoke refresh tokens; bỏ qua khi cả hai đã inactive hoặc thiếu account. Code thực tế KHÔNG chờ feedback; skippedPendingFeedbackStudents luôn0. Đây là điểm khác với mô tả annotation cũ.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c05-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminTermController.java:77).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C05-05. deleteEmptyTerm

- [ ] C05-05: `DELETE /api/admin/terms/{term}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| term | path | String | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Academic term deleted successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Chỉ xóa kỳ không có group và không có feedback; có lịch sử →409. Khóa kỳ, xóa+flush; không xóa dây chuyền dữ liệu học tập.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c05-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminTermController.java:88).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c06"></a>

## C06. AdminUserController

Luồng và fixture: **F02** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java). Luồng xử lý: [AdminUserServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/AdminUserServiceImpl.java).

Chỉ ADMIN theo SecurityConfig. Create/update chuẩn hóa email trim/lowercase, trùng ignore-case →400; change-password theo email có ngoại lệ mô tả riêng bên dưới. Account gắn đúng một profile theo role; ADMIN không được kèm profile; STUDENT/MENTOR/INSTRUCTOR phải có đúng loại và từ chối profile khác loại. Không đổi role qua update.

### C06-01. listUsers

- [ ] C06-01: `GET /api/admin/users` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| page | query | int | Không | `0` |
| size | query | int | Không | `10` |
| search | query | String | Không | — |
| role | query | Role | Không | — |
| status | query | AccountStatus | Không | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Users retrieved successfully`; data: `PageResponse<AdminUserSummaryDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): PageResponse: content/page/number/size/numberOfElements/totalElements/totalPages/hasNext/hasPrevious. Mỗi content item: `id`, `email`, `role`, `status`, `mustChangePassword`, `fullName`, `code`, `createdAt`, `lastLoginAt`, `groupMemberships`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Page0,size10 (1–100), search email/tên/mã profile, role/status; createdAt DESC,id DESC; sinh viên có groupMemberships.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c06-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:38).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C06-02. getUserById

- [ ] C06-02: `GET /api/admin/users/{id}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `User details retrieved successfully`; data: `AdminUserDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `email`, `role`, `status`, `mustChangePassword`, `createdAt`, `updatedAt`, `lastLoginAt`, `studentProfile`, `mentorProfile`, `instructorProfile`, `groupMemberships`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Đọc account+profile+membership bằng account id; thiếu →404.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c06-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:61).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C06-03. createUser

- [ ] C06-03: `POST /api/admin/users` — kết quả: NOT RUN.

Body JSON: [CreateAdminUserRequest](#dto-createadminuserrequest); payload chính minh họa trong luồng F02.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `User created successfully`; data: `AdminUserDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `email`, `role`, `status`, `mustChangePassword`, `createdAt`, `updatedAt`, `lastLoginAt`, `studentProfile`, `mentorProfile`, `instructorProfile`, `groupMemberships`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tạo ACTIVE, BCrypt(initialPassword), mustChangePassword=true; mã student/mentor/instructor phải duy nhất; account/profile trong cùng transaction.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c06-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:68).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C06-04. updateUser

- [ ] C06-04: `PATCH /api/admin/users/{id}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body JSON: [UpdateAdminUserRequest](#dto-updateadminuserrequest); payload chính minh họa trong luồng F02.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `User updated successfully`; data: `AdminUserDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `email`, `role`, `status`, `mustChangePassword`, `createdAt`, `updatedAt`, `lastLoginAt`, `studentProfile`, `mentorProfile`, `instructorProfile`, `groupMemberships`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Cập nhật email,status,mustChangePassword và profile đủ theo role hiện tại; đồng bộ status profile; không cho người gọi vô hiệu hóa chính mình. Khi chuyển sang INACTIVE revoke refresh tokens.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c06-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:77).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C06-05. deleteUser

- [ ] C06-05: `DELETE /api/admin/users/{id}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `User deleted successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Soft-delete account và profile thành INACTIVE, revoke refresh tokens; không xóa lịch sử; cấm tự disable/delete.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c06-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:88).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C06-06. resetPassword

- [ ] C06-06: `POST /api/admin/users/{id}/reset-password` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body JSON: [ResetUserPasswordRequest](#dto-resetuserpasswordrequest); payload chính minh họa trong luồng F02.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Password reset successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Theo account id: BCrypt mật khẩu mới, mustChangePassword=true, revoke tất cả refresh tokens.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c06-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:98).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C06-07. changePasswordByEmail

- [ ] C06-07: `POST /api/admin/users/change-password` — kết quả: NOT RUN.

Body JSON: [AdminChangePasswordRequest](#dto-adminchangepasswordrequest); payload chính minh họa trong luồng F02.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Password changed successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tra `findByEmailIgnoreCase(request.email())` trực tiếp, không trim/lowercase qua helper của create/update; DTO vẫn validate email. BCrypt mật khẩu mới, mustChangePassword=false, revoke tất cả refresh tokens.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c06-m7); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:108).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c07"></a>

## C07. AuthController

Luồng và fixture: **F01** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java). Luồng xử lý: [CustomUserDetailsService](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/CustomUserDetailsService.java), [GoogleAuthServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GoogleAuthServiceImpl.java), [GoogleTokenVerifierImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GoogleTokenVerifierImpl.java), [JwtServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/JwtServiceImpl.java), [RefreshTokenServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/RefreshTokenServiceImpl.java), [TokenBlacklistServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/TokenBlacklistServiceImpl.java).

Login/google/refresh public; me/logout cần JWT. Login thường dùng authentication manager/BCrypt; mọi exception khi authenticate được trả401 "Invalid email or password". TokenResponse gồm accessToken,refreshToken,tokenType=Bearer,expiresIn (giây). JWT secret Base64; sub=email,role,iat,exp. Refresh raw UUID, chỉ lưu SHA-256/Base64; tạo token mới xóa token cũ cùng account.

### C07-01. login

- [ ] C07-01: `POST /api/auth/login` — kết quả: NOT RUN.

Body JSON: [LoginRequest](#dto-loginrequest); payload chính minh họa trong luồng F01.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Login successful`; data: `TokenResponse`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `accessToken`, `refreshToken`, `tokenType`, `expiresIn`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Email/password hợp lệ, account được phép authenticate; cập nhật lastLoginAt rồi phát cặp token. Không trả profile trong TokenResponse.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c07-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java:58).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C07-02. loginWithGoogle

- [ ] C07-02: `POST /api/auth/google` — kết quả: NOT RUN.

Body JSON: [GoogleLoginRequest](#dto-googleloginrequest); payload chính minh họa trong luồng F01.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Google login successful`; data: `TokenResponse`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `accessToken`, `refreshToken`, `tokenType`, `expiresIn`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Gửi ID token tới Google token-info endpoint; verifier Java kiểm client-id đã cấu hình, audience khớp, email_verified=true và email không rỗng, sau đó trim/lowercase email. Java không có nhánh tự kiểm issuer/exp riêng trong verifier; không suy diễn thêm từ tên “verify”. Chỉ account ACTIVE đã đăng ký, không tự đăng ký tài khoản; cập nhật lastLoginAt, phát token.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c07-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java:90).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C07-03. refresh

- [ ] C07-03: `POST /api/auth/refresh` — kết quả: NOT RUN.

Body JSON: [RefreshTokenRequest](#dto-refreshtokenrequest); payload chính minh họa trong luồng F01.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Token refreshed successfully`; data: `TokenResponse`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `accessToken`, `refreshToken`, `tokenType`, `expiresIn`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tra hash refresh, kiểm tồn tại/hết hạn/revoked; tạo JWT mới và thay refresh token toàn account. Controller Java hiện không gọi lại kiểm ACTIVE trong nhánh này; cần phân biệt hành vi thực tế với cải tiến bảo mật mong muốn.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c07-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java:97).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C07-04. me

- [ ] C07-04: `GET /api/auth/me` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Success`; data: `UserInfoResponse`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `email`, `role`, `status`, `mustChangePassword`, `instructorProfile`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tra email principal; trả id,email,role,status,mustChangePassword và instructorProfile chỉ khi role INSTRUCTOR và có profile.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c07-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java:117).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C07-05. logout

- [ ] C07-05: `POST /api/auth/logout` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Logged out successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Revoke tất cả refresh của account nếu tìm được; blacklist bearer token đến hạn; clear SecurityContext; trả200 "Logged out successfully", data=null.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c07-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java:152).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c08"></a>

## C08. BackupController

Luồng và fixture: **F17** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java). Luồng xử lý: [BackupServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/BackupServiceImpl.java).

Chỉ ADMIN. Backup/restore là thao tác thật trên PostgreSQL; không chạy thử restore vào DB người dùng. Job QUEUED→RUNNING→SUCCEEDED/FAILED; ngăn job/restore chồng nhau trong instance Java. Startup đánh dấu job cũ đang chạy/đợi FAILED. Scheduler fixedDelay 60 giây, cron Spring 6 trường và timezone.

### C08-01. createBackup

- [ ] C08-01: `POST /api/admin/backups` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Backup job queued successfully`; data: `BackupJobDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `triggerType`, `status`, `fileName`, `fileSizeBytes`, `errorMessage`, `requestedByAccountId`, `requestedByEmail`, `startedAt`, `finishedAt`, `createdAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tạo MANUAL job và xếp hàng background, HTTP200 (không phải202). Chụp backupDir/retention từ lúc queue. Đang restore/job →409. pg_dump tạo custom-format dump; lưu filename/path/size, finishedAt hoặc lỗi giới hạn4000 ký tự.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c08-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:44).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C08-02. restoreBackup

- [ ] C08-02: `POST /api/admin/backups/restore` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| file | multipart | MultipartFile | Có | — |
| confirmation | multipart | String | Có | — |

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Database restored successfully`; data: `RestoreBackupResponseDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `fileName`, `fileSizeBytes`, `restoredAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Multipart file + confirmation=RESTORE_DATABASE; không rỗng, đuôi .dump; ngăn job/restore đang chạy. Lưu upload, chạy pg_restore, đánh dấu job active trong dump FAILED; trả filename,size,restoredAt. Lỗi xử lý →400 có tiền tố Database restore failed; luôn giải phóng gate.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c08-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:50).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C08-03. listBackups

- [ ] C08-03: `GET /api/admin/backups` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| status | query | BackupJobStatus | Không | — |
| page | query | int | Không | `0` |
| size | query | int | Không | `10` |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Backup jobs retrieved successfully`; data: `PageResponse<BackupJobDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): PageResponse: content/page/number/size/numberOfElements/totalElements/totalPages/hasNext/hasPrevious. Mỗi content item: `id`, `triggerType`, `status`, `fileName`, `fileSizeBytes`, `errorMessage`, `requestedByAccountId`, `requestedByEmail`, `startedAt`, `finishedAt`, `createdAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Page0,size10 (1–100), status optional enum; createdAt DESC,id DESC; trả trạng thái, thời gian, người yêu cầu, file metadata.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c08-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:60).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C08-04. getSchedule

- [ ] C08-04: `GET /api/admin/backups/schedule` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Backup schedule retrieved successfully`; data: `BackupScheduleSettingsDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `enabled`, `cronExpression`, `timezone`, `backupDir`, `retentionDays`, `lastTriggeredAt`, `nextRunAt`, `updatedByAccountId`, `updatedByEmail`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Khởi tạo settings mặc định khi chưa có; DTO dùng các field `enabled`, `cronExpression`, `timezone`, `backupDir`, `retentionDays`, `lastTriggeredAt`, `nextRunAt`, `updatedByAccountId`, `updatedByEmail`, `updatedAt`. Mặc định disabled, 02:00 Asia/Ho_Chi_Minh, giữ14ngày; GET có thể cập nhật backupDir theo cấu hình.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c08-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:80).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C08-05. updateSchedule

- [ ] C08-05: `PUT /api/admin/backups/schedule` — kết quả: NOT RUN.

Body JSON: [UpdateBackupScheduleRequest](#dto-updatebackupschedulerequest); payload chính minh họa trong luồng F17.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Backup schedule updated successfully`; data: `BackupScheduleSettingsDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `enabled`, `cronExpression`, `timezone`, `backupDir`, `retentionDays`, `lastTriggeredAt`, `nextRunAt`, `updatedByAccountId`, `updatedByEmail`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Validate enabled,cron,timezone,retention1–3650; retention null→14; directory do config quản lý. enabled=false→nextRun=null; bật→tính lần tiếp theo. Không tạo job ngay.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c08-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:87).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C08-06. getBackup

- [ ] C08-06: `GET /api/admin/backups/{jobId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| jobId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Backup job retrieved successfully`; data: `BackupJobDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `triggerType`, `status`, `fileName`, `fileSizeBytes`, `errorMessage`, `requestedByAccountId`, `requestedByEmail`, `startedAt`, `finishedAt`, `createdAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tra job theo id; thiếu404.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c08-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:96).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C08-07. downloadBackup

- [ ] C08-07: `GET /api/admin/backups/{jobId}/download` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| jobId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; file nhị phân, không JSON. Kiểm MIME/Content-Disposition và nội dung ở phần dưới.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Chỉ SUCCEEDED; metadata/file phải có, file regular tồn tại. Trả application/octet-stream, Content-Disposition attachment; filename="{job.fileName}", không JSON.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c08-m7); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:103).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c09"></a>

## C09. CourseMilestoneController

Luồng và fixture: **F10** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java). Luồng xử lý: [CourseMilestoneServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/CourseMilestoneServiceImpl.java).

Hai prefix /api/course-milestones và /api/instructor/milestones trỏ cùng controller, nhưng prefix instructor bị SecurityConfig giới hạn INSTRUCTOR; prefix cũ cho authenticated rồi service phân quyền. Milestone thuộc instructor + term + course; maxScore mặc định10, >0; weight0–100, tổng ACTIVE≤100; position≥0. deadlineAt nhận alias dueDate.

### C09-01. createCourseMilestone

- [ ] C09-01.1: `POST /api/course-milestones` — kết quả: NOT RUN.
- [ ] C09-01.2: `POST /api/instructor/milestones` — kết quả: NOT RUN.

Body JSON: [CreateCourseMilestoneRequest](#dto-createcoursemilestonerequest); payload chính minh họa trong luồng F10.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Course milestone created successfully`; data: `CourseMilestoneDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `term`, `courseCode`, `title`, `description`, `weight`, `deadlineAt`, `maxScore`, `position`, `status`, `instructorId`, `instructorName`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

INSTRUCTOR active, có group được giao trong term/course; kỳ tồn tại OPEN; title unique ignore-case trong scope; deadline bắt buộc; tự position=max+1; typeTIMELINE,statusACTIVE; gửi TIMELINE_ITEM_CREATED cho leader các nhóm đúng scope.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c09-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:29).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C09-02. getCourseMilestones

- [ ] C09-02.1: `GET /api/course-milestones` — kết quả: NOT RUN.
- [ ] C09-02.2: `GET /api/instructor/milestones` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| term | query | String | Không | — |
| courseCode | query | String | Không | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Course milestones retrieved successfully`; data: `List<CourseMilestoneDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `term`, `courseCode`, `title`, `description`, `weight`, `deadlineAt`, `maxScore`, `position`, `status`, `instructorId`, `instructorName`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Instructor xem timeline của mình với filter tùy chọn, kể cả archived. Student không filter xem hợp nhất timeline nhóm mình; nếu có filter phải đủ cả term/course, phải thuộc group scope đó; list không chứa ARCHIVED/INACTIVE. Role khác403.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c09-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:41).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C09-03. getCourseMilestoneById

- [ ] C09-03.1: `GET /api/course-milestones/{id}` — kết quả: NOT RUN.
- [ ] C09-03.2: `GET /api/instructor/milestones/{id}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Course milestone retrieved successfully`; data: `CourseMilestoneDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `term`, `courseCode`, `title`, `description`, `weight`, `deadlineAt`, `maxScore`, `position`, `status`, `instructorId`, `instructorName`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Instructor phải sở hữu milestone; student thuộc group cùng scope/instructor, không ARCHIVED. Lưu ý kiểm detail Java chỉ loại ARCHIVED, khác list loại cả INACTIVE.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c09-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:54).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C09-04. updateCourseMilestone

- [ ] C09-04.1: `PUT /api/course-milestones/{id}` — kết quả: NOT RUN.
- [ ] C09-04.2: `PATCH /api/course-milestones/{id}` — kết quả: NOT RUN.
- [ ] C09-04.3: `PUT /api/instructor/milestones/{id}` — kết quả: NOT RUN.
- [ ] C09-04.4: `PATCH /api/instructor/milestones/{id}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body JSON: [UpdateCourseMilestoneRequest](#dto-updatecoursemilestonerequest); payload chính minh họa trong luồng F10.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Course milestone updated successfully`; data: `CourseMilestoneDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `term`, `courseCode`, `title`, `description`, `weight`, `deadlineAt`, `maxScore`, `position`, `status`, `instructorId`, `instructorName`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

PUT/PATCH cùng nghiệp vụ: cần title/deadline, kiểm trùng và maxScore không thấp hơn điểm đã có; tổng weightACTIVE≤100; đổi deadline cập nhật late của submissions; chỉ notify TIMELINE_ITEM_UPDATED nếu trạng thái mới ACTIVE. Description và weight được gán kể cả null; position/status/maxScore null giữ cũ. Update/delete không gọi requireOpenAcademicTerm như create; không áp thêm chặn kỳ CLOSED nếu đang mô tả bản Java.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c09-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:66).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C09-05. deleteCourseMilestone

- [ ] C09-05.1: `DELETE /api/course-milestones/{id}` — kết quả: NOT RUN.
- [ ] C09-05.2: `DELETE /api/instructor/milestones/{id}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Course milestone deleted successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Chỉ instructor sở hữu; archive mềm, không xóa grade/submission.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c09-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:79).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C09-06. deprecatedOutcomes

- [ ] C09-06.1: `GET /api/course-milestones/{milestoneId}/outcomes` — kết quả: NOT RUN.
- [ ] C09-06.2: `POST /api/course-milestones/{milestoneId}/outcomes` — kết quả: NOT RUN.
- [ ] C09-06.3: `GET /api/instructor/milestones/{milestoneId}/outcomes` — kết quả: NOT RUN.
- [ ] C09-06.4: `POST /api/instructor/milestones/{milestoneId}/outcomes` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **410**; envelope code **410**; message `Outcome types were removed; use a generically named timeline milestone`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

GET/POST outcomes là route chủ động deprecated: trả404 với envelope của Java, không chạy MilestoneOutcomeService. Không được dựng lại thành success.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c09-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:91).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c10"></a>

## C10. DashboardController

Luồng và fixture: **F14** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java). Luồng xử lý: [DashboardServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/DashboardServiceImpl.java).

Phân quyền theo prefix admin/mentor/instructor/student; TV showcase vẫn cần đăng nhập theo anyRequest. Đây là dữ liệu tổng hợp thật, không dùng số mẫu. Tiến độ=round(DONE/activeTasks*100), zero task→0; activeTask loại archived; overdue là dueAt<now và chưaDONE. Phân biệt stats loại archived và taskStatusCounts Java đếm nguồn allTasks. nextDueAt của group progress có thể nằm trong quá khứ; TV projects chỉ lấy deadline >= now. Completeness của milestone dashboard dùng tất cả membership có student, không lọc student/account ACTIVE mặc dù biến có tên activeMemberIds; đây không phải cùng cách lọc với grade matrix.

### C10-01. getAdminGroups

- [ ] C10-01: `GET /api/dashboard/admin/groups` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Admin dashboard groups retrieved successfully`; data: `List<DashboardGroupProgressDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `groupId`, `term`, `courseCode`, `groupNo`, `groupName`, `projectName`, `status`, `mentorId`, `mentorCode`, `mentorName`, `memberCount`, `totalTasks`, `completedTasks`, `inProgressTasks`, `overdueTasks`, `progressPercent`, `nextDueAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

ADMIN: tiến độ từng nhóm, mentor/member counts, tasks done/in-progress/overdue,nextDueAt,updatedAt.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:38).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C10-02. getAdminProjects

- [ ] C10-02: `GET /api/dashboard/admin/projects` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Admin dashboard projects retrieved successfully`; data: `List<DashboardProjectDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `groupId`, `term`, `courseCode`, `groupNo`, `groupName`, `projectName`, `ideaDescription`, `researchDomain`, `groupStatus`, `progressPercent`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

ADMIN: project metadata và tiến độ từng nhóm; không nhất thiết chỉ nhóm có projectName.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:44).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C10-03. getAdminMentors

- [ ] C10-03: `GET /api/dashboard/admin/mentors` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Admin dashboard mentors retrieved successfully`; data: `List<DashboardMentorDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `mentorId`, `mentorCode`, `mentorName`, `email`, `assignedGroupCount`, `scheduledMeetingCount`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

ADMIN: danh sách mentor, số nhóm/cuộc họp theo mapMentor; không rút gọn mất fields DTO.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:50).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C10-04. getAdminTimeline

- [ ] C10-04: `GET /api/dashboard/admin/timeline` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Admin dashboard timeline retrieved successfully`; data: `List<DashboardMeetingDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `groupId`, `groupName`, `groupNo`, `projectName`, `selectedProblemId`, `selectedProblemTitle`, `mentorId`, `mentorCode`, `mentorName`, `startAt`, `endAt`, `status`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

ADMIN: timeline meeting startAt ASC, dữ liệu từ meeting và fallback slot nếu trường thời gian chưa có.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:56).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C10-05. getAdminExecutionStatus

- [ ] C10-05: `GET /api/dashboard/admin/execution-status` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Admin dashboard execution status retrieved successfully`; data: `DashboardExecutionStatusDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `stats`, `groupStatusCounts`, `taskStatusCounts`, `groups`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

ADMIN: stats toàn hệ thống + groupStatusCounts + taskStatusCounts theo enum + tiến độ từng nhóm. Giữ đúng cách đếm task archived của từng trường.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:62).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C10-06. getAdminOverview

- [ ] C10-06: `GET /api/dashboard/admin/overview` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| term | query | String | Không | — |
| courseCode | query | String | Không | — |
| limit | query | int | Không | `5` |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Admin dashboard overview retrieved successfully`; data: `AdminDashboardOverviewDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `totalActiveStudents`, `totalActiveMentors`, `topProblems`, `topDomains`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

ADMIN: active student/mentor totals, top problem/domain đã được nhóm chọn; term/course optional, limit1–50. Tie-break theo repository.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:68).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C10-07. getTvShowcaseProjects

- [ ] C10-07: `GET /api/dashboard/tv-showcase/projects` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| term | query | String | Không | — |
| courseCode | query | String | Không | — |
| page | query | int | Không | `0` |
| size | query | int | Không | `20` |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `TV showcase projects retrieved successfully`; data: `TvShowcaseDataDto<TvShowcaseDataDto.Project>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Xem DTO/contract liên kết bên dưới; không suy đoán field.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

TV projects: group ACTIVE thuộc kỳ OPEN mới nhất, lọc term/course; không có kỳ trả trang rỗng. projectName ưu tiên tên nhập, rồi selectedProblem.title, rồi group.name; chỉ lấy tên cuối cùng không trắng. Sắp updatedAt DESC, id DESC trước phân trang.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m7); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:83).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C10-08. getTvShowcaseRecruitments

- [ ] C10-08: `GET /api/dashboard/tv-showcase/recruitments` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| term | query | String | Không | — |
| courseCode | query | String | Không | — |
| page | query | int | Không | `0` |
| size | query | int | Không | `20` |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `TV showcase recruitments retrieved successfully`; data: `TvShowcaseDataDto<TvShowcaseDataDto.Recruitment>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Xem DTO/contract liên kết bên dưới; không suy đoán field.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

TV recruitments: cùng scope TV, chỉ nhóm có recruitmentNeeds; code Java không tự loại nhóm locked. Mỗi nhu cầu gồm role, nhãn VI/EN, quantity; có totalOpenings. Trang kèm refreshedAt và activeTermCode, sắp updatedAt DESC, id DESC.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m8); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:95).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C10-09. getMentorGroups

- [ ] C10-09: `GET /api/dashboard/mentor/groups` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Mentor dashboard groups retrieved successfully`; data: `List<DashboardGroupProgressDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `groupId`, `term`, `courseCode`, `groupNo`, `groupName`, `projectName`, `status`, `mentorId`, `mentorCode`, `mentorName`, `memberCount`, `totalTasks`, `completedTasks`, `inProgressTasks`, `overdueTasks`, `progressPercent`, `nextDueAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

MENTOR: chỉ nhóm gán mentor hiện tại, cùng công thức tiến độ.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m9); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:107).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C10-10. getMentorMeetings

- [ ] C10-10: `GET /api/dashboard/mentor/meetings` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| status | query | DashboardMeetingStatusFilter | Không | `ALL` |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Mentor dashboard meetings retrieved successfully`; data: `List<DashboardMeetingDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `groupId`, `groupName`, `groupNo`, `projectName`, `selectedProblemId`, `selectedProblemTitle`, `mentorId`, `mentorCode`, `mentorName`, `startAt`, `endAt`, `status`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

MENTOR: chỉ meeting do mentor đó và group vẫn gán mentor đó; status enum optional/ALL; startAt ASC.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m10); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:113).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C10-11. getInstructorMilestones

- [ ] C10-11: `GET /api/dashboard/instructor/milestones` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| term | query | String | Không | — |
| courseCode | query | String | Không | — |
| groupId | query | Long | Không | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Instructor dashboard milestones retrieved successfully`; data: `List<DashboardMilestoneStatusDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `milestoneId`, `milestoneTitle`, `groupId`, `groupName`, `groupNo`, `deadlineAt`, `submitted`, `submittedAt`, `late`, `submissionStatus`, `graded`, `score`, `maxScoreSnapshot`, `contributionsComplete`, `gradeComplete`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

INSTRUCTOR: milestone status đúng scope instructor/term/course/group; groupId không thuộc scope→404; bao gồm submitted/late/graded/contribution completeness.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m11); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:121).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C10-12. getStudentGroups

- [ ] C10-12: `GET /api/dashboard/student/groups` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Student dashboard groups retrieved successfully`; data: `List<DashboardGroupProgressDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `groupId`, `term`, `courseCode`, `groupNo`, `groupName`, `projectName`, `status`, `mentorId`, `mentorCode`, `mentorName`, `memberCount`, `totalTasks`, `completedTasks`, `inProgressTasks`, `overdueTasks`, `progressPercent`, `nextDueAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

STUDENT: chỉ các nhóm người gọi là thành viên; tiến độ theo task nhóm.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m12); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:133).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C10-13. getStudentProgress

- [ ] C10-13: `GET /api/dashboard/student/progress` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Student dashboard progress retrieved successfully`; data: `DashboardStudentProgressDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `stats`, `groups`, `checkpoints`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

STUDENT: stats các nhóm của mình, mentor distinct, checkpoints task có dueAt và không archived; kèm overdue/status/priority.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m13); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:139).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C10-14. getStudentProjects

- [ ] C10-14: `GET /api/dashboard/student/projects` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Student dashboard projects retrieved successfully`; data: `List<DashboardProjectDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `groupId`, `term`, `courseCode`, `groupNo`, `groupName`, `projectName`, `ideaDescription`, `researchDomain`, `groupStatus`, `progressPercent`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

STUDENT: projects của các nhóm mình; dùng chung map project/tiến độ.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m14); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:145).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C10-15. getStudentMilestones

- [ ] C10-15: `GET /api/dashboard/student/milestones` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | query | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Student dashboard milestones retrieved successfully`; data: `List<DashboardMilestoneStatusDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `milestoneId`, `milestoneTitle`, `groupId`, `groupName`, `groupNo`, `deadlineAt`, `submitted`, `submittedAt`, `late`, `submissionStatus`, `graded`, `score`, `maxScoreSnapshot`, `contributionsComplete`, `gradeComplete`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

STUDENT: groupId bắt buộc, phải membership; rows timeline đúng instructor/term/course; không instructor→mảng rỗng.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c10-m15); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:151).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c11"></a>

## C11. FeedbackController

Luồng và fixture: **F15** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/FeedbackController.java). Luồng xử lý: [FeedbackServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/FeedbackServiceImpl.java).

GET me/PUT chỉ STUDENT; GET received chỉ MENTOR/INSTRUCTOR. Bản ghi feedback được snapshot khi đóng kỳ; không tự tạo khi sinh viên submit. DTO rating bắt buộc1–5, giới hạn comment theo DTO; entity @Version cho concurrency.

### C11-01. getOwnFeedbacks

- [ ] C11-01: `GET /api/feedback/me` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| term | query | String | Không | — |
| status | query | FeedbackStatus | Không | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Feedbacks retrieved successfully`; data: `List<TermFeedbackResponseDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `academicTermId`, `academicTermCode`, `academicTermStatus`, `groupId`, `groupName`, `studentId`, `studentName`, `studentCode`, `targetType`, `mentorId`, `mentorName`, `instructorId`, `instructorName`, `rating`, `comment`, `status`, `submittedAt`, `version`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Lấy feedback của student hiện tại, filter term (chuẩn hóa) và status; không lấy của người khác.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c11-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/FeedbackController.java:28).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C11-02. submitOrUpdateFeedback

- [ ] C11-02: `PUT /api/feedback/{id}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body JSON: [SubmitFeedbackRequest](#dto-submitfeedbackrequest); payload chính minh họa trong luồng F15.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Feedback submitted successfully`; data: `TermFeedbackResponseDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `academicTermId`, `academicTermCode`, `academicTermStatus`, `groupId`, `groupName`, `studentId`, `studentName`, `studentCode`, `targetType`, `mentorId`, `mentorName`, `instructorId`, `instructorName`, `rating`, `comment`, `status`, `submittedAt`, `version`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tra record→kiểm sở hữu→kỳ không OPEN; cập nhật rating,comment trim/blank→null,statusSUBMITTED; submittedAt chỉ đặt lần đầu; optimistic version do ORM. Cho sửa lại feedback đã gửi.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c11-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/FeedbackController.java:42).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C11-03. getReceivedFeedbacks

- [ ] C11-03: `GET /api/feedback/received` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| term | query | String | Không | — |
| courseCode | query | String | Không | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Feedbacks retrieved successfully`; data: `FeedbackReceivedSummaryDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `targetId`, `targetCode`, `targetName`, `targetType`, `term`, `courseCode`, `totalCount`, `averageRating`, `ratingDistribution`, `entries`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Chỉ feedback SUBMITTED gửi đến profile người gọi, filter kỳ/môn; average làm tròn2 số theo Math.round, distribution đủ1–5 kể cả0; entries không chứa tên/MSSV/email sinh viên.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c11-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/FeedbackController.java:54).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c12"></a>

## C12. GroupController

Luồng và fixture: **F05, F06, F10** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java). Luồng xử lý: [GroupServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupServiceImpl.java), [CourseMilestoneServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/CourseMilestoneServiceImpl.java), [StudentTermWriteGuard](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/StudentTermWriteGuard.java), [GroupMembershipLockGuard](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupMembershipLockGuard.java).

Quyền theo từng route và service, không phải một quyền chung cho controller. Create/discover/student-me/lock: STUDENT; mentor-me: MENTOR; instructor-me: INSTRUCTOR; gán/gỡ người phụ trách: ADMIN. Sửa thông tin/criteria/member/leader cho leader hoặc ADMIN. Student ghi cần kỳ chưa đóng; ADMIN có nhánh bypass. Khóa thành viên (isLock) và kỳ CLOSED là hai điều kiện khác nhau.

### C12-01. getGroups

- [ ] C12-01: `GET /api/groups` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| search | query | String | Không | — |
| status | query | String | Không | — |
| neededRole | query | String | Không | — |
| roleCategory | query | String | Không | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Groups retrieved successfully`; data: `List<GroupSummaryDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `term`, `termStatus`, `termClosedAt`, `studentReadOnly`, `courseCode`, `groupNo`, `name`, `projectName`, `leaderName`, `memberCount`, `requiredGpa`, `targetGrade`, `status`, `mentorId`, `mentorAccountId`, `mentorCode`, `mentorName`, `instructorId`, `instructorAccountId`, `instructorCode`, `instructorName`, `isLock`, `selectedProblem`, `recruitmentNeeds`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Lọc search/status/neededRole/roleCategory; giá trị enum sai trả 400. Recruitment filter bỏ nhu cầu nhóm locked. Sau filter chỉ giữ nhóm không có academicTerm liên kết hoặc academicTerm OPEN; loại nhóm kỳ CLOSED. Sắp createdAt DESC, id DESC.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:34).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-02. discoverGroups

- [ ] C12-02: `GET /api/groups/discover` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| page | query | int | Không | `0` |
| size | query | int | Không | `12` |
| name | query | String | Không | — |
| studentGpa | query | BigDecimal | Không | — |
| neededRole | query | String | Không | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Discover groups retrieved successfully`; data: `PageResponse<GroupSummaryDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): PageResponse: content/page/number/size/numberOfElements/totalElements/totalPages/hasNext/hasPrevious. Mỗi content item: `id`, `term`, `termStatus`, `termClosedAt`, `studentReadOnly`, `courseCode`, `groupNo`, `name`, `projectName`, `leaderName`, `memberCount`, `requiredGpa`, `targetGrade`, `status`, `mentorId`, `mentorAccountId`, `mentorCode`, `mentorName`, `instructorId`, `instructorAccountId`, `instructorCode`, `instructorName`, `isLock`, `selectedProblem`, `recruitmentNeeds`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Student tìm group ACTIVE trong kỳ OPEN, loại nhóm mình đã tham gia; lọc neededRole chỉ nhận nhóm unlocked. GPA 0–4 và requiredGpa≤GPA hoặc null. Page 0, size 12.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:46).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-03. getGroupDetail

- [ ] C12-03: `GET /api/groups/{id}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Group details retrieved successfully`; data: `GroupDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `term`, `termStatus`, `termClosedAt`, `studentReadOnly`, `courseCode`, `groupNo`, `name`, `projectName`, `ideaDescription`, `researchDomain`, `leader`, `requiredGpa`, `targetGrade`, `status`, `mentor`, `mentorAccountId`, `instructorId`, `instructorAccountId`, `instructorCode`, `instructorName`, `isLock`, `members`, `selectedProblem`, `recruitmentNeeds`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Đọc group detail cùng leader/members/mentor/instructor, selectedProblem, recruitmentNeeds có category/nhãn VI/EN, trạng thái kỳ và studentReadOnly; thiếu group trả 404.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:78).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-04. updateGroupCriteria

- [ ] C12-04: `PATCH /api/groups/{id}/criteria` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body JSON: [UpdateGroupCriteriaRequest](#dto-updategroupcriteriarequest); payload chính minh họa trong luồng F05, F06, F10.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Group criteria updated successfully`; data: `GroupDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `term`, `termStatus`, `termClosedAt`, `studentReadOnly`, `courseCode`, `groupNo`, `name`, `projectName`, `ideaDescription`, `researchDomain`, `leader`, `requiredGpa`, `targetGrade`, `status`, `mentor`, `mentorAccountId`, `instructorId`, `instructorAccountId`, `instructorCode`, `instructorName`, `isLock`, `members`, `selectedProblem`, `recruitmentNeeds`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Khóa group, leader/admin; ít nhất một field GPA/targetGrade/recruitmentNeeds. GPA 0–4, target 0–10; role đúng enum, không trùng, quantity 1–6 mỗi role và tổng≤6. Thay collection khi truyền recruitmentNeeds.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:87).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-05. getMyAssignedGroups

- [ ] C12-05: `GET /api/groups/mentor/me` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Assigned groups retrieved successfully`; data: `List<GroupSummaryDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `term`, `termStatus`, `termClosedAt`, `studentReadOnly`, `courseCode`, `groupNo`, `name`, `projectName`, `leaderName`, `memberCount`, `requiredGpa`, `targetGrade`, `status`, `mentorId`, `mentorAccountId`, `mentorCode`, `mentorName`, `instructorId`, `instructorAccountId`, `instructorCode`, `instructorName`, `isLock`, `selectedProblem`, `recruitmentNeeds`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Các nhóm được gán mentor người gọi, mới nhất trước.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:99).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-06. getMyStudentGroup

- [ ] C12-06: `GET /api/groups/student/me` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Student group retrieved successfully`; data: `List<GroupSummaryDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `term`, `termStatus`, `termClosedAt`, `studentReadOnly`, `courseCode`, `groupNo`, `name`, `projectName`, `leaderName`, `memberCount`, `requiredGpa`, `targetGrade`, `status`, `mentorId`, `mentorAccountId`, `mentorCode`, `mentorName`, `instructorId`, `instructorAccountId`, `instructorCode`, `instructorName`, `isLock`, `selectedProblem`, `recruitmentNeeds`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Các nhóm có membership của student người gọi, mới nhất trước.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:107).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-07. createGroup

- [ ] C12-07: `POST /api/groups` — kết quả: NOT RUN.

Body JSON: [CreateGroupRequest](#dto-creategrouprequest); payload chính minh họa trong luồng F05, F06, F10.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Group created successfully`; data: `GroupDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `term`, `termStatus`, `termClosedAt`, `studentReadOnly`, `courseCode`, `groupNo`, `name`, `projectName`, `ideaDescription`, `researchDomain`, `leader`, `requiredGpa`, `targetGrade`, `status`, `mentor`, `mentorAccountId`, `instructorId`, `instructorAccountId`, `instructorCode`, `instructorName`, `isLock`, `members`, `selectedProblem`, `recruitmentNeeds`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Kỳ OPEN tồn tại; chuẩn hóa term/course/name; tên unique trong kỳ; sinh viên chưa có nhóm cùng kỳ/môn. groupNo là số nguyên dương nhỏ nhất chưa dùng, parse "01" như 1. Tạo group, board Default và membership LEADER trong một transaction.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m7); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:115).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-08. updateGroup

- [ ] C12-08: `PATCH /api/groups/{id}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body JSON: [UpdateGroupRequest](#dto-updategrouprequest); payload chính minh họa trong luồng F05, F06, F10.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Group updated successfully`; data: `GroupDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `term`, `termStatus`, `termClosedAt`, `studentReadOnly`, `courseCode`, `groupNo`, `name`, `projectName`, `ideaDescription`, `researchDomain`, `leader`, `requiredGpa`, `targetGrade`, `status`, `mentor`, `mentorAccountId`, `instructorId`, `instructorAccountId`, `instructorCode`, `instructorName`, `isLock`, `members`, `selectedProblem`, `recruitmentNeeds`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Leader/admin, khóa group; sửa field non-null; kiểm tên trùng và GPA/target/recruitment như criteria. Không tự xóa field không truyền.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m8); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:126).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-09. removeMember

- [ ] C12-09: `DELETE /api/groups/{groupId}/members/{studentId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| studentId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Member removed successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Group unlocked; leader/admin không được xóa leader. Xóa assignment task active và ghi activity system; xóa membership, nhóm rỗng→INACTIVE/leader=null/unlocked; notify người bị xóa.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m9); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:138).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-10. leaveGroup

- [ ] C12-10: `DELETE /api/groups/{groupId}/members/me` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Left group successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Student thành viên rời nhóm unlocked, kỳ writable. Leader chỉ rời được khi là thành viên cuối cùng; còn người khác trả 400. Dọn task assignments, deactivate nhóm rỗng; notify leader khi thành viên thường rời.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m10); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:149).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-11. leaveGroupPost

- [ ] C12-11: `POST /api/groups/{groupId}/leave` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Left group successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Alias POST cho leave, cùng quyền và transaction/side effects như DELETE members/me.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m11); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:159).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-12. transferLeader

- [ ] C12-12: `PATCH /api/groups/{groupId}/leader` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body JSON: [TransferLeaderRequest](#dto-transferleaderrequest); payload chính minh họa trong luồng F05, F06, F10.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Leadership transferred successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Leader/admin; người nhận phải là thành viên; nhánh student cần kỳ writable. Khóa hàng group trong giao dịch nhưng KHÔNG kiểm cờ isLock bằng requireUnlocked. Chuyển lại chính leader hiện tại là no-op. Đổi memberRole cũ/mới, leader của group; notify cả leader cũ/mới.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m12); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:169).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-13. assignInstructor

- [ ] C12-13: `PATCH /api/groups/{groupId}/instructor` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body JSON: [AssignInstructorRequest](#dto-assigninstructorrequest); payload chính minh họa trong luồng F05, F06, F10.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Instructor assigned to group successfully`; data: `GroupDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `term`, `termStatus`, `termClosedAt`, `studentReadOnly`, `courseCode`, `groupNo`, `name`, `projectName`, `ideaDescription`, `researchDomain`, `leader`, `requiredGpa`, `targetGrade`, `status`, `mentor`, `mentorAccountId`, `instructorId`, `instructorAccountId`, `instructorCode`, `instructorName`, `isLock`, `members`, `selectedProblem`, `recruitmentNeeds`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

ADMIN truyền instructorId là ACCOUNT ID. Group ACTIVE có thành viên; account phải INSTRUCTOR và có profile; khóa group rồi gán profile.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m13); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:180).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-14. unassignInstructor

- [ ] C12-14: `DELETE /api/groups/{groupId}/instructor` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Instructor unassigned from group successfully`; data: `GroupDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `term`, `termStatus`, `termClosedAt`, `studentReadOnly`, `courseCode`, `groupNo`, `name`, `projectName`, `ideaDescription`, `researchDomain`, `leader`, `requiredGpa`, `targetGrade`, `status`, `mentor`, `mentorAccountId`, `instructorId`, `instructorAccountId`, `instructorCode`, `instructorName`, `isLock`, `members`, `selectedProblem`, `recruitmentNeeds`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

ADMIN gỡ instructor dưới group lock; không xóa milestone/grade lịch sử.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m14); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:193).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-15. assignMentor

- [ ] C12-15: `PATCH /api/groups/{groupId}/mentor` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body JSON: [AssignMentorRequest](#dto-assignmentorrequest); payload chính minh họa trong luồng F05, F06, F10.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Mentor assigned to group successfully`; data: `GroupDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `term`, `termStatus`, `termClosedAt`, `studentReadOnly`, `courseCode`, `groupNo`, `name`, `projectName`, `ideaDescription`, `researchDomain`, `leader`, `requiredGpa`, `targetGrade`, `status`, `mentor`, `mentorAccountId`, `instructorId`, `instructorAccountId`, `instructorCode`, `instructorName`, `isLock`, `members`, `selectedProblem`, `recruitmentNeeds`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

ADMIN truyền mentorId là ACCOUNT ID, account MENTOR có profile; group ACTIVE có thành viên; khóa group và gán.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m15); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:205).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-16. unassignMentor

- [ ] C12-16: `DELETE /api/groups/{groupId}/mentor` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Mentor unassigned from group successfully`; data: `GroupDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `term`, `termStatus`, `termClosedAt`, `studentReadOnly`, `courseCode`, `groupNo`, `name`, `projectName`, `ideaDescription`, `researchDomain`, `leader`, `requiredGpa`, `targetGrade`, `status`, `mentor`, `mentorAccountId`, `instructorId`, `instructorAccountId`, `instructorCode`, `instructorName`, `isLock`, `members`, `selectedProblem`, `recruitmentNeeds`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

ADMIN gỡ mentor; không xóa meeting lịch sử.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m16); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:218).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-17. getMyAssignedGroups

- [ ] C12-17: `GET /api/groups/instructor/me` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| term | query | String | Không | — |
| courseCode | query | String | Không | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Assigned groups retrieved successfully`; data: `List<GroupSummaryDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `term`, `termStatus`, `termClosedAt`, `studentReadOnly`, `courseCode`, `groupNo`, `name`, `projectName`, `leaderName`, `memberCount`, `requiredGpa`, `targetGrade`, `status`, `mentorId`, `mentorAccountId`, `mentorCode`, `mentorName`, `instructorId`, `instructorAccountId`, `instructorCode`, `instructorName`, `isLock`, `selectedProblem`, `recruitmentNeeds`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

INSTRUCTOR: nhóm gán cho mình, filter term/course optional trim, ignore-case; createdAt DESC, id DESC.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m17); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:230).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-18. getGroupMilestones

- [ ] C12-18: `GET /api/groups/{groupId}/milestones` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Group timeline retrieved successfully`; data: `List<CourseMilestoneDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `term`, `courseCode`, `title`, `description`, `weight`, `deadlineAt`, `maxScore`, `position`, `status`, `instructorId`, `instructorName`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Chỉ INSTRUCTOR được gán nhóm hoặc STUDENT có membership được xem timeline; ADMIN/MENTOR không được service này cho phép. Lấy milestone thuộc instructor hiện tại, cùng term/course, bỏ ARCHIVED/INACTIVE; chưa gán instructor trả mảng rỗng.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m18); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:245).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C12-19. updateGroupLock

- [ ] C12-19: `PATCH /api/groups/{groupId}/lock` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body JSON: [UpdateGroupLockRequest](#dto-updategrouplockrequest); payload chính minh họa trong luồng F05, F06, F10.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Group locked successfully` hoặc `Group unlocked successfully`; data: `GroupDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `term`, `termStatus`, `termClosedAt`, `studentReadOnly`, `courseCode`, `groupNo`, `name`, `projectName`, `ideaDescription`, `researchDomain`, `leader`, `requiredGpa`, `targetGrade`, `status`, `mentor`, `mentorAccountId`, `instructorId`, `instructorAccountId`, `instructorCode`, `instructorName`, `isLock`, `members`, `selectedProblem`, `recruitmentNeeds`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Student leader, group ACTIVE, kỳ writable; đổi isLock; notify các thành viên khác với GROUP_LOCK_UPDATED và params groupId/locked.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c12-m19); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:259).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c13"></a>

## C13. GroupInvitationController

Luồng và fixture: **F06** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java). Luồng xử lý: [GroupInvitationServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupInvitationServiceImpl.java).

Trạng thái PENDING/ACCEPTED/DECLINED/CANCELED; hết hạn sau 72 giờ. Tạo/xem nhóm/hủy dành leader hoặc ADMIN; nhận/decline chỉ invitee. Có kiểm khóa thành viên và kỳ đóng theo từng nhánh student. Tối đa 6 thành viên và một nhóm trong mỗi kỳ/môn.

### C13-01. createInvitation

- [ ] C13-01: `POST /api/groups/{groupId}/invitations` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body JSON: [CreateInvitationRequest](#dto-createinvitationrequest); payload chính minh họa trong luồng F06.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Invitation created successfully`; data: `InvitationDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `groupId`, `groupName`, `groupNo`, `courseCode`, `term`, `inviterId`, `inviterCode`, `inviterName`, `inviteeId`, `inviteeCode`, `inviteeName`, `studentId`, `studentCode`, `studentName`, `status`, `message`, `createdAt`, `respondedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Khóa group; ADMIN dùng leader của nhóm làm inviter, thiếu leader trả 400. Resolve người nhận bằng MSSV/email; chặn tự mời, thành viên hiện tại, người có nhóm cùng scope, nhóm đủ 6, invitation pending trùng; tạo PENDING và notify invitee.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c13-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:27).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C13-02. getGroupInvitations

- [ ] C13-02: `GET /api/groups/{groupId}/invitations` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Invitations retrieved successfully`; data: `List<InvitationDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `groupId`, `groupName`, `groupNo`, `courseCode`, `term`, `inviterId`, `inviterCode`, `inviterName`, `inviteeId`, `inviteeCode`, `inviteeName`, `studentId`, `studentCode`, `studentName`, `status`, `message`, `createdAt`, `respondedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Chỉ leader/admin xem invitation của group; không cho mọi thành viên.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c13-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:38).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C13-03. getMyPendingInvitations

- [ ] C13-03: `GET /api/groups/invitations/me` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Pending invitations retrieved successfully`; data: `List<InvitationDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `groupId`, `groupName`, `groupNo`, `courseCode`, `term`, `inviterId`, `inviterCode`, `inviterName`, `inviteeId`, `inviteeCode`, `inviteeName`, `studentId`, `studentCode`, `studentName`, `status`, `message`, `createdAt`, `respondedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Chỉ PENDING gửi đến student hiện tại và chưa hết hạn.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c13-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:48).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C13-04. acceptInvitation

- [ ] C13-04: `POST /api/groups/invitations/{invitationId}/accept` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| invitationId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Invitation accepted successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

PENDING, chưa hết hạn, đúng invitee; khóa student rồi group; kiểm ACTIVE, writable, unlocked, count<6 và chưa có nhóm cùng scope. Tạo MEMBER/set ACCEPTED; decline invitation khác và cancel join requests cùng scope; notify leader.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c13-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:55).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C13-05. declineInvitation

- [ ] C13-05: `POST /api/groups/invitations/{invitationId}/decline` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| invitationId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Invitation declined successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

PENDING, chưa hết hạn, đúng invitee; khóa group và kiểm student write; set DECLINED/respondedAt, notify leader.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c13-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:65).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C13-06. cancelInvitation

- [ ] C13-06: `POST /api/groups/invitations/{invitationId}/cancel` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| invitationId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Invitation cancelled successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

PENDING, chưa hết hạn; leader/admin. Nhánh student khóa hàng group, kiểm leader và kỳ writable nhưng không kiểm isLock; ADMIN không đi qua nhánh khóa hàng/kiểm kỳ này. Set CANCELED/respondedAt.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c13-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:75).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c14"></a>

## C14. GroupJoinRequestController

Luồng và fixture: **F06** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java). Luồng xử lý: [GroupJoinRequestServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupJoinRequestServiceImpl.java).

Enum chính xác là PENDING/ACCEPTED/REJECTED/CANCELED, không phải APPROVED/CANCELLED. Student tạo/hủy request mình; leader/admin approve/reject. Group listing cho ADMIN, thành viên/leader hoặc assigned mentor. Alias có/không groupId dùng cùng logic.

### C14-01. getMyJoinRequests

- [ ] C14-01: `GET /api/groups/join-requests/me` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `My join requests retrieved successfully`; data: `List<GroupJoinRequestDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `groupId`, `groupName`, `groupNo`, `courseCode`, `term`, `studentId`, `studentCode`, `studentName`, `status`, `message`, `respondedById`, `respondedByCode`, `respondedByName`, `respondedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Student xem mọi request mình đã gửi, giữ cả lịch sử đã xử lý.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c14-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:27).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C14-02. createJoinRequest

- [ ] C14-02: `POST /api/groups/{groupId}/join-requests` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body JSON: [CreateJoinRequestDto](#dto-createjoinrequestdto); payload chính minh họa trong luồng F06.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Join request submitted successfully`; data: `GroupJoinRequestDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `groupId`, `groupName`, `groupNo`, `courseCode`, `term`, `studentId`, `studentCode`, `studentName`, `status`, `message`, `respondedById`, `respondedByCode`, `respondedByName`, `respondedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Group ACTIVE/unlocked, kỳ writable; student chưa có nhóm cùng kỳ/môn, chưa là thành viên, nhóm dưới 6, không có invitation PENDING cùng group. Pending request dưới 72h bị từ chối; hết hạn thì cancel và tạo mới; notify leader.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c14-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:34).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C14-03. getJoinRequests

- [ ] C14-03: `GET /api/groups/{groupId}/join-requests` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Join requests retrieved successfully`; data: `List<GroupJoinRequestDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `groupId`, `groupName`, `groupNo`, `courseCode`, `term`, `studentId`, `studentCode`, `studentName`, `status`, `message`, `respondedById`, `respondedByCode`, `respondedByName`, `respondedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

ADMIN, thành viên/leader hoặc assigned mentor xem request của nhóm; instructor không có quyền mặc định.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c14-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:45).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C14-04. approveJoinRequest

- [ ] C14-04.1: `POST /api/groups/{groupId}/join-requests/{requestId}/approve` — kết quả: NOT RUN.
- [ ] C14-04.2: `POST /api/groups/join-requests/{requestId}/approve` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Không | — |
| requestId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Join request approved successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Khóa student và group; PENDING, chưa hết hạn 72h, groupId nếu truyền phải đúng. Leader/admin; nhóm ACTIVE, unlocked cho cả ADMIN và student, dưới 6 và student chưa có nhóm cùng scope. Kỳ writable chỉ bắt buộc với non-admin. Tạo membership, set ACCEPTED/responder/time (responder student có thể null với ADMIN); cancel requests và decline invitations khác cùng scope, notify requester.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c14-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:55).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C14-05. rejectJoinRequest

- [ ] C14-05.1: `POST /api/groups/{groupId}/join-requests/{requestId}/reject` — kết quả: NOT RUN.
- [ ] C14-05.2: `POST /api/groups/join-requests/{requestId}/reject` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Không | — |
| requestId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Join request rejected successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

PENDING và groupId đúng; leader/admin; khóa group, chỉ non-admin chịu student write gate, không kiểm isLock. Set REJECTED/responder/time, notify requester. Java không áp expiration check giống approve ở nhánh này.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c14-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:69).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C14-06. cancelJoinRequest

- [ ] C14-06.1: `POST /api/groups/{groupId}/join-requests/{requestId}/cancel` — kết quả: NOT RUN.
- [ ] C14-06.2: `POST /api/groups/join-requests/{requestId}/cancel` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Không | — |
| requestId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Join request cancelled successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Chỉ requester, PENDING và groupId đúng; khóa group, student write gate nhưng không kiểm isLock; set CANCELED/respondedAt.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c14-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:83).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c15"></a>

## C15. GroupMeetingController

Luồng và fixture: **F09** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java). Luồng xử lý: [MeetingServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MeetingServiceImpl.java), [MentorScheduleTimePolicy](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MentorScheduleTimePolicy.java).

POST có slotId: leader đặt slot; không slotId: assigned mentor tạo trực tiếp. Tối đa 2 cuộc SCHEDULED+COMPLETED mỗi nhóm. Lịch bắt đầu phút 00/30, không giây/nano, kéo dài đúng 60 phút. Link meeting được trim, bắt đầu http:// hoặc https://, dài tối đa 500 ký tự; không nhầm với regex Google Meet nghiêm ngặt của availability. Quyền đọc gồm member, assigned mentor và assigned instructor; ADMIN không mặc nhiên được đọc.

### C15-01. getAvailableMentorSlots

- [ ] C15-01: `GET /api/groups/{groupId}/mentor/availability` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Available slots retrieved successfully`; data: `List<MentorAvailabilitySlotDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `mentorId`, `mentorCode`, `mentorName`, `startAt`, `endAt`, `meetLink`, `note`, `status`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Chỉ member; group phải có mentor. Trả slots AVAILABLE của mentor, startAt>now, tăng dần.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c15-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:34).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C15-02. createMeeting

- [ ] C15-02: `POST /api/groups/{groupId}/mentor/meetings` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body JSON: [CreateOrBookMentorMeetingRequest](#dto-createorbookmentormeetingrequest); payload chính minh họa trong luồng F09.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Meeting booked successfully` hoặc `Meeting created successfully`; data: `MentorMeetingDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `slotId`, `groupId`, `groupName`, `groupNo`, `projectName`, `selectedProblemId`, `selectedProblemTitle`, `mentorId`, `mentorCode`, `mentorName`, `bookedByStudentId`, `bookedByStudentCode`, `bookedByStudentName`, `startAt`, `endAt`, `meetLink`, `note`, `status`, `leaderConfirmedByStudentId`, `leaderConfirmedAt`, `mentorConfirmedAt`, `completedAt`, `canceledAt`, `cancelReason`, `evidenceImageUrl`, `evidenceSubmittedByStudentId`, `evidenceSubmittedByStudentName`, `evidenceSubmittedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Booking khóa group+slot; leader và kỳ writable; kiểm quota 2, slot future/đúng mentor/AVAILABLE/time policy/link. Set slot BOOKED, tạo SCHEDULED, copy lịch/link/note; notify mentor+members trừ actor. Tạo trực tiếp chỉ assigned mentor, kiểm lịch/overlap/link/quota nhưng KHÔNG kiểm startAt ở tương lai; note trim/blank→null; notify members. DTO CreateOrBook bắt buộc chọn slotId hoặc cặp startAt/endAt, không đồng thời; nhánh direct bắt buộc meetLink, note/link tối đa 500 ký tự.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c15-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:45).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C15-03. updateMeeting

- [ ] C15-03: `PATCH /api/groups/{groupId}/mentor/meetings/{meetingId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| meetingId | path | Long | Có | — |

Body JSON: [UpdateMentorMeetingRequest](#dto-updatementormeetingrequest); payload chính minh họa trong luồng F09.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Meeting updated successfully`; data: `MentorMeetingDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `slotId`, `groupId`, `groupName`, `groupNo`, `projectName`, `selectedProblemId`, `selectedProblemTitle`, `mentorId`, `mentorCode`, `mentorName`, `bookedByStudentId`, `bookedByStudentCode`, `bookedByStudentName`, `startAt`, `endAt`, `meetLink`, `note`, `status`, `leaderConfirmedByStudentId`, `leaderConfirmedAt`, `mentorConfirmedAt`, `completedAt`, `canceledAt`, `cancelReason`, `evidenceImageUrl`, `evidenceSubmittedByStudentId`, `evidenceSubmittedByStudentName`, `evidenceSubmittedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Assigned mentor, meeting thuộc group và mutable; startAt/endAt không truyền giữ cũ; revalidate lịch/overlap/link, không kiểm future. HTTP bắt buộc meetLink do UpdateMentorMeetingRequest có @NotBlank và controller có @Valid; fallback null trong service không làm field này optional ở HTTP. Note chỉ sửa khi non-null; notify members.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c15-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:66).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C15-04. submitEvidence

- [ ] C15-04: `PUT /api/groups/{groupId}/mentor/meetings/{meetingId}/evidence` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| meetingId | path | Long | Có | — |

Body JSON: [SubmitMeetingEvidenceRequest](#dto-submitmeetingevidencerequest); payload chính minh họa trong luồng F09.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Meeting evidence submitted successfully`; data: `MentorMeetingDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `slotId`, `groupId`, `groupName`, `groupNo`, `projectName`, `selectedProblemId`, `selectedProblemTitle`, `mentorId`, `mentorCode`, `mentorName`, `bookedByStudentId`, `bookedByStudentCode`, `bookedByStudentName`, `startAt`, `endAt`, `meetLink`, `note`, `status`, `leaderConfirmedByStudentId`, `leaderConfirmedAt`, `mentorConfirmedAt`, `completedAt`, `canceledAt`, `cancelReason`, `evidenceImageUrl`, `evidenceSubmittedByStudentId`, `evidenceSubmittedByStudentName`, `evidenceSubmittedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Bất kỳ active student member, không chỉ leader. Kỳ writable; meeting SCHEDULED, chưa evidence, đã kết thúc. Trim URL; lưu người/thời gian, chuyển COMPLETED; notify mentor và instructor. Không cho gửi lần hai.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c15-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:74).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C15-05. cancelMeeting

- [ ] C15-05: `PATCH /api/groups/{groupId}/mentor/meetings/{meetingId}/cancel` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| meetingId | path | Long | Có | — |

Body JSON: [CancelMeetingRequest](#dto-cancelmeetingrequest); payload chính minh họa trong luồng F09.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Meeting canceled successfully`; data: `MentorMeetingDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `slotId`, `groupId`, `groupName`, `groupNo`, `projectName`, `selectedProblemId`, `selectedProblemTitle`, `mentorId`, `mentorCode`, `mentorName`, `bookedByStudentId`, `bookedByStudentCode`, `bookedByStudentName`, `startAt`, `endAt`, `meetLink`, `note`, `status`, `leaderConfirmedByStudentId`, `leaderConfirmedAt`, `mentorConfirmedAt`, `completedAt`, `canceledAt`, `cancelReason`, `evidenceImageUrl`, `evidenceSubmittedByStudentId`, `evidenceSubmittedByStudentName`, `evidenceSubmittedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

CANCELED trả lại hiện hữu; COMPLETED trả 400, có evidence trả 409. Assigned mentor hủy, reason trim; slot liên kết chuyển CANCELED, không AVAILABLE; notify members trừ actor.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c15-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:82).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C15-06. confirmMeeting

- [ ] C15-06: `PATCH /api/groups/{groupId}/mentor/meetings/{meetingId}/confirm` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| meetingId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Meeting confirmation saved successfully`; data: `MentorMeetingDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `slotId`, `groupId`, `groupName`, `groupNo`, `projectName`, `selectedProblemId`, `selectedProblemTitle`, `mentorId`, `mentorCode`, `mentorName`, `bookedByStudentId`, `bookedByStudentCode`, `bookedByStudentName`, `startAt`, `endAt`, `meetLink`, `note`, `status`, `leaderConfirmedByStudentId`, `leaderConfirmedAt`, `mentorConfirmedAt`, `completedAt`, `canceledAt`, `cancelReason`, `evidenceImageUrl`, `evidenceSubmittedByStudentId`, `evidenceSubmittedByStudentName`, `evidenceSubmittedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

CANCELED trả 400, COMPLETED trả hiện hữu; phải có slot và slot đã bắt đầu. Leader hoặc assigned mentor xác nhận, student cần kỳ writable. Chỉ đặt timestamp lần đầu. Không tự hoàn thành dù cả hai xác nhận; vẫn cần evidence.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c15-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:95).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C15-07. getGroupMeetings

- [ ] C15-07: `GET /api/groups/{groupId}/mentor/meetings` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Group meetings retrieved successfully`; data: `List<MentorMeetingDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `slotId`, `groupId`, `groupName`, `groupNo`, `projectName`, `selectedProblemId`, `selectedProblemTitle`, `mentorId`, `mentorCode`, `mentorName`, `bookedByStudentId`, `bookedByStudentCode`, `bookedByStudentName`, `startAt`, `endAt`, `meetLink`, `note`, `status`, `leaderConfirmedByStudentId`, `leaderConfirmedAt`, `mentorConfirmedAt`, `completedAt`, `canceledAt`, `cancelReason`, `evidenceImageUrl`, `evidenceSubmittedByStudentId`, `evidenceSubmittedByStudentName`, `evidenceSubmittedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Group tồn tại; member/assigned mentor/assigned instructor được đọc. DTO đầy đủ, legacy thời gian thiếu fallback từ slot.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c15-m7); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:107).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C15-08. getGroupMeetingDetail

- [ ] C15-08: `GET /api/groups/{groupId}/mentor/meetings/{meetingId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| meetingId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Group meeting detail retrieved successfully`; data: `MentorMeetingDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `slotId`, `groupId`, `groupName`, `groupNo`, `projectName`, `selectedProblemId`, `selectedProblemTitle`, `mentorId`, `mentorCode`, `mentorName`, `bookedByStudentId`, `bookedByStudentCode`, `bookedByStudentName`, `startAt`, `endAt`, `meetLink`, `note`, `status`, `leaderConfirmedByStudentId`, `leaderConfirmedAt`, `mentorConfirmedAt`, `completedAt`, `canceledAt`, `cancelReason`, `evidenceImageUrl`, `evidenceSubmittedByStudentId`, `evidenceSubmittedByStudentName`, `evidenceSubmittedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Meeting phải thuộc group, cùng quyền đọc; sai group trả 404.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c15-m8); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:118).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c16"></a>

## C16. GroupProblemController

Luồng và fixture: **F07** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java). Luồng xử lý: [GroupProblemServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupProblemServiceImpl.java).

Prefix chỉ STUDENT. Leader mới được select/clear/propose/update/delete; GET proposals cho mọi thành viên của nhóm. Các thao tác ghi áp dụng kiểm tra kỳ writable; đối chiếu transaction/khóa theo service gốc. Proposal mới tự trở thành selectedProblem.

### C16-01. selectProblem

- [ ] C16-01: `POST /api/groups/{groupId}/problems/select` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body JSON: [SelectProblemRequest](#dto-selectproblemrequest); payload chính minh họa trong luồng F07.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Problem selected successfully`; data: `GroupDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `term`, `termStatus`, `termClosedAt`, `studentReadOnly`, `courseCode`, `groupNo`, `name`, `projectName`, `ideaDescription`, `researchDomain`, `leader`, `requiredGpa`, `targetGrade`, `status`, `mentor`, `mentorAccountId`, `instructorId`, `instructorAccountId`, `instructorCode`, `instructorName`, `isLock`, `members`, `selectedProblem`, `recruitmentNeeds`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Leader, kỳ writable; problem tồn tại, ACTIVE và OFFICIAL. Hai lỗi trạng thái/type riêng biệt; lưu selection, trả GroupDetail.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c16-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:27).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C16-02. clearProblem

- [ ] C16-02: `DELETE /api/groups/{groupId}/problems/select` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Selected problem cleared successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Leader, kỳ writable; đặt selectedProblem=null, trả data=null.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c16-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:42).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C16-03. proposeProblem

- [ ] C16-03: `POST /api/groups/{groupId}/problems/propose` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body JSON: [ProposeGroupProblemRequest](#dto-proposegroupproblemrequest); payload chính minh họa trong luồng F07.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Problem proposed successfully`; data: `ProblemDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `code`, `title`, `statement`, `strategicTheme`, `researchArea`, `difficultyLevel`, `expectedOutput`, `ownerLab`, `suggestedCourses`, `driveFolderLink`, `sourceType`, `status`, `domain`, `proposedByGroup`, `proposedByStudent`, `reviewComment`, `reviewedBy`, `reviewedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Leader, kỳ writable; domain bắt buộc ACTIVE; tạo SELF_PROPOSED/PENDING_REVIEW với chủ sở hữu group/student và trường DTO. Code tự sinh `SP-{groupId}-{8 ký tự đầu UUID}`; title/statement được lấy nguyên request, không trim trong service. Chọn proposal trong cùng transaction.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c16-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:56).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C16-04. updatePendingProposal

- [ ] C16-04: `PUT /api/groups/{groupId}/problems/proposals/{problemId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| problemId | path | Long | Có | — |

Body JSON: [ProposeGroupProblemRequest](#dto-proposegroupproblemrequest); payload chính minh họa trong luồng F07.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Problem proposal updated successfully`; data: `ProblemDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `code`, `title`, `statement`, `strategicTheme`, `researchArea`, `difficultyLevel`, `expectedOutput`, `ownerLab`, `suggestedCourses`, `driveFolderLink`, `sourceType`, `status`, `domain`, `proposedByGroup`, `proposedByStudent`, `reviewComment`, `reviewedBy`, `reviewedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Leader, kỳ writable; sai owner group→404, sai SELF_PROPOSED→400, không PENDING_REVIEW→409; domain ACTIVE; sửa fields.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c16-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:71).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C16-05. deletePendingProposal

- [ ] C16-05: `DELETE /api/groups/{groupId}/problems/proposals/{problemId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| problemId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Problem proposal deleted successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Cùng kiểm owner/type/status; clear selection nếu đang chọn; xóa proposal thật.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c16-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:87).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C16-06. getGroupProposals

- [ ] C16-06: `GET /api/groups/{groupId}/problems/proposals` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Group proposals retrieved successfully`; data: `List<ProblemSummaryDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `code`, `title`, `domainCode`, `domainName`, `difficultyLevel`, `sourceType`, `status`, `strategicTheme`, `researchArea`, `proposedByGroupId`, `proposedByGroupNo`, `proposedByGroupName`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Student có membership của group được đọc các proposal do group đề xuất; không cần là leader. Không có group/profile/membership trả 403 theo service gốc.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c16-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:102).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c17"></a>

## C17. GroupRecruitmentRoleController

Luồng và fixture: **F04** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupRecruitmentRoleController.java).

Authenticated. Nguồn là enum RecruitmentRole theo thứ tự khai báo: 26 roles thuộc TECHNOLOGY, DESIGN, BUSINESS, COMMUNICATION, LANGUAGE_LEGAL. Không query DB, không có CRUD.

### C17-01. getRecruitmentRoles

- [ ] C17-01: `GET /api/group-recruitment-roles` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Recruitment roles retrieved successfully`; data: `List<RecruitmentRoleDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `code`, `category`, `displayNameVi`, `displayNameEn`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Trả mảng code/category/displayNameVi/displayNameEn đúng enum; giữ nguyên nhãn tiếng Việt, tiếng Anh và thứ tự.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c17-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupRecruitmentRoleController.java:22).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c18"></a>

## C18. GroupTaskController

Luồng và fixture: **F08** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java). Luồng xử lý: [GroupTaskServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupTaskServiceImpl.java), [StudentTermWriteGuard](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/StudentTermWriteGuard.java).

Đọc/comment: student member hoặc assigned mentor; không mặc định cho admin/instructor. Create/update/assign/archive/restore/reorder: leader. Đổi cột/checklist: leader hoặc assignee còn membership. Student ghi cần kỳ writable; mentor comment không áp student gate. Năm trạng thái BACKLOG/TODO/IN_PROGRESS/REVIEW/DONE; bốn priority LOW/MEDIUM/HIGH/URGENT. Row lock và transaction đảm bảo thứ tự từng board/cột; stale version trả 409.

### C18-01. getBoard

- [ ] C18-01.1: `GET /api/groups/{groupId}/board` — kết quả: NOT RUN.
- [ ] C18-01.2: `GET /api/groups/{groupId}/boards/{boardId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| boardId | path | Long | Không | — |
| boardId | query | Long | Không | — |
| priority | query | String | Không | — |
| assigneeStudentId | query | Long | Không | — |
| search | query | String | Không | — |
| includeArchived | query | Boolean | Không | `false` |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Group task board retrieved successfully`; data: `GroupTaskBoardDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `groupId`, `groupName`, `columns`, `activeTaskCount`, `overdueTaskCount`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Đọc default board hoặc boardId; kiểm member/mentor. Lazy-create Default dưới lock khi thiếu; archived board trả 400. Lọc priority/assignee/search/includeArchived; trả 5 cột theo thứ tự cố định. Hai thống kê đếm toàn board, không chịu filter hiển thị.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:34).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C18-02. createTask

- [ ] C18-02: `POST /api/groups/{groupId}/tasks` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body JSON: [CreateTaskRequest](#dto-createtaskrequest); payload chính minh họa trong luồng F08.

**Response mong đợi:** HTTP **201**; envelope code **200**; message `Task created successfully`; data: `TaskDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `title`, `description`, `status`, `priority`, `dueAt`, `position`, `version`, `createdByStudentId`, `createdByStudentName`, `archivedAt`, `createdAt`, `updatedAt`, `assignees`, `checklistCount`, `checklistCompletedCount`, `checklistProgressPercent`, `overdue`, `checklistItems`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Leader; title trim 1–255; board cùng group/chưa archive. Initial status chỉ BACKLOG/TODO, mặc định BACKLOG; priority mặc định MEDIUM; dueAt future. List assignees null được xem như rỗng; các phần tử không được null/trùng và phải là thành viên hiện tại của group, không có kiểm tra riêng student/account ACTIVE ở bước này. Append position, ghi activities và notify assignees.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:53).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C18-03. getTaskDetail

- [ ] C18-03: `GET /api/groups/{groupId}/tasks/{taskId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| taskId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Task details retrieved successfully`; data: `TaskDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `title`, `description`, `status`, `priority`, `dueAt`, `position`, `version`, `createdByStudentId`, `createdByStudentName`, `archivedAt`, `createdAt`, `updatedAt`, `assignees`, `checklistCount`, `checklistCompletedCount`, `checklistProgressPercent`, `overdue`, `checklistItems`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Read access và đúng group/task; trả assignees/checklist/time/version/counters. Legacy task thiếu board có thể được gán Default.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:65).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C18-04. updateTask

- [ ] C18-04: `PATCH /api/groups/{groupId}/tasks/{taskId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| taskId | path | Long | Có | — |

Body JSON: [UpdateTaskRequest](#dto-updatetaskrequest); payload chính minh họa trong luồng F08.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Task updated successfully`; data: `TaskDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `title`, `description`, `status`, `priority`, `dueAt`, `position`, `version`, `createdByStudentId`, `createdByStudentName`, `archivedAt`, `createdAt`, `updatedAt`, `assignees`, `checklistCount`, `checklistCompletedCount`, `checklistProgressPercent`, `overdue`, `checklistItems`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Leader, task chưa archive; version bắt buộc (@NotNull) và phải khớp. PATCH non-null; title/priority hợp lệ; dueAt và clearDueAt không đồng thời. Chỉ kiểm dueAt future khi giá trị khác dueAt đang lưu; gửi lại cùng deadline đã quá hạn không bị rule này chặn. Chỉ ghi change activity khi có thay đổi.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:77).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C18-05. replaceAssignees

- [ ] C18-05: `PUT /api/groups/{groupId}/tasks/{taskId}/assignees` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| taskId | path | Long | Có | — |

Body JSON: [ReplaceTaskAssigneesRequest](#dto-replacetaskassigneesrequest); payload chính minh họa trong luồng F08.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Task assignees replaced successfully`; data: `TaskDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `title`, `description`, `status`, `priority`, `dueAt`, `position`, `version`, `createdByStudentId`, `createdByStudentName`, `archivedAt`, `createdAt`, `updatedAt`, `assignees`, `checklistCount`, `checklistCompletedCount`, `checklistProgressPercent`, `overdue`, `checklistItems`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Leader, version và list bắt buộc; list rỗng để xóa hết. Kiểm membership hiện tại/trùng/null, không tự thêm điều kiện student/account ACTIVE; diff added/removed, activity; notify người mới được gán, bỏ actor.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:90).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C18-06. moveTask

- [ ] C18-06: `PATCH /api/groups/{groupId}/tasks/{taskId}/move` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| taskId | path | Long | Có | — |

Body JSON: [MoveTaskRequest](#dto-movetaskrequest); payload chính minh họa trong luồng F08.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Task moved successfully`; data: `TaskDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `title`, `description`, `status`, `priority`, `dueAt`, `position`, `version`, `createdByStudentId`, `createdByStudentName`, `archivedAt`, `createdAt`, `updatedAt`, `assignees`, `checklistCount`, `checklistCompletedCount`, `checklistProgressPercent`, `overdue`, `checklistItems`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Task chưa archive, version bắt buộc và khớp, target status hợp lệ/position≥0. Reorder cùng cột chỉ leader; đổi cột cho leader/assignee. Khóa các cột liên quan, clamp vị trí, chuẩn hóa positions; luôn ghi TASK_MOVED activity, notification chỉ khi đổi status. No-op vẫn có activity nhưng không tự tăng entity version nếu entity không đổi.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:103).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C18-07. archiveTask

- [ ] C18-07: `DELETE /api/groups/{groupId}/tasks/{taskId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| taskId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Task archived successfully`; data: `TaskDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `title`, `description`, `status`, `priority`, `dueAt`, `position`, `version`, `createdByStudentId`, `createdByStudentName`, `archivedAt`, `createdAt`, `updatedAt`, `assignees`, `checklistCount`, `checklistCompletedCount`, `checklistProgressPercent`, `overdue`, `checklistItems`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Leader, chưa archive; set archivedAt, compact cột, ghi activity; giữ assignment/history.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m7); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:116).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C18-08. restoreTask

- [ ] C18-08: `POST /api/groups/{groupId}/tasks/{taskId}/restore` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| taskId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Task restored successfully`; data: `TaskDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `title`, `description`, `status`, `priority`, `dueAt`, `position`, `version`, `createdByStudentId`, `createdByStudentName`, `archivedAt`, `createdAt`, `updatedAt`, `assignees`, `checklistCount`, `checklistCompletedCount`, `checklistProgressPercent`, `overdue`, `checklistItems`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Leader, phải archived; append cuối cột, normalize; xóa assignment của người đã rời nhóm và ghi system activity; clear archivedAt, TASK_RESTORED.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m8); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:128).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C18-09. addChecklistItem

- [ ] C18-09: `POST /api/groups/{groupId}/tasks/{taskId}/checklist-items` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| taskId | path | Long | Có | — |

Body JSON: [CreateChecklistItemRequest](#dto-createchecklistitemrequest); payload chính minh họa trong luồng F08.

**Response mong đợi:** HTTP **201**; envelope code **200**; message `Checklist item added successfully`; data: `ChecklistItemDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `title`, `completed`, `completedByAccountId`, `completedAt`, `position`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Leader/assignee; checklist title trim 1–500; append position, completed=false, activity.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m9); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:140).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C18-10. updateChecklistItem

- [ ] C18-10: `PATCH /api/groups/{groupId}/tasks/{taskId}/checklist-items/{itemId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| taskId | path | Long | Có | — |
| itemId | path | Long | Có | — |

Body JSON: [UpdateChecklistItemRequest](#dto-updatechecklistitemrequest); payload chính minh họa trong luồng F08.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Checklist item updated successfully`; data: `ChecklistItemDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `title`, `completed`, `completedByAccountId`, `completedAt`, `position`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Leader/assignee; item đúng task; optional title/completed/position, position≥0; reorder liên tục và completion metadata.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m10); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:153).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C18-11. deleteChecklistItem

- [ ] C18-11: `DELETE /api/groups/{groupId}/tasks/{taskId}/checklist-items/{itemId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| taskId | path | Long | Có | — |
| itemId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Checklist item deleted successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Leader/assignee; xóa đúng item, compact position, activity.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m11); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:167).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C18-12. getComments

- [ ] C18-12: `GET /api/groups/{groupId}/tasks/{taskId}/comments` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| taskId | path | Long | Có | — |
| page | query | int | Không | `0` |
| size | query | int | Không | `20` |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Comments retrieved successfully`; data: `PageResponse<TaskCommentDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): PageResponse: content/page/number/size/numberOfElements/totalElements/totalPages/hasNext/hasPrevious. Mỗi content item: `id`, `authorAccountId`, `authorEmail`, `authorFullName`, `authorRole`, `content`, `editedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Read access; page≥0,size>0, size>100 được clamp 100; mặc định theo chữ ký ở phụ lục, sort comment newest-first.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m12); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:180).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C18-13. createComment

- [ ] C18-13: `POST /api/groups/{groupId}/tasks/{taskId}/comments` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| taskId | path | Long | Có | — |

Body JSON: [CreateTaskCommentRequest](#dto-createtaskcommentrequest); payload chính minh họa trong luồng F08.

**Response mong đợi:** HTTP **201**; envelope code **200**; message `Comment added successfully`; data: `TaskCommentDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `authorAccountId`, `authorEmail`, `authorFullName`, `authorRole`, `content`, `editedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Member/assigned mentor; task chưa archive, content trim không rỗng; student kỳ đóng bị chặn. Lưu comment/activity, notify leader/assignees trừ actor.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m13); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:194).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C18-14. updateComment

- [ ] C18-14: `PATCH /api/groups/{groupId}/tasks/{taskId}/comments/{commentId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| taskId | path | Long | Có | — |
| commentId | path | Long | Có | — |

Body JSON: [UpdateTaskCommentRequest](#dto-updatetaskcommentrequest); payload chính minh họa trong luồng F08.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Comment updated successfully`; data: `TaskCommentDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `authorAccountId`, `authorEmail`, `authorFullName`, `authorRole`, `content`, `editedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Chỉ author có quyền truy cập task, task mutable; content trim không rỗng; chỉ cập nhật editedAt và ghi activity khi content thực sự thay đổi.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m14); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:207).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C18-15. deleteComment

- [ ] C18-15: `DELETE /api/groups/{groupId}/tasks/{taskId}/comments/{commentId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| taskId | path | Long | Có | — |
| commentId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Comment deleted successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Author hoặc student leader; task mutable, student write gate; xóa và ghi activity.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m15); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:221).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C18-16. getActivities

- [ ] C18-16: `GET /api/groups/{groupId}/tasks/{taskId}/activities` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| taskId | path | Long | Có | — |
| page | query | int | Không | `0` |
| size | query | int | Không | `20` |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Activities retrieved successfully`; data: `PageResponse<TaskActivityDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): PageResponse: content/page/number/size/numberOfElements/totalElements/totalPages/hasNext/hasPrevious. Mỗi content item: `id`, `taskId`, `groupId`, `actor`, `activityType`, `details`, `createdAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Read access; page≥0,size>0, size>100 được clamp 100; activity newest-first, actor có thể null cho system, details JSON.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m16); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:234).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C18-17. getMyAssignedTasks

- [ ] C18-17: `GET /api/tasks/me` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | query | Long | Không | — |
| status | query | String | Không | — |
| priority | query | String | Không | — |
| overdue | query | Boolean | Không | — |
| dueBefore | query | Instant | Không | — |
| page | query | int | Không | `0` |
| size | query | int | Không | `20` |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `My assigned tasks retrieved successfully`; data: `PageResponse<TaskSummaryDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): PageResponse: content/page/number/size/numberOfElements/totalElements/totalPages/hasNext/hasPrevious. Mỗi content item: `id`, `title`, `status`, `priority`, `dueAt`, `position`, `version`, `createdByStudentId`, `createdByStudentName`, `archivedAt`, `createdAt`, `updatedAt`, `assignees`, `checklistCount`, `checklistCompletedCount`, `checklistProgressPercent`, `overdue`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Student: lấy task theo assignment.studentId của người gọi; lọc groupId/status/priority/overdue/dueBefore, không có search. Query không join kiểm lại membership; không mô tả thành bảo đảm “chỉ nhóm hiện còn tham gia” độc lập với cleanup assignment. Bỏ archived; dueBefore strict<; page≥0,size>0, clamp size về 100. Sắp dueAt ASC nulls-last rồi updatedAt DESC.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m17); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:248).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C18-18. reorderTask

- [ ] C18-18: `POST /api/groups/{groupId}/tasks/reorder` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body JSON: [ReorderTaskRequest](#dto-reordertaskrequest); payload chính minh họa trong luồng F08.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Task reordered successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Reorder wrapper truyền taskId/status/position/version vào Move; trả 200 data=null.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c18-m18); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:265).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c19"></a>

## C19. ImportController

Luồng và fixture: **F03** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java). Luồng xử lý: [ImportServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ImportServiceImpl.java), [ImportValidationServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ImportValidationServiceImpl.java), [ImportRowExecutorImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ImportRowExecutorImpl.java), [ImportJobSupport](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ImportJobSupport.java).

Chỉ ADMIN. Multipart file CSV/XLSX; lỗi file rỗng/tên/type trả 400 trước queue. Một job QUEUED/RUNNING tại một thời điểm, HTTP202. Worker xử lý và dọn file tạm. Dòng lưu bằng REQUIRES_NEW, một dòng lỗi không rollback dòng thành công. Batch FAILED nếu tất cả dòng fail hoặc lỗi xử lý; còn lại COMPLETED, vẫn có row errors/warnings.

### C19-01. importStudents

- [ ] C19-01: `POST /api/imports/students` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| file | multipart | MultipartFile | Có | — |

**Response mong đợi:** HTTP **202**; envelope code **202**; message `Student import job queued successfully`; data: `ImportResultResponse`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `batchId`, `targetType`, `status`, `fileName`, `fileType`, `totalRows`, `successRows`, `failedRows`, `startedAt`, `finishedAt`, `createdGroups`, `skippedGroups`, `leaderFallbackWarnings`, `assignedMentors`, `mentorAssignmentWarnings`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Queue STUDENT. Validate email/code/name/password nếu có≥8/gender/date; trùng file/DB thành row errors. Identity email+code cùng student inactive có thể reactivate giữ hash. Workbook dùng sheet kỳ/môn, kế thừa group context; group identity (term,normalized name), leader so tên bỏ dấu/fallback warning, mentor parse code/warning, membership không trùng scope, Default board khi tạo group.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c19-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:43).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C19-02. importMentors

- [ ] C19-02: `POST /api/imports/mentors` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| file | multipart | MultipartFile | Có | — |

**Response mong đợi:** HTTP **202**; envelope code **202**; message `Mentor import job queued successfully`; data: `ImportResultResponse`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `batchId`, `targetType`, `status`, `fileName`, `fileType`, `totalRows`, `successRows`, `failedRows`, `startedAt`, `finishedAt`, `createdGroups`, `skippedGroups`, `leaderFallbackWarnings`, `assignedMentors`, `mentorAssignmentWarnings`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Queue MENTOR; code/email/name bắt buộc; experience nhận số/nhãn Việt. BCrypt password nhập hoặc random; ACTIVE,mustChangePassword=false; không tạo group.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c19-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:54).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C19-03. getBatchStatus

- [ ] C19-03: `GET /api/imports/{batchId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| batchId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Success`; data: `ImportBatch`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `targetType`, `fileName`, `fileType`, `status`, `totalRows`, `successRows`, `failedRows`, `startedAt`, `finishedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Đọc batch theo public id: target/file/type/status/counts/start/finish; thiếu 404, không trả password.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c19-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:65).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C19-04. getBatchErrors

- [ ] C19-04: `GET /api/imports/{batchId}/errors` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| batchId | path | Long | Có | — |
| page | query | int | Không | `0` |
| size | query | int | Không | `20` |
| search | query | String | Không | — |
| rowNumber | query | Integer | Không | — |
| fieldName | query | String | Không | — |
| errorCode | query | ImportErrorCode | Không | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Success`; data: `PageResponse<ImportRowErrorDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): PageResponse: content/page/number/size/numberOfElements/totalElements/totalPages/hasNext/hasPrevious. Mỗi content item: `rowNumber`, `fieldName`, `errorCode`, `errorMessage`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Page0,size20 (1–100), rowNumber≥1; filter search/rowNumber/fieldName/errorCode; rowNumber ASC,id ASC; trả field/message/error code/row number.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c19-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:74).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C19-05. getStudentTemplate

- [ ] C19-05: `GET /api/imports/templates/students` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; file nhị phân, không JSON. Kiểm MIME/Content-Disposition và nội dung ở phần dưới.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tải template student với format và filename/header/content type ở phụ lục; không JSON.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c19-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:110).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C19-06. getMentorTemplate

- [ ] C19-06: `GET /api/imports/templates/mentors` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; file nhị phân, không JSON. Kiểm MIME/Content-Disposition và nội dung ở phần dưới.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tải template mentor, đúng cột/alias/format; không tạo job.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c19-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:119).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C19-07. getProblemBankTemplate

- [ ] C19-07: `GET /api/imports/templates/problem-bank` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; file nhị phân, không JSON. Kiểm MIME/Content-Disposition và nội dung ở phần dưới.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tải template problem bank theo format CSV/XLSX, đúng sheet/header parser.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c19-m7); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:128).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c20"></a>

## C20. InstructorGroupBoardController

Luồng và fixture: **F05** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorGroupBoardController.java). Luồng xử lý: [InstructorGroupBoardServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/InstructorGroupBoardServiceImpl.java).

Chỉ INSTRUCTOR có account và profile ACTIVE. Danh sách chỉ gồm group ACTIVE, có thành viên, thuộc kỳ OPEN. assignment nhận ALL/AVAILABLE/MINE/OTHER; page mặc định 0, size 12 (1–100). Summary và course counts không chỉ tính trong trang hiện tại.

### C20-01. getBoard

- [ ] C20-01: `GET /api/instructor/groups/board` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| page | query | int | Không | `0` |
| size | query | int | Không | `12` |
| term | query | String | Không | — |
| courseCode | query | String | Không | — |
| search | query | String | Không | — |
| assignment | query | InstructorGroupAssignmentFilter | Không | `ALL` |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Instructor group board retrieved successfully`; data: `InstructorGroupBoardResponseDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `summary`, `courses`, `groups`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Lọc term/course đã trim, không phân biệt hoa thường. Search gồm tên nhóm, groupNo, project và tên/MSSV/email/className thành viên. Summary áp term/course, không áp search/assignment; courses chỉ áp term, không áp courseCode. Trang sắp createdAt DESC, id DESC; members ưu tiên leader rồi joinedAt/id.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c20-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorGroupBoardController.java:36).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C20-02. claimGroup

- [ ] C20-02: `POST /api/instructor/groups/{groupId}/claim` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Group claimed successfully`; data: `InstructorGroupBoardItemDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `term`, `courseCode`, `groupNo`, `name`, `projectName`, `ideaDescription`, `researchDomain`, `isLock`, `memberCount`, `mentorId`, `mentorCode`, `mentorName`, `instructorId`, `instructorCode`, `instructorName`, `assignmentState`, `members`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Khóa kỳ rồi group; kỳ OPEN, group ACTIVE và có thành viên. Chưa gán thì gán instructor hiện tại; đã gán chính mình trả nguyên trạng; gán người khác trả 409. Khác API admin assignment dùng accountId.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c20-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorGroupBoardController.java:63).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c21"></a>

## C21. InstructorProblemController

Luồng và fixture: **F07** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorProblemController.java). Luồng xử lý: [ProblemBankServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemBankServiceImpl.java).

Chỉ INSTRUCTOR có account/profile ACTIVE. Không được duyệt proposal của nhóm do instructor khác phụ trách.

### C21-01. getPendingProblems

- [ ] C21-01: `GET /api/instructor/problems/pending` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Pending problems retrieved successfully`; data: `List<ProblemSummaryDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `code`, `title`, `domainCode`, `domainName`, `difficultyLevel`, `sourceType`, `status`, `strategicTheme`, `researchArea`, `proposedByGroupId`, `proposedByGroupNo`, `proposedByGroupName`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Chỉ lấy SELF_PROPOSED/PENDING_REVIEW có proposedByGroup.instructor là người gọi.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c21-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorProblemController.java:33).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C21-02. reviewProblem

- [ ] C21-02: `PATCH /api/instructor/problems/{problemId}/review` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| problemId | path | Long | Có | — |

Body JSON: [ReviewProblemRequest](#dto-reviewproblemrequest); payload chính minh họa trong luồng F07.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Problem reviewed successfully`; data: `ProblemDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `code`, `title`, `statement`, `strategicTheme`, `researchArea`, `difficultyLevel`, `expectedOutput`, `ownerLab`, `suggestedCourses`, `driveFolderLink`, `sourceType`, `status`, `domain`, `proposedByGroup`, `proposedByStudent`, `reviewComment`, `reviewedBy`, `reviewedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Kiểm type/status và nhóm được giao. APPROVED chuyển OFFICIAL/ACTIVE; REJECTED cần comment và bỏ selection nếu đang chọn. Lưu người duyệt, thời gian và comment như luồng admin review.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c21-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorProblemController.java:40).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c22"></a>

## C22. InstructorSubmissionController

Luồng và fixture: **F12** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorSubmissionController.java). Luồng xử lý: [MilestoneSubmissionServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneSubmissionServiceImpl.java).

Chỉ INSTRUCTOR; account ACTIVE và có profile. Đây là đọc submission legacy, không mở lại các API submit đã bị comment.

### C22-01. getSubmissions

- [ ] C22-01: `GET /api/instructor/submissions` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| term | query | String | Không | — |
| courseCode | query | String | Không | — |
| milestoneId | query | Long | Không | — |
| groupId | query | Long | Không | — |
| status | query | SubmissionStatus | Không | — |
| late | query | Boolean | Không | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Instructor submissions retrieved successfully`; data: `List<MilestoneSubmissionDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `milestoneId`, `groupId`, `submittedBy`, `fileUrl`, `comments`, `submittedAt`, `late`, `status`, `version`, `score`, `maxScore`, `feedback`, `gradedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Chỉ lấy submission khi cả milestone.owner và group.assignedInstructor đều là người gọi. Lọc term/course đã trim, không phân biệt hoa thường; milestoneId/groupId/status/late tùy chọn. Trả mảng, không paging. gradeMaxScore fallback milestone.maxScore khi grade.maxScore null.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c22-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorSubmissionController.java:29).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c23"></a>

## C23. MentorAvailabilityController

Luồng và fixture: **F09** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorAvailabilityController.java). Luồng xử lý: [MentorAvailabilityServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MentorAvailabilityServiceImpl.java), [MentorScheduleTimePolicy](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MentorScheduleTimePolicy.java).

Chỉ MENTOR, resolve profile qua email. Slot đúng 60 phút, bắt đầu phút 00/30, không giây/nano, cùng ngày Asia/Ho_Chi_Minh; link dạng https://meet.google.com/xxx-xxxx-xxx bằng chữ thường. Không tự thêm điều kiện future vào create: booking kiểm tra future riêng.

### C23-01. createSlot

- [ ] C23-01: `POST /api/mentor/availability` — kết quả: NOT RUN.

Body JSON: [CreateAvailabilitySlotRequest](#dto-createavailabilityslotrequest); payload chính minh họa trong luồng F09.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Availability slot created successfully`; data: `MentorAvailabilitySlotDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `mentorId`, `mentorCode`, `mentorName`, `startAt`, `endAt`, `meetLink`, `note`, `status`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Kiểm time/link, không overlap slot chưa CANCELED của cùng mentor; tạo AVAILABLE, HTTP 200.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c23-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorAvailabilityController.java:36).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C23-02. listSlots

- [ ] C23-02: `GET /api/mentor/availability` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Availability slots retrieved successfully`; data: `List<MentorAvailabilitySlotDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `mentorId`, `mentorCode`, `mentorName`, `startAt`, `endAt`, `meetLink`, `note`, `status`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Chỉ slot của mình, startAt DESC, có cả lịch sử trạng thái.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c23-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorAvailabilityController.java:48).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C23-03. listMySlots

- [ ] C23-03: `GET /api/mentor/availability/me` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Availability slots retrieved successfully`; data: `List<MentorAvailabilitySlotDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `mentorId`, `mentorCode`, `mentorName`, `startAt`, `endAt`, `meetLink`, `note`, `status`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Alias /me gọi cùng listSlots.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c23-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorAvailabilityController.java:57).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C23-04. updateSlot

- [ ] C23-04: `PATCH /api/mentor/availability/{id}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body JSON: [UpdateAvailabilitySlotRequest](#dto-updateavailabilityslotrequest); payload chính minh họa trong luồng F09.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Availability slot updated successfully`; data: `MentorAvailabilitySlotDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `mentorId`, `mentorCode`, `mentorName`, `startAt`, `endAt`, `meetLink`, `note`, `status`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Slot phải thuộc mentor; BOOKED không được sửa. Time không truyền giữ cũ; link truyền vào phải hợp lệ; kiểm overlap loại chính slot; note chỉ cập nhật khi non-null.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c23-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorAvailabilityController.java:65).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C23-05. cancelSlot

- [ ] C23-05: `DELETE /api/mentor/availability/{id}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Availability slot canceled successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Slot phải thuộc mentor và không BOOKED. Chuyển CANCELED, không xóa vật lý; trả data=null.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c23-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorAvailabilityController.java:78).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c24"></a>

## C24. MentorMeetingReportController

Luồng và fixture: **F09** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorMeetingReportController.java). Luồng xử lý: [MentorMeetingReportServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MentorMeetingReportServiceImpl.java).

Chỉ MENTOR. Bao gồm nhóm hiện được gán hoặc nhóm có meeting lịch sử của mentor; giữ lịch sử khi nhóm đổi mentor. Thời gian báo cáo theo ICT.

### C24-01. listReportTerms

- [ ] C24-01: `GET /api/mentor/meeting-reports/terms` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Meeting report terms retrieved successfully`; data: `List<MentorReportTermDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `code`, `status`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Union mã kỳ từ nhóm và meeting của mentor; chỉ academic term tồn tại, sắp createdAt DESC, id DESC; trả termCode/status.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c24-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorMeetingReportController.java:33).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C24-02. exportReport

- [ ] C24-02: `GET /api/mentor/meeting-reports/export.xlsx` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| term | query | String | Không | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; file nhị phân, không JSON. Kiểm MIME/Content-Disposition và nội dung ở phần dưới.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Kỳ bắt buộc và phải tồn tại. Xuất hai sheet Group Summary (9 cột), Meeting Details (18 cột), gồm link, minh chứng, người gửi, xác nhận, hủy; giữ nhóm chưa có meeting. Sắp course/groupNo, meeting start/id; cell có kiểu dữ liệu, hyperlink, freeze/filter. Tên file mentor-meeting-report-{safeTerm}.xlsx.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c24-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorMeetingReportController.java:42).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c25"></a>

## C25. MilestoneGradeController

Luồng và fixture: **F12** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeController.java). Luồng xử lý: [MilestoneGradeServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneGradeServiceImpl.java).

gradeSubmission/updateGrade nằm trong block comment Java: không phải API đang hoạt động. Ba route thực tế gồm hai đọc grade legacy và một bulk trả 404. Read cho ADMIN, leader/member hoặc instructor được gán; mentor/người ngoài trả 403.

### C25-01. getGradeBySubmissionId

- [ ] C25-01: `GET /api/milestone-submissions/{submissionId}/grades` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| submissionId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Milestone grade retrieved successfully`; data: `MilestoneGradeDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `submissionId`, `score`, `maxScore`, `feedback`, `instructorId`, `gradedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tra submission, kiểm quyền group rồi tra grade; thiếu submission/grade trả 404; trả MilestoneGradeDto.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c25-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeController.java:62).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C25-02. getGradesByGroupId

- [ ] C25-02: `GET /api/milestone-submissions/groups/{groupId}/grades` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Grades retrieved successfully`; data: `List<MilestoneGradeDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `submissionId`, `score`, `maxScore`, `feedback`, `instructorId`, `gradedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tra group và quyền; trả grade của các submission thuộc nhóm, không phải grade matrix mới.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c25-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeController.java:74).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C25-03. bulkGrade

- [ ] C25-03: `POST /api/milestone-submissions/bulk-grade` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **404**; envelope code **404**; message `Bulk grading not supported`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Luôn trả 404, envelope code=404, message="Bulk grading not supported" sau security; không chấm điểm.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c25-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeController.java:86).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c26"></a>

## C26. MilestoneGradeMatrixController

Luồng và fixture: **F11** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java). Luồng xử lý: [MilestoneGradeMatrixServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneGradeMatrixServiceImpl.java).

Chấm theo group/milestone và contribution của từng active member. Không yêu cầu tổng contribution bằng 100: mỗi thành viên có phần trăm 0–100. Grade scope cần milestone ACTIVE, weight>0, group ACTIVE, cùng term/course và instructor. Student write áp dụng kỳ readonly. Service có transaction; revision FOR UPDATE ở luồng contribution/agreement và kiểm agreement khi chấm lần đầu, không suy ra mọi lần sửa grade đều khóa revision/group.

### C26-01. upsertGroupGrade

- [ ] C26-01: `PUT /api/instructor/milestones/{milestoneId}/groups/{groupId}/grade` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| milestoneId | path | Long | Có | — |
| groupId | path | Long | Có | — |

Body JSON: [UpsertMilestoneGroupGradeRequest](#dto-upsertmilestonegroupgraderequest); payload chính minh họa trong luồng F11.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Group milestone grade saved successfully`; data: `MilestoneGroupGradeDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `milestoneId`, `groupId`, `score`, `maxScoreSnapshot`, `weightSnapshot`, `feedback`, `instructorId`, `gradedAt`, `contributionsComplete`, `gradeComplete`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

INSTRUCTOR sở hữu milestone và group; score trong 0..maxScore. Phải có đúng tập contribution active members, không thiếu/thừa; lần chấm đầu cần tất cả AGREE, lần sửa không yêu cầu lại. Lưu maxScore/weight snapshots, người chấm, thời gian, feedback. Individual score: groupScore × percent ÷ 100 làm tròn HALF_UP scale 8, sau đó HALF_UP scale 4. Notify khi điểm/feedback thay đổi.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c26-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:42).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C26-02. upsertContributions

- [ ] C26-02: `PUT /api/groups/{groupId}/milestones/{milestoneId}/contributions` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| milestoneId | path | Long | Có | — |

Body JSON: [UpsertMilestoneContributionsRequest](#dto-upsertmilestonecontributionsrequest); payload chính minh họa trong luồng F11.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Milestone contributions saved successfully`; data: `List<MilestoneMemberScoreDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `milestoneId`, `groupId`, `studentId`, `studentCode`, `studentName`, `contributionPercent`, `calculatedScore`, `maxScoreSnapshot`, `weightSnapshot`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Leader nộp contribution cho đủ active members, mỗi người đúng một lần; trùng/thiếu/người ngoài trả 400. Đã có grade trả 409. Upsert scores, xóa calculated score/snapshots; tăng revision, xóa agreement cũ, notify revision; trả member scores sắp MSSV.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c26-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:54).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C26-03. getGroupGradeMatrix

- [ ] C26-03: `GET /api/groups/{groupId}/grades` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Group grade matrix retrieved successfully`; data: `GroupGradeMatrixDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `groupId`, `groupName`, `groupNo`, `term`, `courseCode`, `milestones`, `members`, `complete`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Read cho ADMIN, instructor được gán hoặc student member/leader. Chưa gán instructor trả 409; trả 409 do owner lệch chỉ khi KHÔNG có milestone visible của instructor hiện tại nhưng CÓ milestone visible của instructor khác trong scope. Columns loại ARCHIVED/INACTIVE, kèm agreement và completeness. Mỗi hạng tử weighted = (individualScore ÷ maxScore, HALF_UP scale 8) × 10 × weight ÷ 100, HALF_UP scale 4; tổng scale 4. Matrix complete cần ít nhất một milestone và mọi column gradeComplete. Zero columns: member row có thể complete=true/total=0.0000 nhưng matrix complete=false. Zero active members: các phép allMatch về member scores là true; không tự thêm điều kiện non-empty. Đã có grade thì agreement summary trả AGREED, approved=required.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c26-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:66).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C26-04. exportInstructorGradesCsv

- [ ] C26-04: `GET /api/instructor/grades/export.csv` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| term | query | String | Không | — |
| courseCode | query | String | Không | — |
| groupId | query | Long | Không | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; file nhị phân, không JSON. Kiểm MIME/Content-Disposition và nội dung ở phần dưới.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

INSTRUCTOR export CSV theo term/course/group và các milestone đúng scope. UTF-8 BOM, CRLF; cột group/student, contribution và individual score mỗi milestone, finalTotal/complete. Java dùng csvCell nhưng header milestone được escape title trước rồi mới nối hậu tố contribution/score ngoài cell đã quote; cần test title có dấu phẩy/nháy, không khẳng định mọi header đều là CSV chuẩn. Tên file grades-{term hoặc all-terms}-{course hoặc all-courses}.csv.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c26-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:76).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C26-05. upsertContributionAgreement

- [ ] C26-05: `PUT /api/groups/{groupId}/milestones/{milestoneId}/contribution-agreement` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| milestoneId | path | Long | Có | — |

Body JSON: [UpsertContributionAgreementRequest](#dto-upsertcontributionagreementrequest); payload chính minh họa trong luồng F11.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Contribution response saved successfully`; data: `ContributionAgreementDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `revision`, `status`, `approvedCount`, `requiredCount`, `studentId`, `decision`, `reason`, `respondedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Active member, chưa grade, đã có revision. Nếu đã có REQUEST_CHANGES thì chặn phản hồi tiếp đến revision mới. REQUEST_CHANGES cần reason trim; AGREE đặt reason null. Upsert và notify; đếm active members, trạng thái NOT_SUBMITTED/PENDING/CHANGES_REQUESTED/AGREED.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c26-m5); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:95).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C26-06. exportInstructorGradesXlsx

- [ ] C26-06: `GET /api/instructor/grades/export.xlsx` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| term | query | String | Không | — |
| courseCode | query | String | Không | — |
| groupId | query | Long | Không | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; file nhị phân, không JSON. Kiểm MIME/Content-Disposition và nội dung ở phần dưới.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

INSTRUCTOR export XLSX có layout riêng: sheet RAW, Tên Nhóm, Họ & Tên, MSSV, Email (đuôi FPT), một cột điểm mỗi milestone, Final. Cell số định dạng 0.0###, header bold/frozen, autofilter/autosize; không có merge cell. Final chỉ ghi khi member row complete, ngược lại để trống. Không dùng nguyên layout CSV.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c26-m6); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:106).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c27"></a>

## C27. MilestoneSubmissionController

Luồng và fixture: **F12** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneSubmissionController.java). Luồng xử lý: [MilestoneSubmissionServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneSubmissionServiceImpl.java).

POST/PATCH submission đã bị comment trong Java; chỉ ba GET đang hoạt động. DTO chứa grade legacy, maxScore null fallback milestone.maxScore.

### C27-01. getSubmissionById

- [ ] C27-01: `GET /api/milestone-submissions/{submissionId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| submissionId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Milestone submission retrieved successfully`; data: `MilestoneSubmissionDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `milestoneId`, `groupId`, `submittedBy`, `fileUrl`, `comments`, `submittedAt`, `late`, `status`, `version`, `score`, `maxScore`, `feedback`, `gradedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Submission phải tồn tại. ADMIN, leader/member hoặc instructor HIỆN ĐANG được gán group được đọc; không chỉ dựa vào owner cũ của milestone.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c27-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneSubmissionController.java:61).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C27-02. getSubmissionsByGroupId

- [ ] C27-02: `GET /api/milestone-submissions/groups/{groupId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Milestone submissions retrieved successfully`; data: `List<MilestoneSubmissionDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `milestoneId`, `groupId`, `submittedBy`, `fileUrl`, `comments`, `submittedAt`, `late`, `status`, `version`, `score`, `maxScore`, `feedback`, `gradedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Group phải tồn tại, áp cùng quyền đọc; trả submissions của group.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c27-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneSubmissionController.java:73).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C27-03. getSubmissionsByMilestoneId

- [ ] C27-03: `GET /api/milestone-submissions/milestones/{milestoneId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| milestoneId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Milestone submissions retrieved successfully`; data: `List<MilestoneSubmissionDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `milestoneId`, `groupId`, `submittedBy`, `fileUrl`, `comments`, `submittedAt`, `late`, `status`, `version`, `score`, `maxScore`, `feedback`, `gradedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Chỉ INSTRUCTOR sở hữu milestone; lọc thêm group.instructor hiện tại là người gọi, kể cả sau khi nhóm được chuyển sang instructor khác.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c27-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneSubmissionController.java:85).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c28"></a>

## C28. NotificationController

Luồng và fixture: **F13** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/NotificationController.java). Luồng xử lý: [NotificationServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/NotificationServiceImpl.java).

Mọi role authenticated với account ACTIVE, chỉ dữ liệu của recipient hiện tại. Khi gửi: distinct recipients ACTIVE, dedup theo recipient/eventKey; lưu DB rồi push STOMP sau commit tới /user/queue/notifications. Realtime lỗi không rollback nghiệp vụ. action.key/params có fallback dữ liệu legacy.

### C28-01. getNotifications

- [ ] C28-01: `GET /api/notifications` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| page | query | int | Không | `0` |
| size | query | int | Không | `20` |
| unreadOnly | query | boolean | Không | `false` |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Notifications retrieved successfully`; data: `PageResponse<NotificationDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): PageResponse: content/page/number/size/numberOfElements/totalElements/totalPages/hasNext/hasPrevious. Mỗi content item: `id`, `type`, `title`, `body`, `actionUrl`, `entityType`, `entityId`, `payload`, `action`, `read`, `readAt`, `createdAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Page mặc định 0, size 20; page<0 hoặc size≤0 trả 400; size>100 được clamp 100. unreadOnly tùy chọn; sắp createdAt DESC, id DESC.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c28-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/NotificationController.java:25).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C28-02. getUnreadCount

- [ ] C28-02: `GET /api/notifications/unread-count` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Unread count retrieved successfully`; data: `Long`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Giá trị số, không phải object.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Đếm bản ghi readAt=null của recipient.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c28-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/NotificationController.java:37).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C28-03. markAsRead

- [ ] C28-03: `PATCH /api/notifications/{id}/read` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Notification marked as read successfully`; data: `NotificationDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `type`, `title`, `body`, `actionUrl`, `entityType`, `entityId`, `payload`, `action`, `read`, `readAt`, `createdAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tra notification và ownership (sai người trả 403); chỉ đặt readAt lần đầu, gọi lại không đổi thời gian.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c28-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/NotificationController.java:44).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C28-04. markAllAsRead

- [ ] C28-04: `PATCH /api/notifications/read-all` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `All notifications marked as read successfully`; data: `Long`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Giá trị số, không phải object.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Mark tất cả của recipient; trả số unread còn lại, không phải số bản ghi vừa cập nhật.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c28-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/NotificationController.java:54).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c29"></a>

## C29. ProblemController

Luồng và fixture: **F07** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemController.java). Luồng xử lý: [ProblemBankServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemBankServiceImpl.java).

Authenticated, service yêu cầu account tồn tại. Java không ẩn problem theo role; không tự lọc OFFICIAL/ACTIVE khi không truyền status/sourceType.

### C29-01. getProblems

- [ ] C29-01: `GET /api/problems` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| page | query | int | Không | `0` |
| size | query | int | Không | `10` |
| search | query | String | Không | — |
| domainCode | query | String | Không | — |
| difficulty | query | DifficultyLevel | Không | — |
| expectedOutput | query | String | Không | — |
| sourceType | query | ProblemSourceType | Không | — |
| status | query | ProblemStatus | Không | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Problems retrieved successfully`; data: `PageResponse<ProblemSummaryDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): PageResponse: content/page/number/size/numberOfElements/totalElements/totalPages/hasNext/hasPrevious. Mỗi content item: `id`, `code`, `title`, `domainCode`, `domainName`, `difficultyLevel`, `sourceType`, `status`, `strategicTheme`, `researchArea`, `proposedByGroupId`, `proposedByGroupNo`, `proposedByGroupName`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Page mặc định 0, size 10, giới hạn 1–100. Search code/title/statement, lọc domainCode/difficulty/expectedOutput/sourceType/status. Sắp createdAt DESC, id DESC; trả PageResponse.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c29-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemController.java:36).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C29-02. getProblemById

- [ ] C29-02: `GET /api/problems/{id}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Problem details retrieved successfully`; data: `ProblemDetailDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `code`, `title`, `statement`, `strategicTheme`, `researchArea`, `difficultyLevel`, `expectedOutput`, `ownerLab`, `suggestedCourses`, `driveFolderLink`, `sourceType`, `status`, `domain`, `proposedByGroup`, `proposedByStudent`, `reviewComment`, `reviewedBy`, `reviewedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tra id; thiếu trả 404 "Problem not found with id: {id}". Trả chi tiết domain/proposer/reviewer/time, không tự chặn SELF_PROPOSED/INACTIVE.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c29-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemController.java:80).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c30"></a>

## C30. ProblemCriteriaController

Luồng và fixture: **F04, F07** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemCriteriaController.java). Luồng xử lý: [ProblemBankServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemBankServiceImpl.java).

Authenticated. Danh mục tiêu chí evaluation hiện chỉ đọc, không có CRUD.

### C30-01. getActiveCriteria

- [ ] C30-01: `GET /api/problem-evaluation-criteria` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Active evaluation criteria retrieved successfully`; data: `List<ProblemEvaluationCriteriaDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `code`, `category`, `question`, `suggestion`, `maxScore`, `displayOrder`, `active`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Chỉ active=true, displayOrder ASC; trả id/code/category/question/suggestion/maxScore/displayOrder/active/createdAt/updatedAt.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c30-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemCriteriaController.java:27).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c31"></a>

## C31. ProblemDomainController

Luồng và fixture: **F04, F07** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemDomainController.java). Luồng xử lý: [ProblemBankServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemBankServiceImpl.java).

Authenticated theo security fallback. status có kiểu ProblemStatus enum, không phải chuỗi tự do.

### C31-01. getProblemDomains

- [ ] C31-01: `GET /api/problem-domains` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| search | query | String | Không | — |
| status | query | ProblemStatus | Không | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Problem domains retrieved successfully`; data: `List<ProblemDomainDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `code`, `name`, `description`, `macroDomain`, `subDomain`, `typicalExamples`, `primaryDiscipline`, `supportingDisciplines`, `bestSources`, `studentCapabilities`, `potentialOutputs`, `notes`, `status`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Lọc status tùy chọn; search trim/lowercase trên code/name/description. Không truyền status thì giữ INACTIVE. Sắp createdAt DESC, id DESC; trả mảng không paging.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c31-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemDomainController.java:28).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c32"></a>

## C32. ProblemImportController

Luồng và fixture: **F03** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemImportController.java). Luồng xử lý: [ProblemImportServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemImportServiceImpl.java), [ProblemImportRowExecutorImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemImportRowExecutorImpl.java), [ImportJobSupport](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ImportJobSupport.java).

Chỉ ADMIN. Dùng queue/admission/background chung với imports. CSV unified hoặc XLSX có domain/problem sheets và alias header. Validate domain trước problem; code trùng trong file không phân biệt hoa thường là lỗi, mã đã có ở DB thì upsert.

### C32-01. importProblemBank

- [ ] C32-01: `POST /api/imports/problem-bank` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| file | multipart | MultipartFile | Có | — |

**Response mong đợi:** HTTP **202**; envelope code **202**; message `Problem bank import job queued successfully`; data: `ImportResultResponse`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `batchId`, `targetType`, `status`, `fileName`, `fileType`, `totalRows`, `successRows`, `failedRows`, `startedAt`, `finishedAt`, `createdGroups`, `skippedGroups`, `leaderFallbackWarnings`, `assignedMentors`, `mentorAssignmentWarnings`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Multipart file, HTTP 202, target PROBLEM_BANK. Domain code≤50/name≤255; problem code≤100/title≤255, statement/difficulty bắt buộc. Difficulty nhận BEGINNER/INTERMEDIATE/ADVANCED và alias easy/medium/hard/beginer; status chỉ ACTIVE/INACTIVE. Domain không tồn tại và không valid trong batch: code thực tế clear reference, không tự fail row. Mỗi dòng save REQUIRES_NEW, problem luôn OFFICIAL, metadata optional blank thành null; errors/counts đọc qua imports/{batchId}.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c32-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemImportController.java:27).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c33"></a>

## C33. ProfileController

Luồng và fixture: **F01** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProfileController.java). Luồng xử lý: [ProfileServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProfileServiceImpl.java).

Authenticated. Khi mustChangePassword=true, gate chỉ miễn auth/me, auth/logout, profile/me/password; GET profile/me không tự được miễn. DTO không lộ password hash.

### C33-01. getMyProfile

- [ ] C33-01: `GET /api/profile/me` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Profile retrieved successfully`; data: `SelfProfileResponse`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `email`, `role`, `status`, `mustChangePassword`, `createdAt`, `updatedAt`, `lastLoginAt`, `studentProfile`, `mentorProfile`, `instructorProfile`, `groupMemberships`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Tra account ACTIVE; trả account và profile theo role, memberships cho student. Không có account trả 401, inactive trả 403 trong service.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c33-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProfileController.java:31).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C33-02. updateMyProfile

- [ ] C33-02: `PATCH /api/profile/me` — kết quả: NOT RUN.

Body JSON: [UpdateSelfProfileRequest](#dto-updateselfprofilerequest); payload chính minh họa trong luồng F01.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Profile updated successfully`; data: `SelfProfileResponse`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `email`, `role`, `status`, `mustChangePassword`, `createdAt`, `updatedAt`, `lastLoginAt`, `studentProfile`, `mentorProfile`, `instructorProfile`, `groupMemberships`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Sửa profile đúng role; ADMIN trả 403, thiếu profile trả 404. Chỉ cập nhật field non-null; fullName trim không trắng; phone/text optional trim, blank thành null. Giữ maxlength và yearsOfExperience≥0 của DTO. Không đổi email/role/code/status.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c33-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProfileController.java:38).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C33-03. changeMyPassword

- [ ] C33-03: `PATCH /api/profile/me/password` — kết quả: NOT RUN.

Body JSON: [ChangeOwnPasswordRequest](#dto-changeownpasswordrequest); payload chính minh họa trong luồng F01.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Password changed successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Kiểm currentPassword bằng BCrypt; sai trả 400. Hash mật khẩu mới, đặt mustChangePassword=false, revoke mọi refresh token; không blacklist access token hiện tại.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c33-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProfileController.java:48).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c34"></a>

## C34. StudentAccountImportController

Luồng và fixture: **F03** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentAccountImportController.java). Luồng xử lý: [StudentAccountImportServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/StudentAccountImportServiceImpl.java), [StudentAccountImportValidationService](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/StudentAccountImportValidationService.java), [ImportJobSupport](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ImportJobSupport.java).

Chỉ ADMIN. Luồng roster riêng, không tạo group/membership. Cột GroupName map student.className. Đúng năm header RollNumber/Fullname/Email/SubjectCode/GroupName; SubjectCode chỉ EXE101/EXE201.

### C34-01. importStudentAccounts

- [ ] C34-01: `POST /api/imports/student-accounts` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| file | multipart | MultipartFile | Có | — |

**Response mong đợi:** HTTP **202**; envelope code **202**; message `Student account import job queued successfully`; data: `ImportResultResponse`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `batchId`, `targetType`, `status`, `fileName`, `fileType`, `totalRows`, `successRows`, `failedRows`, `startedAt`, `finishedAt`, `createdGroups`, `skippedGroups`, `leaderFallbackWarnings`, `assignedMentors`, `mentorAssignmentWarnings`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

File CSV/XLSX, HTTP 202, target STUDENT_ACCOUNT. Validate required/length/email/trùng. Email và MSSV cùng khớp một student có cả student.status và account.status INACTIVE, account.role=STUDENT mới được đưa vào danh sách reactivate, giữ hash; duplicate ACTIVE là lỗi. Code uppercase, email lowercase; tài khoản mới dùng random password mạnh, mustChangePassword=false; không trả password.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c34-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentAccountImportController.java:39).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C34-02. getTemplate

- [ ] C34-02: `GET /api/imports/templates/student-accounts` — kết quả: NOT RUN.

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; file nhị phân, không JSON. Kiểm MIME/Content-Disposition và nội dung ở phần dưới.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Download template CSV/XLSX có đúng năm cột roster; filename/content type ở phụ lục; không tạo job.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c34-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentAccountImportController.java:56).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c35"></a>

## C35. StudentController

Luồng và fixture: **F04** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentController.java). Luồng xử lý: [StudentServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/StudentServiceImpl.java).

Chỉ STUDENT; API detail ẩn target student/account inactive bằng 404. API ungrouped chỉ lọc student.status ACTIVE, không kiểm account.status; không áp quy tắc detail cho list. Ungrouped là chưa có nhóm trong term/course, không phải chưa thuộc nhóm nào trên hệ thống.

### C35-01. getStudentById

- [ ] C35-01: `GET /api/students/{id}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| id | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Student profile retrieved successfully`; data: `StudentProfileDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `studentCode`, `fullName`, `email`, `phone`, `dateOfBirth`, `gender`, `address`, `major`, `cohort`, `className`, `status`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Student được đọc profile ACTIVE của student khác theo profile id. Role khác trả 403; target inactive/thiếu account trả 404.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c35-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentController.java:25).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C35-02. getUngroupedStudents

- [ ] C35-02: `GET /api/students/ungrouped` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| term | query | String | Không | — |
| courseCode | query | String | Không | — |
| search | query | String | Không | — |
| page | query | int | Không | `0` |
| size | query | int | Không | `20` |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Ungrouped students retrieved successfully`; data: `PageResponse<StudentProfileDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): PageResponse: content/page/number/size/numberOfElements/totalElements/totalPages/hasNext/hasPrevious. Mỗi content item: `id`, `studentCode`, `fullName`, `email`, `phone`, `dateOfBirth`, `gender`, `address`, `major`, `cohort`, `className`, `status`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

term/course optional ở chữ ký HTTP nhưng service bắt buộc; truyền nguyên giá trị tới query, không trim/uppercase trong service. Page≥0, size>0, Java không cap 100. Query chỉ yêu cầu student ACTIVE, không có điều kiện account ACTIVE; không có membership cùng scope; search code/fullName/email; sắp createdAt DESC, id DESC. Không suy rộng bộ lọc của getStudentById sang endpoint này.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c35-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentController.java:39).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c36"></a>

## C36. StudentGroupGradeController

Luồng và fixture: **F12** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentGroupGradeController.java). Luồng xử lý: [MilestoneGradeServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneGradeServiceImpl.java).

Average từ legacy submission grades, không phải member total của grade matrix. ADMIN/leader/member/instructor được gán được đọc; mentor không được đọc.

### C36-01. calculateAverageGradeForGroup

- [ ] C36-01: `GET /api/student-groups/{groupId}/average-grade` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Average grade calculated successfully`; data: `AverageGradeDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `averageGrade`, `average`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Group phải tồn tại. Tính sum(score × weight)/sum(weight), bỏ milestone INACTIVE hoặc weight null; denominator=0 trả 0.00; HALF_UP hai số. ARCHIVED không bị loại bởi điều kiện INACTIVE. averageGrade và finalScore cùng giá trị.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c36-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentGroupGradeController.java:24).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

<a id="c37"></a>

## C37. TaskBoardController

Luồng và fixture: **F08** trong playbook.

Nguồn: [controller Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/TaskBoardController.java). Luồng xử lý: [TaskBoardServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/TaskBoardServiceImpl.java), [StudentTermWriteGuard](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/StudentTermWriteGuard.java).

Các alias boards/task-boards tương đương. GET cho student member/mentor được gán; ghi chỉ student leader, kỳ writable. Service có transaction; get/update/delete khóa hàng group, create chỉ findById, không FOR UPDATE. Mỗi nhóm chỉ một default, có thể nhiều board.

### C37-01. getBoards

- [ ] C37-01.1: `GET /api/groups/{groupId}/boards` — kết quả: NOT RUN.
- [ ] C37-01.2: `GET /api/groups/{groupId}/task-boards` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Task boards retrieved successfully`; data: `List<TaskBoardDto>`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): Mảng; mỗi phần tử: `id`, `groupId`, `name`, `description`, `position`, `defaultBoard`, `createdByStudentId`, `archivedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

List non-archived theo position ASC; repository không cam kết id tie-break. Không có board active và chưa có default thì lazy-create Default sau lock/recheck; GET có side effect có chủ ý.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c37-m1); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/TaskBoardController.java:38).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C37-02. createBoard

- [ ] C37-02.1: `POST /api/groups/{groupId}/boards` — kết quả: NOT RUN.
- [ ] C37-02.2: `POST /api/groups/{groupId}/task-boards` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |

Body JSON: [CreateTaskBoardRequest](#dto-createtaskboardrequest); payload chính minh họa trong luồng F08.

**Response mong đợi:** HTTP **201**; envelope code **200**; message `Task board created successfully`; data: `TaskBoardDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `groupId`, `name`, `description`, `position`, `defaultBoard`, `createdByStudentId`, `archivedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Name bắt buộc, trim, tối đa 255; tạo board không default, position=max+1, createdBy là leader. HTTP 201; code trong envelope vẫn theo APIResponse Java.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c37-m2); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/TaskBoardController.java:53).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C37-03. updateBoard

- [ ] C37-03.1: `PATCH /api/groups/{groupId}/boards/{boardId}` — kết quả: NOT RUN.
- [ ] C37-03.2: `PATCH /api/groups/{groupId}/task-boards/{boardId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| boardId | path | Long | Có | — |

Body JSON: [UpdateTaskBoardRequest](#dto-updatetaskboardrequest); payload chính minh họa trong luồng F08.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Task board updated successfully`; data: `TaskBoardDto`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): `id`, `groupId`, `name`, `description`, `position`, `defaultBoard`, `createdByStudentId`, `archivedAt`, `createdAt`, `updatedAt`.

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

PATCH name/description/position/archived/defaultBoard; name không trắng và≤255. Position không có @Min hay kiểm range trong service, không mặc định loại số âm. Không archive default; promote mới phải demote default cũ. Không unset default trực tiếp (400).

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c37-m3); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/TaskBoardController.java:69).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

### C37-04. deleteBoard

- [ ] C37-04.1: `DELETE /api/groups/{groupId}/boards/{boardId}` — kết quả: NOT RUN.
- [ ] C37-04.2: `DELETE /api/groups/{groupId}/task-boards/{boardId}` — kết quả: NOT RUN.

| Input | Vị trí | Kiểu | Bắt buộc theo HTTP | Mặc định |
|---|---|---|---|---|
| groupId | path | Long | Có | — |
| boardId | path | Long | Có | — |

Body: không cần JSON theo chữ ký controller.

**Response mong đợi:** HTTP **200**; envelope code **200**; message `Task board deleted successfully`; data: `Void`.

Các field `data` để kiểm (schema snapshot, không phải dữ liệu đã chạy): null

**Nghiệp vụ, kết quả dữ liệu và nhánh cần kiểm:**

Không xóa default (400). Có bất kỳ task nào, kể cả archived, trả 409. Board rỗng được xóa, HTTP 200 data=null.

Contract: [chữ ký, response và header](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md#c37-m4); [source gốc](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/TaskBoardController.java:87).

- [ ] Happy path và readback/side effect đúng mô tả trên.
- [ ] Nhánh lỗi/validation/ownership phù hợp và không ghi dữ liệu trái phép.
- [ ] Actual HTTP/code/message/data/header đã lưu, không chứa token/password.

## Body JSON theo DTO

Trường bắt buộc dưới đây theo schema snapshot; các điều kiện chéo (profile theo role, slotId hoặc startAt/endAt, owner/group, version, kỳ OPEN) nằm trong playbook và nghiệp vụ từng API. Không gửi tên biến như S1_ID dưới dạng JSON number; thay bằng ID thực. Không dùng dữ liệu mẫu làm tài khoản thật.

<a id="dto-adminchangepasswordrequest"></a>

### AdminChangePasswordRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/AdminChangePasswordRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| email | string (email) | Có | minLength=1 |
| newPassword | string | Có | minLength=6 |

<a id="dto-assigninstructorrequest"></a>

### AssignInstructorRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/AssignInstructorRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| instructorId | integer (int64) | Có | — |

<a id="dto-assignmentorrequest"></a>

### AssignMentorRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/AssignMentorRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| mentorId | integer (int64) | Có | — |

<a id="dto-cancelmeetingrequest"></a>

### CancelMeetingRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CancelMeetingRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| reason | string | Có | minLength=0; maxLength=500 |

<a id="dto-changeownpasswordrequest"></a>

### ChangeOwnPasswordRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ChangeOwnPasswordRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| currentPassword | string | Có | minLength=1 |
| newPassword | string | Có | minLength=6 |

<a id="dto-createacademictermrequest"></a>

### CreateAcademicTermRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateAcademicTermRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| code | string | Có | minLength=0; maxLength=30 |

<a id="dto-createadminuserrequest"></a>

### CreateAdminUserRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateAdminUserRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| email | string (email) | Có | minLength=1 |
| role | string | Có | ADMIN, STUDENT, MENTOR, INSTRUCTOR |
| initialPassword | string | Có | minLength=6 |
| studentProfile | [CreateStudentProfileRequest](#dto-createstudentprofilerequest) | Không | — |
| mentorProfile | [CreateMentorProfileRequest](#dto-creatementorprofilerequest) | Không | — |
| instructorProfile | [CreateInstructorProfileRequest](#dto-createinstructorprofilerequest) | Không | — |

<a id="dto-createstudentprofilerequest"></a>

### CreateStudentProfileRequest

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| studentCode | string | Có | minLength=0; maxLength=50 |
| fullName | string | Có | minLength=0; maxLength=255 |
| phone | string | Không | minLength=0; maxLength=30 |
| dateOfBirth | string (date) | Không | — |
| gender | string | Không | MALE, FEMALE, OTHER |
| address | string | Không | — |
| major | string | Không | minLength=0; maxLength=150 |
| cohort | string | Không | minLength=0; maxLength=50 |
| className | string | Không | minLength=0; maxLength=100 |

<a id="dto-creatementorprofilerequest"></a>

### CreateMentorProfileRequest

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| mentorCode | string | Có | minLength=0; maxLength=50 |
| fullName | string | Có | minLength=0; maxLength=255 |
| phone | string | Không | minLength=0; maxLength=30 |
| jobTitle | string | Không | minLength=0; maxLength=150 |
| company | string | Không | minLength=0; maxLength=150 |
| expertise | string | Không | — |
| yearsOfExperience | integer (int32) | Không | minimum=0 |
| linkedinUrl | string | Không | minLength=0; maxLength=500 |

<a id="dto-createinstructorprofilerequest"></a>

### CreateInstructorProfileRequest

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| instructorCode | string | Có | minLength=0; maxLength=50 |
| fullName | string | Có | minLength=0; maxLength=255 |
| phone | string | Không | minLength=0; maxLength=30 |
| department | string | Không | minLength=0; maxLength=150 |
| expertise | string | Không | — |

<a id="dto-createavailabilityslotrequest"></a>

### CreateAvailabilitySlotRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateAvailabilitySlotRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| startAt | string (date-time) | Có | — |
| endAt | string (date-time) | Có | — |
| meetLink | string | Có | minLength=1; pattern=^https://meet\.google\.com/[a-z]{3}-[a-z]{4}-[a-z]{3}$ |
| note | string | Không | minLength=0; maxLength=500 |

<a id="dto-createchecklistitemrequest"></a>

### CreateChecklistItemRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateChecklistItemRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| title | string | Có | minLength=0; maxLength=500 |

<a id="dto-createcoursemilestonerequest"></a>

### CreateCourseMilestoneRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateCourseMilestoneRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| term | string | Có | minLength=0; maxLength=30 |
| courseCode | string | Có | minLength=0; maxLength=30 |
| title | string | Có | minLength=0; maxLength=255 |
| description | string | Không | — |
| weight | integer (int32) | Không | minimum=0; maximum=100 |
| deadlineAt | string (date-time) | Không | — |
| maxScore | number | Không | minimum=0.01 |
| position | integer (int64) | Không | minimum=0 |

<a id="dto-creategrouprequest"></a>

### CreateGroupRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateGroupRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| term | string | Có | minLength=0; maxLength=30 |
| courseCode | string | Có | minLength=0; maxLength=30 |
| name | string | Có | minLength=0; maxLength=255 |
| projectName | string | Không | minLength=0; maxLength=255 |
| ideaDescription | string | Không | — |
| researchDomain | string | Không | — |
| requiredGpa | number | Không | minimum=0; maximum=4 |
| targetGrade | number | Không | minimum=0; maximum=10 |
| recruitmentNeeds | [array<GroupRecruitmentNeedRequest>](#dto-grouprecruitmentneedrequest) | Không | — |

<a id="dto-grouprecruitmentneedrequest"></a>

### GroupRecruitmentNeedRequest

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| role | string | Có | minLength=1 |
| quantity | integer (int32) | Có | minimum=1; maximum=6 |

<a id="dto-createinvitationrequest"></a>

### CreateInvitationRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateInvitationRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| studentCodeOrEmail | string | Có | minLength=0; maxLength=255 |
| message | string | Không | minLength=0; maxLength=500 |

<a id="dto-createjoinrequestdto"></a>

### CreateJoinRequestDto

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateJoinRequestDto.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| message | string | Không | minLength=0; maxLength=500 |

<a id="dto-createorbookmentormeetingrequest"></a>

### CreateOrBookMentorMeetingRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateOrBookMentorMeetingRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| slotId | integer (int64) | Không | — |
| startAt | string (date-time) | Không | — |
| endAt | string (date-time) | Không | — |
| meetLink | string | Không | minLength=0; maxLength=500; pattern=^https?://.+ |
| note | string | Không | minLength=0; maxLength=500 |
| validRequestShape | boolean | Không | — |
| meetLinkProvidedForDirectMeeting | boolean | Không | — |

<a id="dto-createproblemdomainrequest"></a>

### CreateProblemDomainRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateProblemDomainRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| code | string | Có | minLength=0; maxLength=50 |
| name | string | Có | minLength=0; maxLength=255 |
| description | string | Không | — |
| macroDomain | string | Không | — |
| subDomain | string | Không | — |
| typicalExamples | string | Không | — |
| primaryDiscipline | string | Không | — |
| supportingDisciplines | string | Không | — |
| bestSources | string | Không | — |
| studentCapabilities | string | Không | — |
| potentialOutputs | string | Không | — |
| notes | string | Không | — |
| status | string | Không | ACTIVE, INACTIVE, PENDING_REVIEW, APPROVED, REJECTED, ARCHIVED |

<a id="dto-createproblemrequest"></a>

### CreateProblemRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateProblemRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| domainCode | string | Không | — |
| title | string | Có | minLength=0; maxLength=255 |
| statement | string | Có | minLength=1 |
| code | string | Không | minLength=0; maxLength=100 |
| strategicTheme | string | Không | minLength=0; maxLength=255 |
| researchArea | string | Không | minLength=0; maxLength=255 |
| difficultyLevel | string | Có | BEGINNER, INTERMEDIATE, ADVANCED |
| expectedOutput | string | Không | — |
| ownerLab | string | Không | — |
| suggestedCourses | string | Không | — |
| driveFolderLink | string | Không | minLength=0; maxLength=500 |
| status | string | Không | ACTIVE, INACTIVE, PENDING_REVIEW, APPROVED, REJECTED, ARCHIVED |

<a id="dto-createtaskboardrequest"></a>

### CreateTaskBoardRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateTaskBoardRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| name | string | Có | minLength=0; maxLength=255 |
| description | string | Không | — |

<a id="dto-createtaskcommentrequest"></a>

### CreateTaskCommentRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateTaskCommentRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| content | string | Có | minLength=1 |

<a id="dto-createtaskrequest"></a>

### CreateTaskRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateTaskRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| title | string | Có | minLength=0; maxLength=255 |
| description | string | Không | — |
| status | string | Không | — |
| priority | string | Không | — |
| dueAt | string (date-time) | Không | — |
| assigneeStudentIds | array<integer (int64)> | Không | — |
| boardId | integer (int64) | Không | — |

<a id="dto-googleloginrequest"></a>

### GoogleLoginRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/GoogleLoginRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| idToken | string | Có | minLength=1 |

<a id="dto-loginrequest"></a>

### LoginRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/LoginRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| email | string (email) | Có | minLength=1 |
| password | string | Có | minLength=1 |

<a id="dto-movetaskrequest"></a>

### MoveTaskRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/MoveTaskRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| status | string | Có | — |
| position | integer (int64) | Có | — |
| version | integer (int64) | Có | — |

<a id="dto-proposegroupproblemrequest"></a>

### ProposeGroupProblemRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ProposeGroupProblemRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| title | string | Có | minLength=0; maxLength=255 |
| statement | string | Có | minLength=1 |
| strategicTheme | string | Không | minLength=0; maxLength=255 |
| researchArea | string | Không | minLength=0; maxLength=255 |
| difficultyLevel | string | Có | BEGINNER, INTERMEDIATE, ADVANCED |
| expectedOutput | string | Không | — |
| domainCode | string | Có | minLength=1 |

<a id="dto-refreshtokenrequest"></a>

### RefreshTokenRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/RefreshTokenRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| refreshToken | string | Có | minLength=1 |

<a id="dto-reordertaskrequest"></a>

### ReorderTaskRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ReorderTaskRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| taskId | integer (int64) | Có | — |
| targetStatus | string | Có | minLength=1 |
| targetIndex | integer (int64) | Có | — |

<a id="dto-replacetaskassigneesrequest"></a>

### ReplaceTaskAssigneesRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ReplaceTaskAssigneesRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| assigneeStudentIds | array<integer (int64)> | Có | — |
| version | integer (int64) | Không | — |
| assigneeIds | array<integer (int64)> | Không | — |

<a id="dto-resetuserpasswordrequest"></a>

### ResetUserPasswordRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ResetUserPasswordRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| newPassword | string | Có | minLength=6 |

<a id="dto-reviewproblemrequest"></a>

### ReviewProblemRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ReviewProblemRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| status | string | Có | ACTIVE, INACTIVE, PENDING_REVIEW, APPROVED, REJECTED, ARCHIVED |
| comment | string | Không | — |

<a id="dto-selectproblemrequest"></a>

### SelectProblemRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/SelectProblemRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| problemId | integer (int64) | Có | — |

<a id="dto-submitfeedbackrequest"></a>

### SubmitFeedbackRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/SubmitFeedbackRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| rating | integer (int32) | Có | minimum=1; maximum=5 |
| comment | string | Không | minLength=0; maxLength=2000 |

<a id="dto-submitmeetingevidencerequest"></a>

### SubmitMeetingEvidenceRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/SubmitMeetingEvidenceRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| imageUrl | string | Có | minLength=0; maxLength=2000; pattern=^https?://.+ |

<a id="dto-transferleaderrequest"></a>

### TransferLeaderRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/TransferLeaderRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| studentId | integer (int64) | Có | — |

<a id="dto-updateadminuserrequest"></a>

### UpdateAdminUserRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateAdminUserRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| email | string (email) | Có | minLength=1 |
| status | string | Có | ACTIVE, INACTIVE, LOCKED |
| mustChangePassword | boolean | Có | — |
| studentProfile | [UpdateStudentProfileRequest](#dto-updatestudentprofilerequest) | Không | — |
| mentorProfile | [UpdateMentorProfileRequest](#dto-updatementorprofilerequest) | Không | — |
| instructorProfile | [UpdateInstructorProfileRequest](#dto-updateinstructorprofilerequest) | Không | — |

<a id="dto-updatestudentprofilerequest"></a>

### UpdateStudentProfileRequest

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| studentCode | string | Có | minLength=0; maxLength=50 |
| fullName | string | Có | minLength=0; maxLength=255 |
| phone | string | Không | minLength=0; maxLength=30 |
| dateOfBirth | string (date) | Không | — |
| gender | string | Không | MALE, FEMALE, OTHER |
| address | string | Không | — |
| major | string | Không | minLength=0; maxLength=150 |
| cohort | string | Không | minLength=0; maxLength=50 |
| className | string | Không | minLength=0; maxLength=100 |

<a id="dto-updatementorprofilerequest"></a>

### UpdateMentorProfileRequest

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| mentorCode | string | Có | minLength=0; maxLength=50 |
| fullName | string | Có | minLength=0; maxLength=255 |
| phone | string | Không | minLength=0; maxLength=30 |
| jobTitle | string | Không | minLength=0; maxLength=150 |
| company | string | Không | minLength=0; maxLength=150 |
| expertise | string | Không | — |
| yearsOfExperience | integer (int32) | Không | minimum=0 |
| linkedinUrl | string | Không | minLength=0; maxLength=500 |

<a id="dto-updateinstructorprofilerequest"></a>

### UpdateInstructorProfileRequest

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| instructorCode | string | Có | minLength=0; maxLength=50 |
| fullName | string | Có | minLength=0; maxLength=255 |
| phone | string | Không | minLength=0; maxLength=30 |
| department | string | Không | minLength=0; maxLength=150 |
| expertise | string | Không | — |

<a id="dto-updateavailabilityslotrequest"></a>

### UpdateAvailabilitySlotRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateAvailabilitySlotRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| startAt | string (date-time) | Không | — |
| endAt | string (date-time) | Không | — |
| meetLink | string | Không | pattern=^https://meet\.google\.com/[a-z]{3}-[a-z]{4}-[a-z]{3}$ |
| note | string | Không | minLength=0; maxLength=500 |

<a id="dto-updatebackupschedulerequest"></a>

### UpdateBackupScheduleRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateBackupScheduleRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| enabled | boolean | Có | — |
| cronExpression | string | Có | minLength=1 |
| timezone | string | Có | minLength=1 |
| retentionDays | integer (int32) | Không | minimum=1; maximum=3650 |

<a id="dto-updatechecklistitemrequest"></a>

### UpdateChecklistItemRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateChecklistItemRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| title | string | Không | minLength=0; maxLength=500 |
| completed | boolean | Không | — |
| position | integer (int32) | Không | — |

<a id="dto-updatecoursemilestonerequest"></a>

### UpdateCourseMilestoneRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateCourseMilestoneRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| title | string | Có | minLength=0; maxLength=255 |
| description | string | Không | — |
| weight | integer (int32) | Không | minimum=0; maximum=100 |
| deadlineAt | string (date-time) | Không | — |
| maxScore | number | Không | minimum=0.01 |
| position | integer (int64) | Không | minimum=0 |
| status | string | Không | ACTIVE, CLOSED, ARCHIVED, INACTIVE |

<a id="dto-updategroupcriteriarequest"></a>

### UpdateGroupCriteriaRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateGroupCriteriaRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| requiredGpa | number | Không | minimum=0; maximum=4 |
| targetGrade | number | Không | minimum=0; maximum=10 |
| recruitmentNeeds | [array<GroupRecruitmentNeedRequest>](#dto-grouprecruitmentneedrequest) | Không | — |

<a id="dto-updategrouplockrequest"></a>

### UpdateGroupLockRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateGroupLockRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| isLock | boolean | Có | — |

<a id="dto-updategrouprequest"></a>

### UpdateGroupRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateGroupRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| name | string | Không | minLength=1; maxLength=255 |
| projectName | string | Không | minLength=0; maxLength=255 |
| ideaDescription | string | Không | — |
| researchDomain | string | Không | — |
| requiredGpa | number | Không | minimum=0; maximum=4 |
| targetGrade | number | Không | minimum=0; maximum=10 |
| recruitmentNeeds | [array<GroupRecruitmentNeedRequest>](#dto-grouprecruitmentneedrequest) | Không | — |

<a id="dto-updatementormeetingrequest"></a>

### UpdateMentorMeetingRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateMentorMeetingRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| startAt | string (date-time) | Không | — |
| endAt | string (date-time) | Không | — |
| meetLink | string | Có | minLength=0; maxLength=500; pattern=^https?://.+ |
| note | string | Không | minLength=0; maxLength=500 |

<a id="dto-updateproblemdomainrequest"></a>

### UpdateProblemDomainRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateProblemDomainRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| code | string | Không | minLength=0; maxLength=50 |
| name | string | Không | minLength=0; maxLength=255 |
| description | string | Không | — |
| macroDomain | string | Không | — |
| subDomain | string | Không | — |
| typicalExamples | string | Không | — |
| primaryDiscipline | string | Không | — |
| supportingDisciplines | string | Không | — |
| bestSources | string | Không | — |
| studentCapabilities | string | Không | — |
| potentialOutputs | string | Không | — |
| notes | string | Không | — |
| status | string | Không | ACTIVE, INACTIVE, PENDING_REVIEW, APPROVED, REJECTED, ARCHIVED |

<a id="dto-updateproblemrequest"></a>

### UpdateProblemRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateProblemRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| domainCode | string | Không | — |
| title | string | Không | minLength=0; maxLength=255 |
| statement | string | Không | — |
| code | string | Không | minLength=0; maxLength=100 |
| strategicTheme | string | Không | minLength=0; maxLength=255 |
| researchArea | string | Không | minLength=0; maxLength=255 |
| difficultyLevel | string | Không | BEGINNER, INTERMEDIATE, ADVANCED |
| expectedOutput | string | Không | — |
| ownerLab | string | Không | — |
| suggestedCourses | string | Không | — |
| driveFolderLink | string | Không | minLength=0; maxLength=500 |
| status | string | Không | ACTIVE, INACTIVE, PENDING_REVIEW, APPROVED, REJECTED, ARCHIVED |

<a id="dto-updateproblemstatusrequest"></a>

### UpdateProblemStatusRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateProblemStatusRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| status | string | Có | ACTIVE, INACTIVE, PENDING_REVIEW, APPROVED, REJECTED, ARCHIVED |

<a id="dto-updateselfprofilerequest"></a>

### UpdateSelfProfileRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateSelfProfileRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| fullName | string | Không | minLength=0; maxLength=255 |
| phone | string | Không | minLength=0; maxLength=30 |
| dateOfBirth | string (date) | Không | — |
| gender | string | Không | MALE, FEMALE, OTHER |
| address | string | Không | — |
| major | string | Không | minLength=0; maxLength=150 |
| cohort | string | Không | minLength=0; maxLength=50 |
| className | string | Không | minLength=0; maxLength=100 |
| jobTitle | string | Không | minLength=0; maxLength=150 |
| company | string | Không | minLength=0; maxLength=150 |
| expertise | string | Không | — |
| yearsOfExperience | integer (int32) | Không | minimum=0 |
| linkedinUrl | string | Không | minLength=0; maxLength=500 |
| department | string | Không | minLength=0; maxLength=150 |

<a id="dto-updatetaskboardrequest"></a>

### UpdateTaskBoardRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateTaskBoardRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| name | string | Không | — |
| description | string | Không | — |
| position | integer (int64) | Không | — |
| archived | boolean | Không | — |
| defaultBoard | boolean | Không | — |

<a id="dto-updatetaskcommentrequest"></a>

### UpdateTaskCommentRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateTaskCommentRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| content | string | Có | minLength=1 |

<a id="dto-updatetaskrequest"></a>

### UpdateTaskRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateTaskRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| title | string | Không | minLength=0; maxLength=255 |
| description | string | Không | — |
| priority | string | Không | — |
| dueAt | string (date-time) | Không | — |
| clearDueAt | boolean | Không | — |
| version | integer (int64) | Có | — |

<a id="dto-upsertcontributionagreementrequest"></a>

### UpsertContributionAgreementRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpsertContributionAgreementRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| decision | string | Có | AGREE, REQUEST_CHANGES |
| reason | string | Không | minLength=0; maxLength=1000 |

<a id="dto-upsertmilestonecontributionsrequest"></a>

### UpsertMilestoneContributionsRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpsertMilestoneContributionsRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| items | [array<Item>](#dto-item) | Có | minItems=1 |

<a id="dto-item"></a>

### Item

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| studentId | integer (int64) | Có | — |
| contributionPercent | number | Có | minimum=0; maximum=100 |

<a id="dto-upsertmilestonegroupgraderequest"></a>

### UpsertMilestoneGroupGradeRequest

[DTO Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpsertMilestoneGroupGradeRequest.java).

| Field | Kiểu | Required schema | Ràng buộc / enum |
|---|---|---|---|
| score | number | Có | minimum=0 |
| feedback | string | Không | minLength=0; maxLength=5000 |

## Tổng kết lượt test

| Chỉ số | Kết quả người kiểm thử điền |
|---|---|
| HTTP routes đã chạy / 192 | __ |
| PASS / FAIL / BLOCKED / NOT RUN | __ / __ / __ / __ |
| Controller đã đủ positive + negative + scope + side effects / 37 | __ |
| Restore chạy trên DB disposable | Có / Không / BLOCKED |
| Google end-to-end | PASS / FAIL / BLOCKED |
| STOMP nhận MESSAGE sau commit | PASS / FAIL / BLOCKED |
| Legacy fixture đủ dữ liệu | Có / Không |

Không ký nghiệm thu controller còn case FAIL/BLOCKED hoặc thiếu alias chưa chạy.
