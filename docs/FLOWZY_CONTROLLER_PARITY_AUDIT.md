# Audit nghiệp vụ Flowzy so với F-Spark Java

Ngày: 2026-09-25. Phạm vi: source Java tại `D:/f-spark/f-spark-api`, source .NET tại `D:/Flowzy-api`.

**Lưu ý sau triển khai:** Báo cáo này giữ nguyên các phát hiện tại snapshot audit, không phải trạng thái runtime mới nhất. Đợt sửa theo tài liệu đã xử lý các nhánh của F01/F02/F04 (group timeline)/F05/F06/F07/F08/F09/F10/F11/F13/F14/F15/F16/F19/F20; bằng chứng test và phần chưa hoàn tất được theo dõi ở [tiến độ triển khai](D:/Flowzy-api/docs/CONTROLLER_PARITY_IMPLEMENTATION_PROGRESS.md). Không ghi đè manifest baseline để che các thay đổi C#.

## Kết luận

**Flowzy chưa clone chính xác toàn bộ nghiệp vụ Java; chưa đạt điều kiện nghiệm thu tương đương.** Có đủ 192 HTTP operations và không còn synthetic compatibility fallback, nhưng route tồn tại không chứng minh quyền, validation, transaction, state transition, side effect hoặc file format đúng.

- Đã lập đặc tả và bảng đối chiếu đủ **37/37 controller Java**, tương ứng **33 controller C#** (5 catalog controller được gộp).
- Có **20 nhóm phát hiện** dựa trên source, ảnh hưởng trực tiếp **18 controller** trong bảng dưới. Một phát hiện có thể ảnh hưởng nhiều route/controller; con số này không phải tổng số bug độc lập hoặc số endpoint hỏng.
- **19 controller còn lại**: chưa xác nhận sai lệch riêng trong phần đã rà soát; không đồng nghĩa đã pass toàn nghiệp vụ. Cross-cutting validation/auth/realtime vẫn cần kiểm chứng.
- Lần chạy .NET được lưu trong `controller-audit.trx`: **66 passed, 0 failed, 0 skipped**. Đây là kết quả lần audit trước, không phải lần chạy mới trong rà soát tài liệu lần 2. Kết quả không phủ đủ các phát hiện bên dưới, nên không thể dùng để kết luận 100% parity.
- Audit này chỉ thêm tài liệu/công cụ đọc source, cập nhật trạng thái; **không sửa logic runtime, không thay frontend, không thay Java oracle**.

## Tài liệu làm chuẩn

1. [Đặc tả nghiệp vụ 37 controller](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md): từng method, tiền điều kiện, quyền, thao tác DB, state transition và tác động phụ.
2. [Phụ lục HTTP/DTO](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md): toàn bộ 192 route/alias, chữ ký, response body/header từ controller và 102 DTO trực tiếp được chữ ký tham chiếu.
3. [Manifest source SHA-256](D:/Flowzy-api/docs/controller-audit-source-manifest.json): xác định snapshot audit; 413 Java production và 196 C# source, không gồm bin/obj hoặc secret config.
4. [Rà soát tài liệu lần 2](D:/Flowzy-api/docs/CONTROLLER_DOCUMENTATION_REVIEW_2.md): các đính chính của baseline. Đã rút nhận định F08 về PATCH bỏ meetLink; cả hai backend đều bắt buộc field này. F08 vẫn còn các sai lệch note/cancel/confirm độc lập. Bổ sung F03 về lọc kỳ CLOSED và F07 về future-check của meeting trực tiếp; số nhóm phát hiện không đổi.

## Mức bằng chứng và giới hạn

**D — Đã thấy lệch:** có nhánh code Java và C# khác nhau, ghi rõ source ở phần phát hiện. Đây là xác nhận bằng phân tích tĩnh; trừ khi ghi rõ có test, các request tái hiện là checklist cần bổ sung, không phải test đã chạy.

**R — Cần chứng minh đầy đủ:** controller/service/query chính đã được đối chiếu; có thể có test tập trung, nhưng chưa có bộ differential test bao phủ tất cả response/error/side effect/race. Không dùng chữ “hoàn thành” cho mức này.

Audit đi qua controller và các đường gọi service/repository/guard liên quan. Không phải kiểm chứng hình thức mọi execution path của 413 Java file, không phải port đủ bộ 861 test. Không chạy lại toàn Java suite trong lượt này; baseline 861/71 skipped do lịch sử dự án cung cấp không phải bằng chứng mới. Chưa gọi Java và Flowzy song song cho đủ 192 operations, chưa chạy frontend acceptance, chưa kiểm mọi byte của file export và mọi STOMP/SockJS flow. Các annotation/Swagger chỉ hỗ trợ tra cứu, không thay thế source thực thi.

## Ma trận đủ 37 controller

Cột HTTP tính cả alias. R không có nghĩa là pass. Chi tiết API nằm ở số mục tương ứng trong đặc tả.

