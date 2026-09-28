# Rà soát lần 2 — tài liệu nghiệp vụ 37 controller

Ngày: 2026-09-25. Java oracle: `D:/f-spark/f-spark-api`; Flowzy: `D:/Flowzy-api`.

## Kết luận

Tài liệu trước lượt này **chưa hoàn toàn đúng với source Java**. Đã rà lại đủ 37 mục controller (192 cặp method–route, tính riêng alias), sửa/bổ sung 18 nhóm diễn giải dưới đây. Quan trọng nhất: validation HTTP của DTO không thể suy ra chỉ từ fallback trong service; khóa hàng DB không đồng nghĩa cờ isLock; mỗi endpoint có tập dữ liệu/guard riêng.

[Đặc tả đã hiệu chỉnh](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md) là baseline hiện tại. [Báo cáo Flowzy đã hiệu chỉnh](D:/Flowzy-api/docs/FLOWZY_CONTROLLER_PARITY_AUDIT.md) vẫn kết luận **chưa tương đương hoàn toàn**: 20 nhóm phát hiện trên 18 controller; 19 controller còn lại chưa được ký nghiệm thu, không phải 19 controller đã chứng minh đúng mọi nhánh.

Lượt này chỉ sửa tài liệu và công cụ kiểm tra tài liệu, không sửa C#/Java nghiệp vụ, frontend hoặc database. Không có kết luận backend đã deploy-ready.

## Phương pháp và mức xác nhận

- Đọc lại controller/DTO, service, guard, query và các helper liên quan; kiểm chéo mô tả quyền, trạng thái, validation, null/default, transaction, side effect và export.
- Phụ lục giữ nguyên trích source của các method đang hoạt động và 102 DTO được chữ ký tham chiếu trực tiếp. Công cụ kiểm tra so nội dung block, không chỉ đếm route. DTO con/enum và service không được bao phủ hoàn toàn chỉ bởi con số 102 này.
- Mỗi dòng dưới đây ghi phạm vi đã đọc lại và kết quả **đối với tài liệu**. “Giữ mô tả” nghĩa chưa thấy cần sửa diễn giải đã kiểm, không phải mọi đường chạy được chứng minh đúng.
- Với các đính chính ảnh hưởng kết luận Flowzy, đọc lại C# tương ứng trước khi giữ/rút phát hiện. Không suy ra mọi khác biệt đều là lỗi Java; source Java là oracle cho yêu cầu clone, không là chứng nhận thiết kế tối ưu.

## Bảng kiểm 37 controller