| # | Java controller | HTTP | C# controller / xử lý chính | Mức | Kết quả và phần cần kiểm tiếp |
|---|---|---:|---|:---:|---|
| 1 | [AcademicTermController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c01) | 1 | [CatalogController](D:/Flowzy-api/src/Flowzy.Api/Controllers/CatalogController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Platform/PlatformService.cs) | R | Luồng đọc OPEN và aggregate chính tương ứng; cần golden JSON, sort tie và khác biệt hoa/thường.  |
| 2 | [AdminFeedbackController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c02) | 2 | [AdminFeedbackController](D:/Flowzy-api/src/Flowzy.Api/Controllers/AdminFeedbackController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Feedback/AdminFeedbackService.cs) | R | Đã đối chiếu filter/ẩn danh khác received, export và test PostgreSQL; chưa golden toàn workbook/header.  |
| 3 | [AdminGroupController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c03) | 1 | [AdminGroupController](D:/Flowzy-api/src/Flowzy.Api/Controllers/AdminGroupController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Groups/AdminGroupService.cs) | R | Paging id, status/search và DTO chính có implementation/test; cần kiểm mọi combination filter và labels.  |
| 4 | [AdminProblemController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c04) | 6 | [AdminProblemController](D:/Flowzy-api/src/Flowzy.Api/Controllers/AdminProblemController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/PortedDomains/PortedDomainService.cs) | D | PATCH gửi lại domain INACTIVE hiện tại bị từ chối khác Java; validation/normalization cần chốt. [F15](#f15) |
| 5 | [AdminTermController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c05) | 5 | [AdminTermController](D:/Flowzy-api/src/Flowzy.Api/Controllers/AdminTermController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/PortedDomains/PortedDomainService.cs) | D | Create/close không idempotent; thiếu cancel pending, archive nhóm/revoke/activation theo scope. [F01](#f01), [F02](#f02) |
| 6 | [AdminUserController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c06) | 7 | [AdminUserController](D:/Flowzy-api/src/Flowzy.Api/Controllers/AdminUserController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Admin/AdminUserService.cs) | D | Không từ chối profile khác role khi profile đúng đã có. [F16](#f16) |
| 7 | [AuthController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c07) | 5 | [AuthController](D:/Flowzy-api/src/Flowzy.Api/Controllers/AuthController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Authentication/AuthService.cs) | D | Login inactive khác message; refresh kiểm ACTIVE khác Java. [F17](#f17) |
| 8 | [BackupController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c08) | 7 | [BackupController](D:/Flowzy-api/src/Flowzy.Api/Controllers/BackupController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Backup/BackupService.cs) | R | Có dump/download/restore thật và test PostgreSQL; chưa chứng minh toàn cron/timezone/multi-instance.  |
| 9 | [CourseMilestoneController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c09) | 16 | [CourseMilestoneController](D:/Flowzy-api/src/Flowzy.Api/Controllers/CourseMilestoneController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/PortedDomains/PortedDomainService.cs) | D | Validation/alias dueDate, student list và notification còn lệch. [F09](#f09), [F18](#f18) |
| 10 | [DashboardController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c10) | 15 | [DashboardController](D:/Flowzy-api/src/Flowzy.Api/Controllers/DashboardController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Dashboard/DashboardService.cs) | D | Enum-key/archived counts, TV project fallback, recruitment labels khác. [F12](#f12) |
| 11 | [FeedbackController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c11) | 3 | [FeedbackController](D:/Flowzy-api/src/Flowzy.Api/Controllers/FeedbackController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Feedback/FeedbackService.cs) | R | Đã đối chiếu ownership, closed-term, submit/update, privacy; cần differential optimistic concurrency.  |
| 12 | [GroupController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c12) | 19 | [GroupController](D:/Flowzy-api/src/Flowzy.Api/Controllers/GroupController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Groups/GroupOperationsService.cs) | D | Discovery/filter/criteria/transaction và quyền timeline khác. [F03](#f03), [F04](#f04), [F18](#f18) |
| 13 | [GroupInvitationController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c13) | 6 | [GroupInvitationController](D:/Flowzy-api/src/Flowzy.Api/Controllers/GroupInvitationController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Groups/GroupOperationsService.cs) | D | Quyền, expiry/quota, cancel enum, scope cleanup và student lock khác. [F05](#f05), [F06](#f06), [F18](#f18) |
| 14 | [GroupJoinRequestController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c14) | 9 | [GroupJoinRequestController](D:/Flowzy-api/src/Flowzy.Api/Controllers/GroupJoinRequestController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Groups/GroupOperationsService.cs) | D | APPROVED/CANCELLED sai enum, thiếu quota/expiry/role/lock/cleanup. [F05](#f05), [F06](#f06), [F18](#f18) |
| 15 | [GroupMeetingController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c15) | 8 | [GroupMeetingController](D:/Flowzy-api/src/Flowzy.Api/Controllers/GroupMeetingController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Mentors/GroupMeetingService.cs) | D | Quyền đọc/evidence, thời gian/quota, state transitions và notification khác. [F07](#f07), [F08](#f08), [F18](#f18) |
| 16 | [GroupProblemController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c16) | 6 | [GroupProblemController](D:/Flowzy-api/src/Flowzy.Api/Controllers/GroupProblemController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Groups/GroupOperationsService.cs) | D | Status lỗi owner/review và transaction propose khác; GET member hợp lệ, không phải lỗi. [F14](#f14) |
| 17 | [GroupRecruitmentRoleController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c17) | 1 | [CatalogController](D:/Flowzy-api/src/Flowzy.Api/Controllers/CatalogController.cs), catalog enum tĩnh | R | 26 giá trị enum, thứ tự và nhãn đã đối chiếu source; vẫn cần golden HTTP qua auth gate.  |
| 18 | [GroupTaskController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c18) | 19 | [GroupTaskController](D:/Flowzy-api/src/Flowzy.Api/Controllers/GroupTaskController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Tasks/GroupTaskService.cs) | R | Các luồng chính/version/order/cleanup có test chuyên biệt; chưa exhaustive mọi error/notification.  |
| 19 | [ImportController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c19) | 7 | [ImportController](D:/Flowzy-api/src/Flowzy.Api/Controllers/ImportController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Imports/ImportService.cs) | R | Queue/row transactions/parser/template có test; toàn validation workbook và warning/recovery còn cần differential.  |
| 20 | [InstructorGroupBoardController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c20) | 2 | [InstructorGroupBoardController](D:/Flowzy-api/src/Flowzy.Api/Controllers/InstructorGroupBoardController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Groups/InstructorBoardService.cs) | D | Thiếu ACTIVE/member filter, search member và course count scope; claim lại không no-op. [F13](#f13) |
| 21 | [InstructorProblemController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c21) | 2 | [InstructorProblemController](D:/Flowzy-api/src/Flowzy.Api/Controllers/InstructorProblemController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Problems/InstructorProblemService.cs) | R | Luồng pending + instructor ownership/review có test; cần đủ invalid DTO, duplicate và race review.  |
| 22 | [InstructorSubmissionController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c22) | 1 | [InstructorSubmissionController](D:/Flowzy-api/src/Flowzy.Api/Controllers/InstructorSubmissionController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Submissions/InstructorSubmissionService.cs) | R | Query kiểm cả owner milestone và group, có test; cần DTO legacy/null/maxScore/header exact.  |
| 23 | [MentorAvailabilityController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c23) | 5 | [MentorAvailabilityController](D:/Flowzy-api/src/Flowzy.Api/Controllers/MentorAvailabilityController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Mentors/MentorAvailabilityService.cs) | R | Time policy/link/overlap/booked protection có test; không đồng nghĩa GroupMeeting đã đúng.  |
| 24 | [MentorMeetingReportController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c24) | 2 | [MentorMeetingReportController](D:/Flowzy-api/src/Flowzy.Api/Controllers/MentorMeetingReportController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Mentors/MentorMeetingReportService.cs) | R | Hai sheet và lịch sử nhóm/meeting đã đối chiếu, test; còn cần golden workbook, filename/header edge.  |
| 25 | [MilestoneGradeController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c25) | 3 | [MilestoneGradeController](D:/Flowzy-api/src/Flowzy.Api/Controllers/MilestoneGradeController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Grades/MilestoneGradeService.cs) | R | Read legacy + bulk 404 có test; không yêu cầu port writes đã comment.  |
| 26 | [MilestoneGradeMatrixController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c26) | 6 | [MilestoneGradeMatrixController](D:/Flowzy-api/src/Flowzy.Api/Controllers/MilestoneGradeMatrixController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Grades/GradeMatrixService.cs) | D | Revision transaction/lock, complete/rounding, export layout/scope và notification khác. [F10](#f10), [F11](#f11), [F18](#f18) |
| 27 | [MilestoneSubmissionController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c27) | 3 | [MilestoneSubmissionController](D:/Flowzy-api/src/Flowzy.Api/Controllers/MilestoneSubmissionController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/PortedDomains/PortedDomainService.cs) | D | Quyền instructor dùng owner milestone thay current group; list thiếu group filter. [F19](#f19) |
| 28 | [NotificationController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c28) | 4 | [NotificationController](D:/Flowzy-api/src/Flowzy.Api/Controllers/NotificationController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Platform/PlatformService.cs) | D | CRUD DB có implementation; pipeline realtime STOMP chưa hoàn chỉnh. [F18](#f18) |
| 29 | [ProblemController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c29) | 2 | [CatalogController](D:/Flowzy-api/src/Flowzy.Api/Controllers/CatalogController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Platform/PlatformService.cs) | D | Query enum nhận string, không giữ cơ chế từ chối enum invalid của Java. [F20](#f20) |
| 30 | [ProblemCriteriaController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c30) | 1 | [CatalogController](D:/Flowzy-api/src/Flowzy.Api/Controllers/CatalogController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Platform/PlatformService.cs) | R | Active/displayOrder và fields chính tương ứng; cần golden JSON và tie cases.  |
| 31 | [ProblemDomainController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c31) | 1 | [CatalogController](D:/Flowzy-api/src/Flowzy.Api/Controllers/CatalogController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Platform/PlatformService.cs) | D | Sort code ASC thay createdAt/id DESC; query enum invalid. [F15](#f15), [F20](#f20) |
| 32 | [ProblemImportController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c32) | 1 | [ProblemImportController](D:/Flowzy-api/src/Flowzy.Api/Controllers/ProblemImportController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Imports/ImportService.cs) | R | Domain/problem upsert và alias chính có test; cần complete validation matrix. Domain thiếu được clear theo Java.  |
| 33 | [ProfileController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c33) | 3 | [ProfileController](D:/Flowzy-api/src/Flowzy.Api/Controllers/ProfileController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Platform/PlatformService.cs) | D | DTO thiếu maxlength, enum gender, nonnegative experience tương ứng. [F16](#f16) |
| 34 | [StudentAccountImportController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c34) | 2 | [StudentAccountImportController](D:/Flowzy-api/src/Flowzy.Api/Controllers/StudentAccountImportController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Imports/ImportService.cs) | R | Roster-only/reactivation/hash preservation có test; cần tất cả duplicate/status/batch counters.  |
| 35 | [StudentController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c35) | 2 | [StudentController](D:/Flowzy-api/src/Flowzy.Api/Controllers/StudentController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Students/StudentService.cs) | R | Target active, ungrouped scope và search đã đối chiếu/test; cần paging invalid và exact errors.  |
| 36 | [StudentGroupGradeController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c36) | 1 | [StudentGroupGradeController](D:/Flowzy-api/src/Flowzy.Api/Controllers/StudentGroupGradeController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Grades/MilestoneGradeService.cs) | R | Weighted average legacy và round chính có test; cần đủ null weight/archived/zero denominator.  |
| 37 | [TaskBoardController](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md#c37) | 8 | [TaskBoardController](D:/Flowzy-api/src/Flowzy.Api/Controllers/TaskBoardController.cs), [service](D:/Flowzy-api/src/Flowzy.Service/Tasks/TaskBoardService.cs) | R | Lock/default promotion/archive/delete có test; cần response và validation exact toàn aliases.  |

## Phát hiện có bằng chứng source

P1: cần ưu tiên trước nghiệm thu vì quyền truy cập, toàn vẹn dữ liệu hoặc luồng nghiệp vụ chính. P2: khác contract/filter/validation/hiển thị hoặc cần quyết định tương thích có chủ ý. Mức ưu tiên không có nghĩa đã xác nhận khai thác/lỗi ở môi trường production.

<a id="f01"></a>

### F01 — P1: Vòng đời kỳ học không giữ idempotency và cleanup

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/AcademicTermServiceImpl.java:94) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/PortedDomains/PortedDomainService.cs).

- **Java:** createTerm cùng code OPEN trả kỳ đang có; closeTerm đã CLOSED trả thành công. Close khóa nhóm/kỳ, hủy pending invitations/join requests và snapshot feedback.
- **Flowzy:** CreateTermAsync trả conflict nếu code đã có; CloseTermAsync từ chối kỳ đã đóng, không có bước hủy pending invitation/join request tương ứng Java; nhánh nhận kỳ legacy cũng không tương ứng.
- **Tác động:** Retry hợp lệ thành lỗi; dữ liệu pending vẫn tồn tại sau khi khóa kỳ. Chỉ snapshot feedback không đủ để xem close-term là tương đương.
- **Ca kiểm thử cần bổ sung:** Tạo kỳ OPEN rồi tạo lại; đóng hai lần; trước khi đóng có pending invitation/join request. So HTTP, closedAt, feedback không trùng, trạng thái pending và notification.

<a id="f02"></a>

### F02 — P1: Archive students thực hiện quy tắc khác Java

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/AcademicTermServiceImpl.java:284) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/PortedDomains/PortedDomainService.cs).

- **Java:** Kỳ CLOSED: mọi nhóm trong kỳ thành INACTIVE. Student còn thuộc kỳ OPEN khác được giữ/đưa ACTIVE cùng account; còn lại inactive cả hai và revoke refresh. Không đợi hoàn thành feedback.
- **Flowzy:** ArchiveStudentsAsync có nhánh bỏ qua pending feedback, không deactivate toàn nhóm, không revoke refresh tương ứng và không phục hồi account/profile theo nhánh còn kỳ OPEN.
- **Tác động:** Cùng dữ liệu nhưng student có thể vẫn hoạt động hoặc nhóm vẫn ACTIVE; counts kết quả không khớp.
- **Ca kiểm thử cần bổ sung:** Fixture bốn student: feedback pending; feedback done; còn membership kỳ OPEN khác nhưng đang inactive; đã inactive cả account/profile. So mọi trạng thái và refresh-token table.

<a id="f03"></a>

### F03 — P2: Group discovery/filter và recruitment metadata thiếu

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupServiceImpl.java) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/Groups/GroupOperationsService.cs).

- **Java:** Discover loại nhóm mình đã tham gia; neededRole không lấy nhóm locked. List hỗ trợ roleCategory và enum validation; chỉ giữ group có academicTerm null hoặc OPEN, loại kỳ CLOSED. Nhu cầu tuyển có category/nhãn VI/EN.
- **Flowzy:** DiscoverAsync không loại membership của caller và không loại locked khi lọc role. ListAsync không sử dụng category và không loại kỳ CLOSED. Mapping recruitment có nhãn null.
- **Tác động:** FE thấy nhóm không phù hợp và thiếu nhãn; filter hiển thị không phản ánh đúng dữ liệu.
- **Ca kiểm thử cần bổ sung:** Student thuộc G1, G2 locked có role cần tìm, G3 khác category; so list/discover với từng filter và response recruitmentNeeds; GET /api/groups với nhóm thuộc kỳ CLOSED phải bị loại như Java.

<a id="f04"></a>

### F04 — P1: Group write: transaction, tiêu chí và quyền timeline lệch

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupServiceImpl.java) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/Groups/GroupOperationsService.cs).

- **Java:** Tạo group + member + Default board trong transaction; role hợp lệ, mỗi quantity 1–6 và tổng≤6. Group timeline chỉ student member hoặc instructor được gán theo CourseMilestoneServiceImpl.assertCanViewGroup.
- **Flowzy:** CreateAsync SaveChanges group trước ReplaceNeeds, không có transaction bao ngoài. ReplaceNeeds chỉ chặn quantity<1 và trùng role; thiếu giới hạn 6/tổng/enum. GroupMilestonesAsync chỉ kiểm STUDENT/INSTRUCTOR nên ADMIN/MENTOR có thể đi qua service. Một số nhánh ADMIN update vẫn gọi Writable thay bypass.
- **Tác động:** Request recruitment lỗi có thể để lại group đã lưu; quyền đọc rộng hơn Java, dữ liệu tuyển vượt giới hạn.
- **Ca kiểm thử cần bổ sung:** Gửi role trùng khiến bước sau thất bại và đếm group/member/board sau request; quantity=7 hoặc tổng=7; ADMIN/MENTOR đọc timeline; ADMIN sửa nhóm kỳ CLOSED. So cả status lẫn dữ liệu.

<a id="f05"></a>

### F05 — P1: Join/invitation ghi trạng thái không được schema chấp nhận

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupJoinRequestServiceImpl.java:186) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/Groups/GroupOperationsService.cs).

- **Java:** Join approval dùng ACCEPTED; cancel invitation/join dùng CANCELED.
- **Flowzy:** RespondJoinAsync dùng APPROVED; cancel và cleanup dùng CANCELLED. Migration V4/V12 chỉ cho ACCEPTED/CANCELED, không có hai giá trị trên.
- **Tác động:** Đây không chỉ là khác spelling JSON: trên schema gốc, khi nhánh ghi giá trị này được thực thi, CHECK constraint chặn SaveChanges. Kết luận dựa trên source + SQL; chưa chạy riêng request tái hiện trong audit.
- **Ca kiểm thử cần bổ sung:** Approve một pending request hợp lệ; cancel invitation/request; accept invitation khi còn pending join. Mong đợi thành công, DB status đúng enum và membership atomic.

<a id="f06"></a>

### F06 — P1: Join/invitation thiếu quyền, expiry, quota và khóa theo student

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupInvitationServiceImpl.java:52) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/Groups/GroupOperationsService.cs).

- **Java:** Leader/ADMIN có các quyền quản lý invitation; assigned mentor đọc join requests. Expiry 72h, quota tối đa 6, group ACTIVE, writable/unlocked theo nhánh; accept/approve khóa student và group, cleanup pending cùng term/course.
- **Flowzy:** Các method đầu vào resolve Student nên loại ADMIN/MENTOR. Invitation list cho mọi member thay leader/admin. Thiếu expiry/quota/ACTIVE ở các nhánh; không khóa student xuyên nhiều nhóm. Accept hủy mọi pending join của student, không giới hạn term/course, và không decline các invitation còn lại như Java.
- **Tác động:** Quyền bị thừa/thiếu; nhận request hết hạn hoặc vượt capacity; cạnh tranh hai group và cleanup nhầm scope.
- **Ca kiểm thử cần bổ sung:** Test riêng mọi role; pending 71h/73h; nhóm 6 người; group INACTIVE; hai request vào hai nhóm cùng scope đồng thời; pending ở môn khác phải còn nguyên. F05 phải được xử lý để chạy sâu tới các nhánh bị constraint che khuất.

<a id="f07"></a>

### F07 — P1: Meeting sai quyền và điều kiện booking/evidence

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MeetingServiceImpl.java:70) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/Mentors/GroupMeetingService.cs).

- **Java:** Instructor được gán được đọc meeting; availability cho student member. Quota 2 SCHEDULED+COMPLETED; lịch theo 60 phút/00 hoặc 30. Booking cần slot future; mentor tạo/sửa meeting trực tiếp không có kiểm startAt>now. Meeting link trim, bắt đầu http:// hoặc https://, tối đa 500 ký tự; không áp regex Google Meet nghiêm ngặt của availability. Evidence cho mọi active member, sau endAt, chưa có evidence, kỳ writable.
- **Flowzy:** Access không cho INSTRUCTOR nhưng cho mentor đọc availability. Create thiếu quota và student closed-term guard, chỉ kiểm end>start/future và URI HTTP(S), không giữ giới hạn link 500 ký tự tại DTO/service. Create và Update đều thêm chặn startAt<=now, khác nhánh mentor trực tiếp của Java. Evidence chỉ leader nhưng thiếu end-time/đã có evidence/completed và kỳ writable.
- **Tác động:** FE instructor bị chặn sai; meeting vượt quota hoặc chấp nhận chứng cứ trước khi diễn ra/ghi đè, thành viên hợp lệ không gửi được.
- **Ca kiểm thử cần bổ sung:** Tạo meeting thứ ba; lịch 45 phút hoặc start phút 17; mentor tạo/sửa direct meeting có startAt quá khứ nhưng lịch hợp lệ và không overlap; link dài hơn 500 ký tự; student book trong kỳ đóng; instructor GET; member thường gửi evidence sau end; leader gửi trước end và gửi lần hai.

<a id="f08"></a>

### F08 — P1: Meeting PATCH/cancel/confirm không giữ state machine

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MeetingServiceImpl.java:205) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/Mentors/GroupMeetingService.cs).

- **Java:** HTTP update bắt buộc meetLink (@NotBlank + @Valid), startAt/endAt/note không truyền giữ cũ. Cancel lại CANCELED là idempotent, slot liên kết thành CANCELED. Confirm cần slot đã bắt đầu, không CANCELED, timestamp chỉ đặt lần đầu.
- **Flowzy:** Khi gửi link nhưng bỏ Note, service gán Note=null thay vì giữ cũ. Cancel meeting đã hủy trả lỗi; slot future được mở lại AVAILABLE. Confirm thiếu các guard trạng thái/start/slot và ghi đè timestamp mỗi lần.
- **Đính chính lần 2:** Rút nhận định “bỏ meetLink hợp lệ ở Java nhưng bị Flowzy từ chối”. Java cũng bắt buộc meetLink ở DTO; fallback null trong service không đủ để suy ra contract HTTP. Không dùng nhận định đã rút làm yêu cầu sửa code.
- **Tác động còn hiệu lực:** PATCH có link nhưng bỏ note làm mất note; slot bị tái sử dụng khác Java, thời gian xác nhận không còn phản ánh lần đầu.
- **Ca kiểm thử cần bổ sung:** PATCH có link và bỏ note phải giữ note; PATCH chỉ note phải bị validation chặn ở cả hai backend (so tiếp body lỗi); cancel hai lần; kiểm slot sau cancel; confirm trước start, meeting direct không slot, meeting CANCELED; confirm lặp phải không đổi timestamp.

<a id="f09"></a>

### F09 — P1: Course milestone thiếu validation và thay đổi visibility

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/CourseMilestoneServiceImpl.java) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/PortedDomains/PortedDomainService.cs).

- **Java:** DTO giữ alias dueDate→deadlineAt, weight 0–100, maxScore≥0.01, position≥0; student filter phải đủ term/course, list bỏ INACTIVE và ARCHIVED.
- **Flowzy:** CourseMilestoneRequest/UpdateCourseMilestoneRequest không có alias dueDate và các range tương ứng. Create không kiểm đủ maxScore/range. GetMilestonesAsync không giữ đầy đủ paired-filter và INACTIVE exclusion cho student.
- **Tác động:** Payload FE dùng alias bị lỗi; milestone số âm hoặc visibility khác baseline; tác động các phép tính grade.
- **Ca kiểm thử cần bổ sung:** POST với dueDate thay deadlineAt, weight=-1/maxScore=0/position=-1; student list chỉ term, chỉ course và cả hai; fixture INACTIVE/ARCHIVED. So validation data/message, không chỉ status.

<a id="f10"></a>

### F10 — P1: Grade matrix thiếu transaction/revision locking và sai completeness

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneGradeMatrixServiceImpl.java) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/Grades/GradeMatrixService.cs).

- **Java:** Service transactional; revision được khóa ở contribution/agreement và kiểm agreement lần chấm đầu, không mặc định mọi lần sửa grade khóa revision. Grade cần đúng tập contribution active members. Chưa instructor trả 409; owner-scope conflict xảy ra khi không có visible milestone của instructor hiện tại nhưng có visible milestone của instructor khác trong scope. Matrix complete yêu cầu có columns và tất cả gradeComplete; weighted score làm tròn theo quy tắc Java.
- **Flowzy:** Grade/Contributions/Agreement có nhiều SaveChanges nhưng không transaction bao ngoài/row lock revision. Grade chỉ kiểm thiếu active member, không loại thừa; báo conflict thay bad request ở nhánh thiếu contribution. Build trả 200 rỗng khi chưa instructor, có thể complete=true khi có members nhưng không milestone; tổng không áp round từng hạng tử theo Java.
- **Tác động:** Có thể lưu dở grade/member scores, mất agreement trong race, hoặc báo hoàn tất khi chưa có milestone; số điểm có thể khác.
- **Ca kiểm thử cần bổ sung:** Đồng thời contribution revision/agree/grade; score rows có student đã rời nhóm; group chưa instructor; group có members nhưng zero milestones; bộ điểm phân số để so chính xác 4 chữ số.