| # | Controller / đặc tả | HTTP | Kết quả tài liệu | Phạm vi kiểm tra lại và bằng chứng chính |
|---|---|---:|---|---|
| 1 | [AcademicTermController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c01) | 1 | Giữ mô tả | listAvailableTerms: kỳ khả dụng, không đồng nhất với danh sách admin; quyền theo URL security. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/AcademicTermServiceImpl.java). |
| 2 | [AdminFeedbackController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c02) | 2 | Giữ mô tả | Filter/page/sort, quyền ADMIN, workbook feedback và dữ liệu người đánh giá. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/FeedbackServiceImpl.java). |
| 3 | [AdminGroupController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c03) | 1 | Giữ mô tả | getAdminGroups: phân trang, search, mapping membership/người phụ trách; không áp filter kỳ OPEN của getGroups. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupServiceImpl.java). |
| 4 | [AdminProblemController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c04) | 6 | Giữ mô tả | Domain ACTIVE theo nhánh, create/update/status/review, enum và trường null. Phát hiện F15 vẫn còn. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemBankServiceImpl.java). |
| 5 | [AdminTermController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c05) | 5 | Giữ mô tả | Create/close idempotency, lock, pending cleanup, snapshot feedback; archive không chờ feedback; delete chỉ kỳ rỗng. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/AcademicTermServiceImpl.java). |
| 6 | [AdminUserController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c06) | 7 | Đã sửa | Change-password theo email không dùng normalizeEmail; kiểm lại profile đúng role, tự disable và revoke tokens. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/AdminUserServiceImpl.java). |
| 7 | [AuthController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c07) | 5 | Đã sửa | Không gán thêm kiểm issuer/exp riêng cho verifier; token-info + audience/email_verified/email. Đối chiếu thêm auth controller, refresh và blacklist. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GoogleTokenVerifierImpl.java). |
| 8 | [BackupController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c08) | 7 | Đã bổ sung | Tên field settings chính xác và side effect GET; kiểm queue/restore, cron, retention, download; không chạy thao tác backup/restore thật trong lượt này. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/BackupServiceImpl.java). |
| 9 | [CourseMilestoneController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c09) | 16 | Đã bổ sung | Update null semantics, title/deadline bắt buộc; update/delete khác create về open-term guard; list/detail visibility không đồng nhất. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/CourseMilestoneServiceImpl.java). |
| 10 | [DashboardController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c10) | 15 | Đã bổ sung | Đếm archived theo từng trường; nextDueAt progress khác TV; completeness dùng mọi membership, không lọc ACTIVE theo tên biến. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/DashboardServiceImpl.java). |
| 11 | [FeedbackController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c11) | 3 | Giữ mô tả | Owner/closed-term/snapshot, submittedAt lần đầu, rating, privacy received và optimistic version. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/FeedbackServiceImpl.java). |
| 12 | [GroupController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c12) | 19 | Đã sửa | List loại CLOSED; transfer leader không kiểm isLock. Kiểm riêng create/update/criteria/remove/leave/assign/lock/timeline. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupServiceImpl.java). |
| 13 | [GroupInvitationController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c13) | 6 | Đã làm rõ | Cancel student khóa hàng và kiểm kỳ nhưng không kiểm isLock; ADMIN bypass theo đúng nhánh; accept vẫn kiểm quota/expiry/membership. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupInvitationServiceImpl.java). |
| 14 | [GroupJoinRequestController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c14) | 9 | Đã làm rõ | Approve unlocked kể cả ADMIN; kỳ writable chỉ non-admin; reject/cancel không kiểm isLock. ACCEPTED/CANCELED, không APPROVED/CANCELLED. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupJoinRequestServiceImpl.java). |
| 15 | [GroupMeetingController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c15) | 8 | Đã sửa; rút một nhận định F08 | DTO HTTP bắt buộc meetLink; chỉ booking bắt buộc future, direct mentor create/update không có future guard. Kiểm quota/evidence/cancel/confirm. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MeetingServiceImpl.java). |
| 16 | [GroupProblemController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c16) | 6 | Đã bổ sung | Sinh mã SP và giữ nguyên title/statement; transaction selection, owner/type/status của update/delete; bổ sung bằng chứng F14. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupProblemServiceImpl.java). |
| 17 | [GroupRecruitmentRoleController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c17) | 1 | Giữ mô tả | Catalog 26 roles theo thứ tự enum, category và nhãn VI/EN; controller trực tiếp map enum. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/enums/RecruitmentRole.java). |
| 18 | [GroupTaskController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c18) | 19 | Đã sửa | Version bắt buộc; assignee là current member không phải điều kiện ACTIVE; query my-tasks không search/membership join; cap100; move/comment no-op; dueAt chỉ kiểm khi đổi. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupTaskServiceImpl.java). |
| 19 | [ImportController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c19) | 7 | Giữ mô tả | Queue/admission/startup/merge batch; REQUIRES_NEW từng dòng; student/mentor, group scope/leader fallback, error page và templates. Không coi partial success là rollback toàn file. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ImportServiceImpl.java). |
| 20 | [InstructorGroupBoardController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c20) | 2 | Giữ mô tả | OPEN/ACTIVE/có member; search thành viên, course count scope, claim lại no-op. F13 chưa được sửa runtime. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/InstructorGroupBoardServiceImpl.java). |
| 21 | [InstructorProblemController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c21) | 2 | Giữ mô tả | Pending proposal theo instructor hiện tại; review sở hữu/scope/trạng thái, không suy rộng quyền ADMIN. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemBankServiceImpl.java). |
| 22 | [InstructorSubmissionController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c22) | 1 | Giữ mô tả | List instructor đòi đúng owner milestone và instructor hiện đang được gán group; lọc term/course/status. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneSubmissionServiceImpl.java). |
| 23 | [MentorAvailabilityController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c23) | 5 | Giữ mô tả | Alias me, ownership, BOOKED chặn sửa/hủy; time policy 60 phút/00 hoặc30/cùng ngày; Google Meet regex khác meeting. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MentorAvailabilityServiceImpl.java). |
| 24 | [MentorMeetingReportController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c24) | 2 | Giữ mô tả | Terms/scope mentor, 2 sheet, lịch sử meeting, typed date giờ Việt Nam và filename/header. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MentorMeetingReportServiceImpl.java). |
| 25 | [MilestoneGradeController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c25) | 3 | Giữ mô tả | Chỉ GET và bulk 404 đang hoạt động; quyền legacy group, maxScore fallback; không mở lại write bị comment. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneGradeServiceImpl.java). |
| 26 | [MilestoneGradeMatrixController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c26) | 6 | Đã sửa | Owner conflict có điều kiện; scope khóa revision, HALF_UP scale8→4, zero-member/zero-column, agreement đã grade, RAW XLSX/Final trống và CSV header quirk. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneGradeMatrixServiceImpl.java). |
| 27 | [MilestoneSubmissionController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c27) | 3 | Giữ mô tả | Ba GET hoạt động; quyền current assigned instructor chứ không owner cũ đơn thuần. F19 vẫn cần regression test và sửa. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneSubmissionServiceImpl.java). |
| 28 | [NotificationController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c28) | 4 | Giữ mô tả | Recipient ACTIVE/dedup, after-commit STOMP; read-all HTTP trả unread còn lại chứ không số dòng service vừa cập nhật. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/NotificationServiceImpl.java). |
| 29 | [ProblemController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c29) | 2 | Giữ mô tả | Search/filter/page/sort và detail; không tự thêm OFFICIAL/ACTIVE theo role; invalid enum cần so binding. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemBankServiceImpl.java). |
| 30 | [ProblemCriteriaController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c30) | 1 | Giữ mô tả | Chỉ criteria active, displayOrder, đủ trường DTO; không suy ra CRUD. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemBankServiceImpl.java). |
| 31 | [ProblemDomainController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c31) | 1 | Giữ mô tả | Status optional enum, search, thứ tự createdAt/id; không tự bỏ INACTIVE. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemBankServiceImpl.java). |
| 32 | [ProblemImportController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c32) | 1 | Giữ mô tả | Domain trước problem, duplicate trong file/upsert DB, unknown domain clear, difficulty alias, metadata null, từng dòng REQUIRES_NEW. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemImportServiceImpl.java). |
| 33 | [ProfileController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c33) | 3 | Giữ mô tả | ACTIVE, đúng role, PATCH non-null, trim/blank→null, đổi password revoke refresh; không blacklist access token hiện tại. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProfileServiceImpl.java). |
| 34 | [StudentAccountImportController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c34) | 2 | Đã làm rõ | Reactivation chỉ khi cùng identity và cả student/account INACTIVE, role STUDENT; roster không tạo group; password không trả ra. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/StudentAccountImportServiceImpl.java). |
| 35 | [StudentController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c35) | 2 | Đã sửa | Detail kiểm student+account ACTIVE; ungrouped query chỉ student ACTIVE. Đọc StudentRepository và Flowzy StudentDiscoveryRepository xác nhận không phải lỗi clone ở điểm này. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/StudentServiceImpl.java). |
| 36 | [StudentGroupGradeController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c36) | 1 | Giữ mô tả | Legacy weighted average, bỏ INACTIVE/null weight, mẫu số0→0.00, HALF_UP2; không thay bằng grade matrix. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneGradeServiceImpl.java). |
| 37 | [TaskBoardController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c37) | 8 | Đã sửa | Create không FOR UPDATE; get/update/delete có row lock; position-only sort, position update không range guard; default/lazy-create/delete còn task. [Source](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/TaskBoardServiceImpl.java). |