<a id="f11"></a>

### F11 — P1: Export grade lấy sai tập milestone và layout XLSX

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneGradeMatrixServiceImpl.java) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/Grades/GradeMatrixService.cs).

- **Java:** Export milestone đúng instructor/term/course scope. CSV và XLSX có layout riêng; XLSX có Tên Nhóm/Họ & Tên/MSSV/Email (đuôi FPT), một score column/milestone và Final, cell số.
- **Flowzy:** Export lọc groups nhưng lấy tất cả milestone của instructor, không áp term/course tương ứng. XLSX dùng nguyên rows/cột CSV, SetCellValue(string) cho mọi cell. CSV AppendLine rồi Replace LF→CRLF có rủi ro CRCRLF trên Windows.
- **Tác động:** File chứa cột ngoài scope, completeness/tổng sai, bố cục và kiểu cell không đáp ứng FE/người dùng. Phần CRCRLF là suy luận platform-specific cần test byte Windows.
- **Ca kiểm thử cần bổ sung:** Instructor có hai kỳ/hai môn; export từng scope với/không groupId; đọc workbook kiểm sheet/header/number type/cột điểm; so CSV BOM, newline và escaping trên Windows/Linux.

<a id="f12"></a>

### F12 — P2: Dashboard sai enum-map, tập task và TV fallback

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/DashboardServiceImpl.java:95) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/Dashboard/DashboardService.cs).

- **Java:** taskStatusCounts theo enum Java từ allTasks; TV projects dùng projectName fallback selectedProblem.title rồi group.name; recruitment trả nhãn enum. Sort TV updatedAt DESC,id DESC.
- **Flowzy:** Execution có thêm BLOCKED và đếm active tasks cho status map. Program.DictionaryKeyPolicy=CamelCase biến key viết hoa. Tv loại group có ProjectName null/rỗng trước khi fallback, và recruitment labels=null; group ordering không có id tie-break.
- **Tác động:** Dashboard có thể thiếu project hợp lệ, counts khác, key FE mong đợi không có. Không coi việc TV vẫn hiện nhóm locked là lỗi: Java cũng không lọc locked ở luồng này.
- **Ca kiểm thử cần bổ sung:** Fixture archived task, mỗi status hợp lệ, group projectName null nhưng đã chọn problem, group recruitment có role; so JSON keys/counts/labels và paging khi updatedAt bằng nhau.

<a id="f13"></a>

### F13 — P2: Instructor group board khác điều kiện hiển thị và tổng hợp

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/InstructorGroupBoardServiceImpl.java) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/Groups/InstructorBoardService.cs).

- **Java:** Board chỉ OPEN + ACTIVE + có member; search cả member name/code/email/className. Course counters chỉ áp term, không áp courseCode. Claim lại nhóm của mình là no-op.
- **Flowzy:** Get chỉ lọc OPEN; search chỉ name/groupNo/project. baseQ dùng cả courseCode để tạo courses counts. Claim luôn sửa UpdatedAt kể cả đã là người sở hữu.
- **Tác động:** Xuất hiện nhóm không thể nhận; search không tìm thấy student; bộ lọc môn mất counts các môn còn lại.
- **Ca kiểm thử cần bổ sung:** Group inactive/empty trong kỳ mở; search MSSV; chọn một course và kiểm counts toàn kỳ; claim lại và so UpdatedAt.