## Nhật ký đính chính

### R01 — Quy tắc xuyên suốt

- Loại: Làm rõ.
- Mô tả trước: Quyền hiệu lực là giao của URL security, annotation và service.
- Kết quả đọc source: Không tìm thấy @EnableMethodSecurity trong production source. Không mặc định @PreAuthorize được thực thi; xác nhận quyền từ SecurityFilterChain và service. Các controller có annotation hiện vẫn có URL matcher tương ứng.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/configs/SecurityConfig.java:47).

### R02 — 6.7 AdminUser

- Loại: Sửa.
- Mô tả trước: Theo email đã chuẩn hóa.
- Kết quả đọc source: changePasswordByEmail gọi findByEmailIgnoreCase(request.email()) trực tiếp; không trim/lowercase qua normalizeEmail như create/update.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/AdminUserServiceImpl.java:397).

### R03 — 12.1 Group list

- Loại: Bổ sung.
- Mô tả trước: Thiếu điều kiện kỳ.
- Kết quả đọc source: Sau các filter, Java chỉ giữ group.academicTerm null hoặc OPEN; không bao gồm group có kỳ CLOSED.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupServiceImpl.java:157).

### R04 — 12.12 Transfer leader

- Loại: Sửa.
- Mô tả trước: Bắt buộc group unlocked.
- Kết quả đọc source: transferLeader khóa hàng group nhưng KHÔNG gọi GroupMembershipLockGuard.requireUnlocked; phân biệt row lock với isLock.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupServiceImpl.java:722).

### R05 — 13.6 Cancel invitation

- Loại: Sửa.
- Mô tả trước: student branch kiểm group lock/term.
- Kết quả đọc source: Nhánh student khóa hàng group, kiểm kỳ writable/leader; không kiểm isLock. ADMIN không vào nhánh group-row lock này.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupInvitationServiceImpl.java:302).

### R06 — 14.4–14.6 Join requests

- Loại: Làm rõ.
- Mô tả trước: Mô tả guard chung dễ hiểu là mọi action cần unlocked.
- Kết quả đọc source: Approve cần unlocked cả ADMIN và student; writable chỉ non-admin. Reject/cancel không kiểm isLock; reject ADMIN bypass kỳ. responder profile có thể null với ADMIN.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupJoinRequestServiceImpl.java:219).

### R07 — 16.3 Proposal

- Loại: Bổ sung.
- Mô tả trước: Chỉ nói tạo SELF_PROPOSED.
- Kết quả đọc source: Code tự sinh SP-{groupId}-{8 ký tự đầu UUID}; title/statement lấy nguyên request, không tự trim trong service.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupProblemServiceImpl.java:113).

### R08 — 15.3 / F08

- Loại: Rút nhận định sai.
- Mô tả trước: Bỏ meetLink khi PATCH là hợp lệ ở Java, Flowzy Required khác.
- Kết quả đọc source: UpdateMentorMeetingRequest Java có @NotBlank và controller @Valid: bỏ meetLink đều bị validation chặn. Fallback null trong service không làm HTTP field optional. Khác biệt Note null vẫn đúng.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateMentorMeetingRequest.java:10).

### R09 — 15.2 / F07

- Loại: Bổ sung.
- Mô tả trước: Chưa phân biệt future giữa booking và mentor tạo/sửa.
- Kết quả đọc source: Booking cần slot future; mentor create/update không kiểm future, chỉ 60 phút/00 hoặc30/overlap. Flowzy thêm start>now ở cả create/update.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MeetingServiceImpl.java:548).

### R10 — 9.4 Milestone update

- Loại: Bổ sung.
- Mô tả trước: Mô tả chung dễ hiểu PATCH non-null.
- Kết quả đọc source: title/deadline bắt buộc; description/weight được gán kể cả null, khác position/status/maxScore giữ cũ khi null. Update/delete không gọi requireOpenAcademicTerm như create.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/CourseMilestoneServiceImpl.java:190).

### R11 — 37 Task board

- Loại: Sửa.
- Mô tả trước: Khóa group cho mọi thao tác; list position/id.
- Kết quả đọc source: Java get/update/delete dùng findByIdForUpdate, create chỉ findById trong transaction. List chỉ ORDER BY position ASC, không cam kết id tie-break. Update position không có @Min/service range guard.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/TaskBoardServiceImpl.java:108).

### R12 — 7 Auth

- Loại: Sửa.
- Mô tả trước: Verifier kiểm issuer/audience/email.
- Kết quả đọc source: Verifier gọi token-info rồi tự kiểm audience/email_verified/email, không có nhánh issuer/exp riêng.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GoogleTokenVerifierImpl.java:27).

### R13 — 8 Backup

- Loại: Làm rõ.
- Mô tả trước: Field settings viết tắt, timeZone.
- Kết quả đọc source: DTO dùng timezone, cronExpression, retentionDays, các At và updatedByAccountId/Email; GET có thể sync directory.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/BackupScheduleSettingsDto.java:5).

### R14 — 10 Dashboard

- Loại: Bổ sung.
- Mô tả trước: Chưa phân biệt cách tính deadline và tập member.
- Kết quả đọc source: Progress nextDueAt có thể quá khứ, TV >=now; completeness milestone dùng mọi membership, không kiểm ACTIVE.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/DashboardServiceImpl.java:513).

### R15 — 18 GroupTask

- Loại: Sửa.
- Mô tả trước: Version optional, assignee ACTIVE, my-tasks có search, cap không rõ; no-op chưa đủ.
- Kết quả đọc source: Version bắt buộc; current member không có ACTIVE guard; list null khi create→rỗng; my-tasks không search/không kiểm membership riêng; clamp100; move luôn activity; comment chỉ đổi editedAt khi content đổi; due future chỉ khi thay giá trị.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupTaskServiceImpl.java:924).

### R16 — 26 GradeMatrix