<a id="f14"></a>

### F14 — P2: Group proposal gộp lỗi và thiếu atomicity

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupProblemServiceImpl.java) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/Groups/GroupOperationsService.cs).

- **Java:** Propose và selection cùng transaction; sinh code SP-{groupId}-{8 ký tự đầu UUID}, giữ nguyên title/statement từ request. Update phân biệt proposal không thuộc group (404), sai sourceType (400), không PENDING_REVIEW (409); delete kiểm owner/type/status.
- **Flowzy:** Propose lưu problem rồi lưu selection mà không transaction bao ngoài; không gán Code (entity nullable, mặc định null), trim title/statement khác Java. Update gộp owner/type/status thành 400; Delete không kiểm sourceType như Java. Các message select cũng bị gộp.
- **Tác động:** FE xử lý sai conflict/not-found; lỗi bước chọn proposal có thể để lại dữ liệu dở. GET proposals cho member không phải sai lệch.
- **Ca kiểm thử cần bổ sung:** Create proposal kiểm code tự sinh và title/statement có khoảng trắng đầu/cuối; update proposal group khác, OFFICIAL và đã reviewed; kiểm status/message từng nhánh. Inject failure bước selection để kiểm rollback cả problem.

<a id="f15"></a>

### F15 — P2: Problem/domain PATCH và sort khác Java

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/ProblemBankServiceImpl.java) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/PortedDomains/PortedDomainService.cs).

- **Java:** PATCH gửi lại cùng domainCode hiện tại không revalidate ACTIVE; list domain sắp createdAt DESC,id DESC.
- **Flowzy:** UpdateProblemAsync luôn gọi ActiveDomain khi domainCode nonblank nên domain hiện có đã INACTIVE vẫn bị từ chối. PlatformReadRepository.GetProblemDomainsAsync sort code ASC.
- **Tác động:** Form sửa trường khác nhưng giữ domain cũ có thể thất bại; thứ tự danh mục thay đổi.
- **Ca kiểm thử cần bổ sung:** Problem đã gắn domain rồi domain bị INACTIVE; PATCH title cùng domainCode cũ; thêm domain mới có code alphabet nhỏ/lớn để phân biệt sort.

<a id="f16"></a>

### F16 — P2: Admin profile và self profile thiếu quy tắc validation

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/AdminUserServiceImpl.java) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/Admin/AdminUserService.cs).

- **Java:** STUDENT/MENTOR/INSTRUCTOR chỉ có đúng loại profile, không gửi thêm profile loại khác. Self-profile DTO có giới hạn độ dài, Gender enum, yearsOfExperience≥0.
- **Flowzy:** ValidateProfile chỉ yêu cầu profile đúng có mặt, không cấm profile khác loại cho ba role. UpdateSelfProfileRequest không có annotations tương ứng; Gender là string, PlatformService không bù đủ range/length.
- **Tác động:** Request Java từ chối có thể thành success hoặc lỗi DB thay validation envelope, không đạt contract.
- **Ca kiểm thử cần bổ sung:** Tạo STUDENT với cả studentProfile+mentorProfile; update self mentor yearsOfExperience=-1; tên vượt maxlength, gender không hợp lệ. So lỗi validation và DB không đổi.

<a id="f17"></a>

### F17 — P2: Auth: message login và policy refresh khác

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/Authentication/AuthService.cs).

- **Java:** Login catch lỗi authenticate chung thành 401 Invalid email or password. Refresh kiểm token lưu/hết hạn rồi phát token; controller không gọi lại guard ACTIVE trong nhánh refresh.
- **Flowzy:** Login EnsureActive dùng message inactive/locked riêng. Refresh EnsureActive(stored.Account) từ chối account không ACTIVE.
- **Tác động:** Không phải clone tuyệt đối. Check ACTIVE khi refresh là tăng bảo vệ có thể mong muốn; không nên bỏ âm thầm chỉ để làm giống Java. Cần quyết định contract có chủ ý và test.
- **Ca kiểm thử cần bổ sung:** Account inactive + mật khẩu đúng; stored refresh còn hiệu lực nhưng account đổi status bằng fixture không xóa token. So cả code/message; giữ nhánh bảo mật thành quyết định riêng trước sửa.

<a id="f18"></a>

### F18 — P1: Notification side effects và STOMP chưa tương đương

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/NotificationServiceImpl.java) · [Flowzy](D:/Flowzy-api/src/Flowzy.Api/WebSockets/StompWebSocketHandler.cs).