- Loại: Sửa.
- Mô tả trước: Owner conflict quá rộng; round4 trực tiếp; XLSX merge; chưa mô tả các tập rỗng.
- Kết quả đọc source: Conflict khi thiếu own visible nhưng có visible scope khác; round8→4; RAW không merge, Final chỉ khi complete; zero columns/members có quy tắc riêng; không mọi grade update khóa revision.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneGradeMatrixServiceImpl.java:456).

### R17 — 34 Roster import

- Loại: Làm rõ.
- Mô tả trước: Chỉ nói account INACTIVE.
- Kết quả đọc source: Danh sách reactivation yêu cầu student và account cùng INACTIVE, role STUDENT, email/code cùng identity.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/StudentAccountImportServiceImpl.java:231).

### R18 — 35 Student

- Loại: Sửa.
- Mô tả trước: Ungrouped kiểm student và account ACTIVE.
- Kết quả đọc source: Ungrouped chỉ lọc student ACTIVE; detail mới kiểm cả hai. Flowzy hiện cũng phân biệt đúng hai nhánh này.
- Bằng chứng: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/repositories/StudentRepository.java:41).

## Ảnh hưởng tới báo cáo Flowzy

1. **Rút một phần F08:** PATCH thiếu meetLink không phải request HTTP hợp lệ của Java; @NotBlank và @Valid chặn trước service. Cả Java và Flowzy đều yêu cầu field này. Giữ F08 cho note omission, cancel/slot và confirm timestamp/guard.
2. **Bổ sung F03:** GET groups Java loại kỳ CLOSED; Flowzy ListAsync chưa lọc điều kiện này.
3. **Bổ sung F07:** Flowzy thêm future-check khi mentor trực tiếp tạo/sửa meeting; Java chỉ có future-check ở nhánh booking. Cần test tách hai nhánh, không thêm rule đồng loạt.
4. **Bổ sung F14:** Java sinh code SP và không trim title/statement proposal; Flowzy không gán Code, đồng thời trim hai trường này.
5. **Thu hẹp diễn giải F10:** Không mọi lần sửa grade đều khóa revision ở Java; scope conflict không phải chỉ cần xuất hiện một milestone của owner khác. Các sai lệch transaction/completeness/rounding còn nguyên.
6. **Không tạo bug giả:** Student ungrouped thiếu account-ACTIVE filter là hành vi cả Java và Flowzy hiện có, không phải lỗi clone. Transfer leader không yêu cầu isLock=false cũng là baseline Java cần giữ khi đối chiếu.

Bằng chứng C#: [GroupOperationsService](D:/Flowzy-api/src/Flowzy.Service/Groups/GroupOperationsService.cs), [GroupMeetingService](D:/Flowzy-api/src/Flowzy.Service/Mentors/GroupMeetingService.cs), [MeetingContracts](D:/Flowzy-api/src/Flowzy.Service/Contracts/MeetingContracts.cs), [StudentDiscoveryRepository](D:/Flowzy-api/src/Flowzy.Repository/Repositories/StudentDiscoveryRepository.cs).

## Kiểm chứng và giới hạn

Chạy `node scripts/Verify-ControllerAudit.cjs` để kiểm route, trích đoạn method/DTO, đường dẫn/anchor, source hash và số liệu trong TRX lần trước. Công cụ chỉ kiểm tính nhất quán, **không chứng minh văn xuôi đúng mọi nhánh hay runtime parity**.

Kết quả đã chạy trong lượt này: exit code 0, không có failure; 37 controller, 192 method–route, 174 block method và 102 block DTO khớp source (chỉ chuẩn hóa xuống dòng). Các link/anchor trong bốn tài liệu hợp lệ; SHA-256 của 413 file Java production và 196 file C# vẫn khớp snapshot. 174 là số method có mapping đang hoạt động; 192 bao gồm các alias của chúng, không phải hai bộ đếm mâu thuẫn.

Không chạy lại .NET/Java test suite trong lượt rà soát tài liệu này. Kết quả 66 passed / 0 failed / 0 skipped là [TRX đã lưu từ lần audit trước](D:/Flowzy-api/tests/Flowzy.Tests/TestResults/controller-audit.trx), không phải test mới xác nhận các đính chính. Chưa thực hiện differential test cho đủ 192 operations, frontend acceptance hay mọi race/STOMP/export edge.

Trước khi sửa backend theo baseline này, bổ sung regression tests riêng cho null/omitted field, kỳ CLOSED/isLock/row lock, tập member rỗng/inactive, deadline quá khứ, no-op/version, proposal code và các tình huống F01–F20. Không tự sửa Java oracle hoặc nới lỏng bảo mật để làm một phép so sánh hình thức pass.