- **Java:** Java gửi notification cho các sự kiện kỳ/nhóm/invitation/join/meeting/milestone/contribution và push sau commit qua convertAndSendToUser. Đăng ký subscription phải dẫn tới MESSAGE khi có event hợp lệ.
- **Flowzy:** Handler Flowzy chỉ CONNECT/SUBSCRIBE/RECEIPT, không có registry/fan-out/MESSAGE pipeline. Nhiều broad-port methods ở group meeting, grade matrix, close term/course milestone thiếu cả persist notification tương ứng. Task notifications đã có persist, không được gộp nhầm thành thiếu toàn bộ.
- **Tác động:** Frontend nhận REST thành công nhưng không có thông báo hoặc không cập nhật realtime. Route /ws tồn tại và CONNECTED không chứng minh Spring STOMP/SockJS compatibility.
- **Ca kiểm thử cần bổ sung:** Subscribe bằng client FE, thực hiện mỗi event, kiểm đúng recipient/eventKey/action params, duy nhất một DB notification và một MESSAGE sau commit; rollback không gửi. Kiểm SockJS handshake nếu client đang sử dụng.

<a id="f19"></a>

### F19 — P1: Submission có thể lộ cho instructor cũ sau chuyển nhóm

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MilestoneSubmissionServiceImpl.java) · [Flowzy](D:/Flowzy-api/src/Flowzy.Service/PortedDomains/PortedDomainService.cs).

- **Java:** GET submission kiểm instructor đang được gán group. GET theo milestone còn lọc group.instructor hiện tại. Điều này khác việc chỉ kiểm milestone owner.
- **Flowzy:** GetSubmissionAsync truyền e.Milestone.InstructorId vào CanViewGroup; GetMilestoneSubmissionsAsync chỉ lọc MilestoneId, không filter group instructor hiện tại.
- **Tác động:** Instructor cũ vẫn đọc được dữ liệu group sau khi nhóm chuyển cho người khác; instructor mới có thể bị từ chối ở GET detail.
- **Ca kiểm thử cần bổ sung:** Milestone thuộc A, group ban đầu A rồi chuyển sang B, đã có submission. A/B gọi detail, group list và milestone list. So quyền theo Java; không nhầm với InstructorSubmissionController đã có double-scope query.

<a id="f20"></a>

### F20 — P2: Query enum không hợp lệ trả dữ liệu rỗng thay lỗi

Nguồn: [Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemController.java) · [Flowzy](D:/Flowzy-api/src/Flowzy.Api/Controllers/CatalogController.cs).

- **Java:** Query difficulty/sourceType/status là enum Java; binding từ chối giá trị ngoài enum bằng 400 trước truy vấn. Domain status cũng là enum.
- **Flowzy:** CatalogController nhận string; PlatformService/PlatformReadRepository chỉ ToUpper và filter, không validate enum. Ví dụ status=NOT_A_STATUS trở thành filter không khớp, thường trả 200 content rỗng.
- **Tác động:** FE không nhận validation error như backend gốc; contract invalid-input không tương đương dù happy-path trùng.
- **Ca kiểm thử cần bổ sung:** Gọi /api/problems?status=NOT_A_STATUS, difficulty/sourceType không hợp lệ và /api/problem-domains?status=NOT_A_STATUS bằng JWT hợp lệ; so status/envelope với Java.

### Bằng chứng bổ sung cho các phát hiện xuyên tầng

- F04: [assertCanViewGroup](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/CourseMilestoneServiceImpl.java:403), [GroupMilestonesAsync](D:/Flowzy-api/src/Flowzy.Service/Groups/GroupOperationsService.cs), [ReplaceNeeds](D:/Flowzy-api/src/Flowzy.Service/Groups/GroupOperationsService.cs).
- F05: [CHECK invitation status V4](D:/f-spark/f-spark-api/src/main/resources/db/migration/V4__create_group_invitations_table.sql:14), [CHECK join status V12](D:/f-spark/f-spark-api/src/main/resources/db/migration/V12__create_group_join_requests_and_kanban_boards.sql:55). Không nhầm notification type GROUP_JOIN_REQUEST_APPROVED với trạng thái entity ACCEPTED.
- F06: [Join request service](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/GroupJoinRequestServiceImpl.java), ngoài invitation service đã liên kết.
- F09: [DTO Flowzy](D:/Flowzy-api/src/Flowzy.Service/Contracts/PortedDomainContracts.cs); request Java nguyên văn nằm trong phụ lục.
- F07/F08: [Meeting DTO Flowzy](D:/Flowzy-api/src/Flowzy.Service/Contracts/MeetingContracts.cs), [link policy Java meeting](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/MeetingServiceImpl.java:561). Policy meeting và availability không giống nhau.
- F12: [DictionaryKeyPolicy](D:/Flowzy-api/src/Flowzy.Api/Program.cs), [TV Java](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/DashboardServiceImpl.java:427).
- F15/F20: [PlatformReadRepository](D:/Flowzy-api/src/Flowzy.Repository/Repositories/PlatformReadRepository.cs).
- F16: [Self profile DTO](D:/Flowzy-api/src/Flowzy.Service/Contracts/PlatformContracts.cs), [update self profile](D:/Flowzy-api/src/Flowzy.Service/Platform/PlatformService.cs).
- F18: [STOMP handler](D:/Flowzy-api/src/Flowzy.Api/WebSockets/StompWebSocketHandler.cs), [Java NotificationServiceImpl](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/services/NotificationServiceImpl.java). Nhiều service có notification DB riêng nhưng chưa nối thành realtime delivery.

## Các điểm đã kiểm lại để tránh kết luận sai

- Java đang có **37 controller**, không phải 38. Đếm các class production hoạt động tại snapshot này; số endpoint là 192 khi tính alias.
- GET group proposals dành student member, **không chỉ leader**. Không ghi nhận quyền member này là bug Flowzy.
- TV recruitment Java **không tự loại group locked**; cả hai không lọc locked ở đây không phải sai lệch. Khác với group discovery lọc recruitment.
- TV sort Java là updatedAt DESC, id DESC; không phải course/groupNo.
- MilestoneGroupGradeDto có field **instructorId**. Flowzy dùng InstructorId không phải khác contract của field này.
- POST/PATCH submission và write-grade legacy bị comment trong Java. Chúng không phải controller “chưa hoàn thiện” cần mở lại. Ngược lại outcomes/bulk cố ý 404 vẫn phải giữ route/hành vi.
- Archive students Java không chờ feedback dù annotation cũ có thể diễn đạt như vậy. Đặc tả theo code, không theo mô tả cũ.
- Import problem với domain không hợp lệ/không tồn tại có nhánh clear reference trong Java. Không tự xem việc clear domain là lỗi clone.
- Cải tiến bảo mật như kiểm ACTIVE ở refresh phải được ghi nhận là thay đổi có chủ ý, không tùy tiện bỏ để làm giống hành vi yếu hơn của bản cũ.
- Rà soát lần 2: PATCH meeting thiếu meetLink bị DTO Java chặn; không coi Required của Flowzy là lỗi. Transfer leader không kiểm isLock. Student ungrouped chỉ lọc student ACTIVE, không lọc account ACTIVE; Flowzy StudentDiscoveryRepository cũng làm như vậy nên đây là lỗi mô tả cũ, không phải sai lệch mới của Flowzy.

## Bằng chứng test trong lượt audit

Kiểm tra tính nhất quán tài liệu bằng `node scripts/Verify-ControllerAudit.cjs` đã pass: đủ 37 controller và đúng 192 route trong cả đặc tả/phụ lục; các local source link/anchor tồn tại; SHA-256 của 413 Java và 196 C# source không đổi so với snapshot; số liệu TRX khớp báo cáo. Đây là kiểm tra tài liệu, không phải kiểm thử nghiệp vụ.

Rà soát lần 2 đã mở rộng và chạy lại công cụ: cả 174 block method và 102 block DTO trong phụ lục khớp source sau chuẩn hóa xuống dòng; bốn tài liệu kiểm link/anchor đều pass. Đây là lần kiểm tài liệu mới; lệnh .NET và TRX bên dưới thuộc lần audit trước.

Lệnh đã thực hiện:

```powershell
dotnet test tests/Flowzy.Tests/Flowzy.Tests.csproj --no-restore --verbosity quiet --logger "trx;LogFileName=controller-audit.trx"
```

Kết quả: **66 passed / 0 failed / 0 skipped**, exit code 0. [TRX](D:/Flowzy-api/tests/Flowzy.Tests/TestResults/controller-audit.trx). Các repository/integration test hiện có dùng PostgreSQL Testcontainers; không thay bằng in-memory để làm test pass. Các database kiểm thử disposable, không restore/xóa DB người dùng.

Các nhóm test hiện có hỗ trợ đánh giá:

| Nhóm | Bằng chứng chính | Không chứng minh |
|---|---|---|
| API integration | Auth smoke, admin/feedback, instructor ownership, student discovery, availability, report | Mọi error/message, mọi controller happy-path và race |
| Task/import parity | Quyền task, version/move/order, board default/delete, cleanup/restore, notifications DB; CSV/XLSX/reactivation | Toàn validation/warning/workbook và realtime delivery |
| Backup parity/cron | Dump/download/restore disposable, admission, scheduling/retention, validation/recovery | Mọi cron/timezone, cross-instance ownership hoặc vận hành production |
| Contract/route | Branding, 192 concrete routes, bỏ synthetic fallback | Semantics tương đương chỉ vì schema/route trùng |

Không thêm test mới để “xác nhận” các F01–F20 trong lượt này; chúng được ghi thành ca tái hiện cần thực thi. Test suite pass và source audit có lỗi không mâu thuẫn: các nhánh sai chưa được suite hiện tại bao phủ đủ.

## Checklist trước khi ký nghiệm thu

### Ưu tiên sửa và xác minh

1. Khóa quyền và toàn vẹn dữ liệu: F19, F05, F04, F06, F10. Viết regression test thất bại trước, sửa scoped implementation, kiểm PostgreSQL thật và concurrency.
2. State machine học kỳ/meeting: F01, F02, F07, F08; kiểm retry, rollback và cleanup/notification.
3. Timeline/grade/export: F09–F11; fixture nhiều kỳ/môn/instructor, decimal rounding và workbook cell types.
4. Notification end-to-end: F18; giữ Spring STOMP/SockJS contract FE, không thay bằng SignalR chỉ để kết nối được.
5. Filter/DTO/error tương thích: F03, F12–F17, F20; quyết định riêng thay đổi refresh bảo mật và ghi rõ nếu không giữ nguyên Java.

### Bộ tiêu chí áp cho từng method–route

- [ ] Cùng method/path alias, path/query parameter, default, multipart field và content type.
- [ ] Đủ bốn role, account/profile inactive, mustChangePassword, owner/non-owner, group reassignment.
- [ ] Valid request, missing/blank/null, enum invalid, range/length, duplicate và missing resource.
- [ ] So HTTP status, envelope code/message/data, null omission, field names, enum/map keys, timestamp và pagination.
- [ ] So dữ liệu DB trước/sau, transaction rollback, optimistic version, retry idempotency và khóa cạnh tranh.
- [ ] So side effects: refresh revoke/blacklist, pending cleanup, task assignment/activity, notification DB và STOMP.
- [ ] File: filename, Content-Disposition, content type, BOM/newline, sheet/header/typed cells, timezone và thứ tự rows.
- [ ] Golden test Java và .NET dùng cùng fixture độc lập; không lấy OpenAPI làm response oracle thay runtime.

### Các khu vực R cần mở rộng kiểm chứng

Imports cần đầy đủ header aliases, multi-sheet/row numbering, merged context, duplicate identity, inactive reactivation, warning/error counts, partial success và recovery. Backup cần cron/timezone edge và multi-instance admission/recovery trên môi trường disposable. Task cần mọi no-op/version/error/permission/closed-term combination. Feedback cần optimistic update race và privacy cả aggregate/export. Catalog cần enum/numeric binding, wildcard search, sort ties và null fields. Các controller đọc grade/submission/report cần dữ liệu legacy, reassignment, missing snapshots và timezone edge.

### Điều kiện cuối

Chạy lại toàn bộ .NET tests sau khi sửa; bổ sung differential tests cho đủ API; chạy Docker từ PostgreSQL rỗng với migrations/seed; chạy frontend mới nhất chỉ đổi NEXT_PUBLIC_API_BASE_URL, không sửa API client/DTO/UI để che sai lệch. Chỉ khi các bước này đạt mới kết luận backend thay thế Java được trực tiếp. Audit hiện tại **chưa cho phép đưa ra kết luận đó**.
