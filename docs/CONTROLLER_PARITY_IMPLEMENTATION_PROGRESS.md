# Tiến độ sửa nghiệp vụ theo tài liệu Java đã rà soát

Ngày cập nhật: 2026-09-25.

## Kết luận hiện tại

Đã sửa và kiểm thử thêm các nhánh nghiệp vụ của **15 controller Java**: GroupInvitation, GroupJoinRequest, GroupProblem, AdminUser, Profile, AdminProblem, Problem, ProblemDomain, MilestoneSubmission, AdminTerm, GroupMeeting, InstructorGroupBoard, CourseMilestone, nhánh timeline của GroupController và MilestoneGradeMatrix. **Không có nghĩa 15 controller này đã được nghiệm thu toàn bộ; 37/37 controller chưa được chứng nhận tương đương.**

Bộ test .NET hiện có: **190 passed, 0 failed, 0 skipped**. Baseline trước đợt sửa có 66 test; bổ sung **124 test cases** trong 10 lớp bên dưới. Các test tích hợp chạy WebApplicationFactory và PostgreSQL 16/Testcontainers, áp dụng SQL V1–V33. Không sử dụng database người dùng, không sửa Java/FE, không đổi secret.

Nguồn chuẩn: [đặc tả](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md), [HTTP/DTO](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md), [đính chính lần 2](D:/Flowzy-api/docs/CONTROLLER_DOCUMENTATION_REVIEW_2.md). [Audit F01–F20](D:/Flowzy-api/docs/FLOWZY_CONTROLLER_PARITY_AUDIT.md) là lịch sử trước sửa, không phải danh sách lỗi runtime hiện tại.

## Những phần đã triển khai và có test mới

### MilestoneGradeMatrixController — F10/F11, một phần F18

- Sáu API dùng `GradeMatrixService → GradeMatrixRepository`; tách phần export thành partial service, không còn truy vấn EF trực tiếp trong service. Giữ route/verb/envelope/messages; các route `/api/instructor/**` vẫn INSTRUCTOR-only, route group cho authenticated rồi kiểm role/resource trong service như Java.
- DTO score/studentId/contributionPercent nullable để missing field không biến thành 0. Bổ sung required, score ≥0, contribution từng người 0–100, items không rỗng, feedback ≤5000, reason ≤1000; decision theo enum Java (không tự uppercase giá trị sai). Không áp quy tắc tổng contribution bằng 100 vì Java không có quy tắc đó.
- Grade khóa instructor (phối hợp timeline edits), rồi group; contributions/agreement khóa group và revision. Transaction bao group grade/member scores/revision/agreements/notifications; tạo revision đầu tiên cũng được group lock bảo vệ. Java khóa revision ở first-grade, Flowzy không bắt lại unanimous agreement khi sửa grade đã có. Các khóa bổ sung là bảo vệ invariant cạnh tranh, chưa chứng minh mọi race liên miền.
- Grade kiểm milestone ACTIVE/weight>0, group ACTIVE, term/course trim không phân biệt hoa thường và cùng assigned instructor. Contribution rows phải đúng **tập** active member, không chấp nhận thừa sinh viên cũ; lỗi thiếu/thừa trả 400 message Java. Lần đầu cần unanimous AGREE gồm leader; sửa grade không yêu cầu lại agreement. Grade snapshot/individual scores lưu atomic, individual tính divide scale8 rồi scale4 HALF_UP.
- Contribution ghi từng active member, không tự xóa stale rows ngoài tập (Java cũng để lại; grade sẽ từ chối). Duplicate error chứa student id. Mỗi lần submit tăng revision, xóa agreements của revision trong cùng transaction. Leader không tự được AGREE. REQUEST_CHANGES chặn mọi response tiếp cho đến revision mới; reason được trim và AGREE clear reason.
- Student contribution/agreement giữ closed-term 409 bằng StudentTermWriteGuard; instructor grade không bị tự thêm closed-term gate. Summary chỉ tính agreements của active members, trong khi guard "đã có REQUEST_CHANGES" xét tất cả agreements như Java. Grade đã có báo approvedCount = requiredCount, kể cả dữ liệu agreement lịch sử thiếu.
- Matrix trả 409 khi chưa assigned instructor. Nếu owner hiện tại không có visible milestone nhưng scope có visible milestone của owner khác/null, trả owner-scope conflict; không conflict nếu owner hiện tại đã có visible milestone. Visible bỏ ARCHIVED/INACTIVE, giữ CLOSED; sort term/course/position/id, member theo studentCode case-insensitive.
- Matrix complete đòi ít nhất một column và mọi column gradeComplete; không suy từ member rows. Member row có zero milestone vẫn complete=true, nhưng matrix=false. Total làm tròn tỷ lệ scale8 rồi từng hạng tử scale4 HALF_UP trước khi cộng; không cộng tất cả số chưa làm tròn. Giữ snapshot max/weight của score.
- CSV query group ACTIVE/có membership, exact optional term/course, groupId phải thuộc instructor scope; milestone headers được lọc cùng term/course và sort Java. Row dùng matrix riêng của group, blank ở milestone ngoài scope; giữ BOM, CRLF không nhân đôi và attachment filename. Giữ cách ghép/quote header title của Java, chưa tự sửa các CSV edge case nguồn.
- XLSX dùng `RAW`, freeze hàng đầu, bold header, cột Tên Nhóm/Họ & Tên/MSSV/Email (đuôi FPT), mỗi milestone một cột, Final; số thật với format `0.0###`, blank Final khi member row chưa complete, autofilter/autosize theo Java. Không coi byte ZIP workbook phải giống POI.
- Persist notification revision/changes requested/agreed/graded với action OPEN_GRADES, recipient/event keys theo Java. Không gửi grade notice khi score (gồm scale BigDecimal)/feedback không đổi; vẫn cập nhật gradedAt/snapshots như Java. Key CONTRIBUTION_AGREED chỉ dùng revision entity id, không số revision, giữ dedup nguồn. **Chưa phát STOMP sau commit**; timestamp text key chưa differential với Java runtime.
- [14 ca PostgreSQL/HTTP](D:/Flowzy-api/tests/Flowzy.Tests/GradeMatrixParityTests.cs): flow đầy đủ/notification/grade retry; REQUEST_CHANGES/reset; contribution exact set và inactive agreement summary; 2 DTO score cases; DTO/membership errors; scope/closed/role precedence; empty matrix/owner conflict; concurrent revisions/agreements/grades; first-grade tranh revision; weighted rounding và group-grade completeness; rollback member score; rollback revision/agreements khi notification lỗi; CSV/XLSX/header/numeric cell.
- 11 ca đầu pass tập trung; thêm 3 ca pass trong full suite 190. Chưa differential toàn bộ response/file với Java runtime, XLSX style/layout mọi tổ hợp, JSON coercion/decimal overflow/legacy nullable snapshots, instructor reassignment/membership/term close races. F18 realtime vẫn mở; chưa ký nghiệm thu controller.

### CourseMilestoneController và GroupController timeline — F09, nhánh F04, một phần F18

- Toàn bộ action CourseMilestone và GET group milestones dùng `CourseMilestoneService → CourseMilestoneRepository`. Xóa logic timeline cũ khỏi PortedDomainService và GroupOperationsService; chưa tuyên bố các action khác của GroupController đã sửa.
- DTO nhận cả deadlineAt/dueDate; title/term/course giữ maxlength/required message Java, weight 0–100 với message riêng cho min/max, maxScore ≥0.01, position ≥0. Status update từ chối lowercase/giá trị lạ thành malformed JSON; giữ enum ordinal theo cấu hình Jackson mặc định. Deadline null được kiểm trong service, đúng thứ tự lỗi create/update.
- Hai prefix khác quyền: `/api/instructor/milestones` chặn non-INSTRUCTOR trước model validation; `/api/course-milestones` chỉ yêu cầu authentication rồi service xử lý role/resource/error. Outcomes cũ trả 410 theo quyền prefix. GET group timeline dùng cùng service, trả message role/group lookup Java, không gán ADMIN quyền xem.
- Create kiểm account/instructor ACTIVE, kỳ tồn tại và OPEN, scope có ít nhất một group được giao (không tự lọc group ACTIVE/member/locked). Trim term/course/title, giữ nguyên description, maxScore mặc định 10, position max+1 trong scope không phân biệt hoa thường và tính cả ARCHIVED. Title unique không phân biệt hoa thường trong owner/scope, kể cả item ARCHIVED; tổng weight chỉ tính ACTIVE, null tính 0.
- Mọi mutation chạy transaction; khóa instructor để serialize create/update/delete cùng owner và ngăn hai request cộng tổng weight vượt 100. Đây là gia cố cạnh tranh của Flowzy trên invariant Java; chưa coi là chứng minh race với các service grade/close khác chưa được port đầy đủ.
- Update chỉ owner; không tự thêm điều kiện term OPEN hay vẫn còn assigned group vì Java không kiểm các điều kiện đó ở update. Kiểm maxScore không thấp hơn score đã có ở cả legacy grades và matrix group grades; omitted description/weight thành null, maxScore/position/status giữ giá trị cũ khi null. Khi deadline đổi, late = submittedAt > deadline cho mọi submission, bằng deadline không late; transaction bao thay đổi milestone/notification/late flags.
- Delete là ARCHIVED, không xóa submission/grade. Owner vẫn đọc được ARCHIVED. Student list/group list ẩn ARCHIVED và INACTIVE nhưng giữ CLOSED; student detail chỉ chặn ARCHIVED, do đó INACTIVE vẫn đọc được nếu đúng current instructor/group scope như Java.
- Instructor list filter tùy chọn trim/case-insensitive, sort term/course/position/id. Student không filter hợp nhất/dedup timeline nhóm; một filter lẻ trả 400, cặp filter dùng membership term/course **case-sensitive** như repository Java. Không tự thêm student/group ACTIVE hay open-term guard cho đọc; nhóm chưa có instructor trả list rỗng.
- Notification create và update ACTIVE gửi leader của tất cả group trong owner/scope, dedup recipient và lọc account ACTIVE, action OPEN_MILESTONE, metadata/title/body theo Java. Reload timestamp sau SAVE để key dùng timestamp DB. Chưa phát STOMP realtime và chưa differential timestamp key từng byte với Hibernate runtime.
- [18 test cases PostgreSQL/HTTP](D:/Flowzy-api/tests/Flowzy.Tests/CourseMilestoneParityTests.cs): alias/default/raw description/position/notification; 6 DTO cases chạy cả create/update; maxlength/enum; list-detail visibility/filter/sort; role/prefix/lookup/error precedence; close/reassignment update/archive; title/weight/null/concurrency; 2 loại grade bảo vệ maxScore và giữ history; late flag boundary; notification inactive recipient/state; rollback create khi notification lỗi; rollback update khi late flag lỗi.
- Lượt đầu fixture dùng tên group cố định vi phạm unique normalized name trong cùng term; đã sửa tên riêng từng fixture. 15 ca tập trung pass, sau bổ sung 3 ca, toàn bộ 18 pass trong full suite 176. Không sửa constraint/migration để lách kiểm thử.
- Chưa chạy differential Java runtime cho toàn bộ hai prefix/group route, mọi tổ hợp duplicate alias/JSON coercion/decimal precision, missing-profile/legacy null fixtures hoặc race update grade/close/reassignment liên miền; F18 realtime vẫn mở. Chưa nghiệm thu toàn controller hoặc frontend.

### GroupMeetingController — F07/F08, một phần F18

- Chuyển đủ 8 API sang `GroupMeetingService → GroupMeetingRepository`; service không truy cập DbContext trực tiếp. Giữ các route, verb và success message của Java.
- Availability chỉ dành cho student member; chỉ AVAILABLE trong tương lai, tăng dần startAt. GET meeting/list cho student member, mentor được giao và instructor được giao; không tự cấp ADMIN quyền đọc.
- Booking chỉ leader, giữ closed-term guard, khóa group và slot, kiểm quota tối đa 2 SCHEDULED/COMPLETED, slot tương lai/đúng mentor/AVAILABLE. Slot chuyển BOOKED cùng transaction tạo meeting và notification.
- Direct create/update chỉ mentor được giao, khóa mentor chống overlap non-CANCELED giữa các nhóm. Giữ chính sách giờ bắt đầu 00/30 không giây, đúng 60 phút; không tự cấm lịch quá khứ hoặc kỳ CLOSED cho mentor. Update bỏ qua note null, blank note xóa note; HTTP update vẫn yêu cầu meetLink theo Java DTO.
- Evidence: member bất kỳ có student/account ACTIVE, sau giờ kết thúc, kỳ OPEN, SCHEDULED và chưa có evidence. Lưu URL trim, người gửi/thời gian, COMPLETED và notification mentor/instructor trong transaction; không giới hạn riêng leader.
- Cancel chỉ mentor đang được giao, từ chối completed/evidence; slot chuyển CANCELED, không mở lại AVAILABLE. Confirm chỉ leader/mentor, slot đã bắt đầu; direct meeting không có slot không được confirm. Giữ timestamp xác nhận đầu tiên, không tự COMPLETED sau hai bên confirm.
- Giữ đặc điểm Java cần lưu ý bảo mật: cancel meeting đã CANCELED và confirm meeting đã COMPLETED trả no-op **trước kiểm tra actor trong service**. Controller yêu cầu đăng nhập nhưng không có class role filter riêng như bản Flowzy cũ. Chưa thay đổi hành vi này thành hardening hoặc coi đó là bảo mật đã đạt nghiệm thu.
- Notifications được ghi cùng transaction, chỉ recipient ACTIVE, metadata/action/eventKey theo từng thao tác. Đọc lại timestamp sau SAVE vì trigger PostgreSQL thay updated_at; key dùng ISO fraction kiểu Instant ở độ chính xác PostgreSQL để confirm retry không sinh notice trùng. Đây là điều chỉnh tính nhất quán DB; chưa differential eventKey từng byte với Hibernate runtime. **F18 STOMP/after-commit fan-out vẫn chưa hoàn tất.**
- [14 ca PostgreSQL/HTTP](D:/Flowzy-api/tests/Flowzy.Tests/GroupMeetingParityTests.cs): đủ 8 route, actor/lookup/validation/time/quota/closed-term guards, note preservation, evidence/cancel/confirm transitions, notification recipients và retry, slot cạnh tranh, mentor overlap cạnh tranh, group quota cạnh tranh và rollback khi notification insert lỗi.
- Fixture trùng mã học kỳ sau khi đóng kỳ đã sửa thành mã duy nhất. Test dedup đã phát hiện timestamp DB trigger và đã pass sau sửa. Chưa phủ mọi tổ hợp DTO validation/coercion (đặc biệt IValidatableObject và property errors đồng thời), race reassignment/availability liên miền, terminal no-op actor matrix hoặc frontend/differential Java runtime.

### InstructorGroupBoardController — F13

- Hai API dùng `InstructorBoardService → InstructorBoardRepository`; tách query và transaction khỏi service.
- List, summary và course counts đều chỉ xét group ACTIVE, kỳ OPEN, có ít nhất một member. Không tự yêu cầu member ACTIVE hoặc nhóm unlocked khi Java không yêu cầu.
- Search theo group name/no/project và member fullName/studentCode/account email/className; trim/lowercase và giữ SQL LIKE wildcard semantics. Page sort createdAt DESC rồi id DESC; member LEADER trước, tiếp joinedAt/id.
- Assignment ALL/AVAILABLE/MINE/OTHER giữ uppercase như Spring enum; sai hoặc lowercase trả `Invalid parameter format: assignment`. Paging giữ message Java. Term/course/search chuẩn hóa trim/lowercase.
- Summary chỉ chịu term/course, không search/assignment. Course counts chỉ chịu term, không bị course/search/assignment đang chọn cắt mất các môn khác.
- Claim yêu cầu instructor/account ACTIVE, group tồn tại/kỳ OPEN/group ACTIVE/có member. Claim của chính owner là no-op, không UPDATE timestamp; owner khác trả 409. Khóa group rồi term để thống nhất thứ tự với AdminTerm close của Flowzy (Java claim khóa term trước group); giữ nghiệp vụ và transaction, chưa kiểm hết race liên miền.
- [10 ca PostgreSQL/HTTP](D:/Flowzy-api/tests/Flowzy.Tests/InstructorBoardParityTests.cs): eligibility/search/summary/course scope/order/page/member mapping, 5 invalid query cases, role/inactive guard, closed/inactive/empty group guards, locked group được claim, hai instructor tranh một nhóm và retry không đổi owner/timestamp.
- Chưa differential toàn response với Java runtime; chưa failure-injection claim, missing-profile/orphan-term fixture hoặc mọi race close/archive/reassignment. Không coi 2 API đã ký nghiệm thu tuyệt đối.

### AdminTermController — F01/F02, một phần F18 (đợt tiếp theo)

- Chuyển đủ 5 API sang `AcademicTermService → AcademicTermRepository`; xóa logic kỳ học cũ khỏi PortedDomainService.
- Create chuẩn hóa mã, trả lại kỳ OPEN đã có, từ chối kỳ CLOSED, giữ message kỳ OPEN đang cản tạo kỳ mới. Unique index PostgreSQL bảo vệ khi hai request tạo kỳ đồng thời.
- Close khóa nhóm theo id trước rồi khóa kỳ, đọc lại trạng thái sau khóa; close lại trả snapshot cũ. Chỉ hủy invitation/join request PENDING trong kỳ, giữ nguyên lịch sử khác.
- Tạo feedback riêng MENTOR/INSTRUCTOR cho từng member nếu được giao, không tạo trùng; không lọc mất student INACTIVE. Notification lưu trong cùng transaction, chỉ recipient ACTIVE, dedup theo kỳ/student; realtime F18 vẫn chưa hoàn tất.
- Archive yêu cầu kỳ CLOSED, đưa mọi nhóm trong kỳ thành INACTIVE kể cả nhóm rỗng. Không chờ feedback. Student có membership kỳ OPEN khác được ACTIVE cả profile/account, kể cả group của kỳ đó INACTIVE như query Java. Student còn lại bị INACTIVE và xóa refresh token; trường hợp cả hai đã INACTIVE được đếm riêng và không xóa thêm token.
- Archive transaction bao cả group/profile/account và token deletion; lỗi bất kỳ bước nào rollback toàn bộ. Response giữ `skippedPendingFeedbackStudents=0` và counts theo Java.
- Delete khóa kỳ và từ chối history group/feedback với message gốc. DTO create/paging/ADMIN authorization giữ contract.
- [9 ca PostgreSQL/HTTP](D:/Flowzy-api/tests/Flowzy.Tests/AcademicTermParityTests.cs): retry create/close, pending cleanup, feedback/notification count, history/delete, archive/reactivation/token counts, concurrent close/create, rollback close, rollback archive, validation/role matrix, existing submitted feedback, inactive notification recipient và nhóm không có student.
- 6 ca đầu đã chạy riêng và pass; cả 9 ca pass trong full suite 134 tests. Fixture ban đầu dùng mã GUID chữ thường nhưng API chuẩn hóa uppercase đã được sửa; assertion thời gian so cùng thời điểm với sai số dưới 1 microsecond vì PostgreSQL lưu microsecond, không buộc bằng chuỗi fractional seconds trong bộ nhớ.
- Nhánh close đăng ký kỳ legacy khi có group nhưng chưa có term đã port theo Java; chưa integration test nhánh này vì FK V19/V33 không cho fixture group mồ côi. Không bỏ FK/migration thật để ép test. Chưa differential toàn bộ 5 API với Java runtime, chưa chứng minh mọi race với các domain khác còn chưa sửa.

### GroupInvitationController / GroupJoinRequestController — F05/F06, một phần F18

- Tách `GroupMembershipService → IGroupMembershipRepository → GroupMembershipRepository`.
- Trạng thái lưu DB là ACCEPTED/CANCELED theo CHECK constraint gốc, không còn APPROVED/CANCELLED trong các nhánh đã chuyển.
- Quyền leader/admin quản lý invitation; mentor được giao đọc join requests; route mine/accept/decline giữ STUDENT theo SecurityConfig.
- Hết hạn 72 giờ, tối đa 6 thành viên, kiểm trạng thái nhóm, kỳ và isLock đúng từng thao tác. Reject/cancel join không tự thêm kiểm expiry/isLock; decline/cancel invitation không tự thêm isLock.
- Accept/approve khóa student rồi group, kiểm lại trạng thái sau khóa, lưu thành viên và cleanup cùng transaction. Chỉ dọn pending cùng term/course; môn khác giữ nguyên.
- Pending join hết hạn được hủy và flush trước khi tạo request mới để thỏa partial unique index.
- Lưu notification cho recipient ACTIVE, metadata/action/eventKey tương ứng Java trong transaction. **Chưa hoàn tất phát STOMP sau commit (F18).**
- [15 test cases](D:/Flowzy-api/tests/Flowzy.Tests/GroupMembershipParityTests.cs): role/status/errors, expiry, quota, scope cleanup, closed/locked branches, notification DB, accept–approve cạnh tranh cùng student, hai approve tranh chỗ cuối. Đây là coverage tập trung, không phải toàn bộ tổ hợp hoặc differential Java runtime.

### GroupProblemController — F14

- Tách `GroupProblemService → GroupProblemRepository`; toàn bộ 6 action chuyển sang service mới, xóa implementation cũ khỏi GroupOperationsService.
- HTTP STUDENT-only như Java SecurityConfig; đọc proposals cho member, ghi chỉ leader có membership, giữ student closed-term guard.
- Select phân biệt không tồn tại / không ACTIVE / không OFFICIAL bằng message gốc.
- Propose sinh `SP-{groupId}-{8 ký tự UUID}`, giữ nguyên title/statement; insert problem và cập nhật selection trong transaction có khóa group.
- Update/delete kiểm owner (404), source (400), đã review (409); delete flush bỏ selection trước xóa problem.
- DTO giữ maxlength/required/difficulty binding; selection thiếu problemId trả validation 400.
- [8 test cases](D:/Flowzy-api/tests/Flowzy.Tests/GroupProblemParityTests.cs): đủ 6 action, quyền, kỳ đóng, nhóm locked không chặn chọn problem, lỗi owner/source/status, idempotent clear, raw text/code, rollback khi trigger test cố tình làm bước selection thất bại. Trigger chỉ tồn tại trong DB tạm và được xóa trong finally.
- Response group detail tái sử dụng mapper GroupOperationsService: các sai lệch recruitment metadata của F03 còn tồn tại, nên chưa ký xác nhận toàn response controller này.

### AdminUserController / ProfileController — F16

- Create/update admin user từ chối profile loại khác ngay cả khi đã có profile đúng; giữ message và thứ tự required/profile mismatch của Java.
- Self-profile có giới hạn tên, điện thoại, ngành, khóa, lớp, công việc, công ty, LinkedIn, phòng ban; yearsOfExperience không âm.
- Gender JSON bị giới hạn theo Java enum; malformed field JSON trả `Malformed JSON request`, không bị coi là field validation thông thường. Giữ hỗ trợ ordinal theo cấu hình Jackson mặc định; chưa chạy differential riêng cho mọi coercion Jackson.
- Bổ sung cùng giới hạn vào nested student/mentor/instructor profile DTO dùng khi admin tạo/cập nhật.
- ADMIN/missing profile được xử lý trước blank fullName như Java. Trường email/code/role/status không được sửa qua self-profile.
- [22 test cases](D:/Flowzy-api/tests/Flowzy.Tests/ProfileContractParityTests.cs): 9 maxlength, số năm âm, gender sai, trim/clear/ignore protected fields, role/profile error precedence, create/update mismatch không ghi DB, missing profile.
- Chưa phủ toàn validation email/password, enum role/status, nullable mustChangePassword và transaction password/revoke. Không coi cả hai controller đã hoàn tất.

### ProblemController / ProblemDomainController / AdminProblemController — F15/F20

- Query enum difficulty/sourceType/status tại catalog từ chối giá trị lạ và chữ thường với `Invalid parameter format: {parameter}`. Chuỗi rỗng tương đương thiếu filter, giá trị hợp lệ được trim theo converter Spring.
- Domain sort `createdAt DESC, id DESC`.
- Admin PATCH giữ domain hiện tại đã INACTIVE khi gửi lại cùng code; chuyển sang domain INACTIVE khác vẫn bị từ chối; blank code clear cả navigation và FK.
- [11 test cases](D:/Flowzy-api/tests/Flowzy.Tests/ProblemCatalogParityTests.cs): enum errors, filter hợp lệ/rỗng, sort tie, retain/reject/clear domain và không ghi dữ liệu khi request lỗi.
- Các API admin problem khác và mọi combination search/filter/DTO chưa được chứng minh đầy đủ trong đợt này. PortedDomainService vẫn cần tách repository.

### MilestoneSubmissionController — F19

- Tách 3 API đọc sang `MilestoneSubmissionService → MilestoneSubmissionRepository`; không bật lại POST/PUT đã comment bên Java.
- Detail/group list kiểm **giảng viên hiện tại của group**, không dùng owner milestone cũ. ADMIN và student leader/member đọc được; mentor/outsider không được.
- Milestone list yêu cầu instructor sở hữu milestone và chỉ lấy bài của group còn được giao instructor đó; giữ message và thứ tự resource lookup trước role check.
- Không tự thêm sort vào query khi repository Java không chỉ định sort; không tự ẩn dữ liệu legacy của milestone ARCHIVED.
- [3 test cases](D:/Flowzy-api/tests/Flowzy.Tests/LegacySubmissionParityTests.cs): reassignment/owner filtering, role matrix, nullable assignment, resource-not-found precedence và grade snapshot fields. `maxScore` trong schema V33 NOT NULL; không thay SQL gốc để tạo trường hợp legacy null không thể xảy ra dưới schema này.
- Lần chạy tập trung ban đầu lỗi một assertion dùng sai tên `gradeMaxScore`; sửa test theo Java DTO (`maxScore`). Toàn bộ 3 ca đã pass trong lần chạy full suite 125 tests.

## Bảng theo dõi đủ 37 controller

“Đã sửa nhánh” nghĩa là có implementation/test nêu trên, **không** đồng nghĩa nghiệm thu toàn controller. “Chưa sửa đợt này” giữ trạng thái baseline và cần tiếp tục kiểm tra.

| # | Controller Java | Trạng thái đợt này / phần tiếp theo |
|---|---|---|
| 1 | AcademicTermController | Chưa sửa đợt này; golden JSON/count/sort. |
| 2 | AdminFeedbackController | Chưa sửa đợt này; export/header/ẩn danh. |
| 3 | AdminGroupController | Chưa sửa đợt này; đủ filter/labels. |
| 4 | AdminProblemController | Đã sửa nhánh F15 domain PATCH; tiếp tục DTO/create/review/status. |
| 5 | AdminTermController | Đã sửa F01/F02, 9 test; còn F18 realtime, legacy fallback và race liên miền chưa phủ. |
| 6 | AdminUserController | Đã sửa profile contract F16; tiếp tục enum/null/password/transaction. |
| 7 | AuthController | Còn F17; giữ kiểm ACTIVE refresh, không âm thầm hạ bảo vệ. |
| 8 | BackupController | Chưa sửa đợt này; multi-instance/scheduling/restore release checks. |
| 9 | CourseMilestoneController | Đã sửa F09, 18 test chung với group timeline; notification DB có test, còn F18 realtime/differential/race liên miền. |
| 10 | DashboardController | Còn F12: counts, enum keys, project fallback, recruitment labels. |
| 11 | FeedbackController | Chưa sửa đợt này; differential/concurrency. |
| 12 | GroupController | Đã sửa nhánh timeline dùng CourseMilestoneService, có test; còn F03/F04/F18 ở filters, metadata, create atomicity, role/criteria và realtime. |
| 13 | GroupInvitationController | Đã sửa F05/F06, 15 ca chung; còn realtime F18 và mọi nhánh chưa phủ. |
| 14 | GroupJoinRequestController | Đã sửa F05/F06, 15 ca chung; còn realtime F18 và mọi nhánh chưa phủ. |
| 15 | GroupMeetingController | Đã sửa F07/F08, 14 test; notification DB có test, còn F18 realtime và differential/race/validation edge cases. |
| 16 | GroupProblemController | Đã sửa F14, 8 ca; còn phụ thuộc group detail F03 và race/differential sâu hơn. |
| 17 | GroupRecruitmentRoleController | Chưa sửa đợt này; golden auth/JSON. |
| 18 | GroupTaskController | Chưa sửa đợt này; exhaustive errors/realtime. |
| 19 | ImportController | Chưa sửa đợt này; parser validation/recovery/all row errors. |
| 20 | InstructorGroupBoardController | Đã sửa F13, 10 test; còn differential và race liên miền/failure injection. |
| 21 | InstructorProblemController | Chưa sửa đợt này; invalid DTO/race review. |
| 22 | InstructorSubmissionController | Chưa sửa đợt này; test hiện có chạy lại; full DTO/filter differential. |
| 23 | MentorAvailabilityController | Chưa sửa đợt này; full validation/race. |
| 24 | MentorMeetingReportController | Chưa sửa đợt này; golden workbook/header. |
| 25 | MilestoneGradeController | Chưa sửa đợt này; legacy/null/authorization. |
| 26 | MilestoneGradeMatrixController | Đã sửa F10/F11, 14 test; notification DB có test, còn F18 realtime, differential/export edge cases và race liên miền. |
| 27 | MilestoneSubmissionController | Đã sửa F19, 3 ca; tiếp tục differential toàn response. |
| 28 | NotificationController | Còn F18 STOMP MESSAGE fan-out/after-commit/SockJS. |
| 29 | ProblemController | Đã sửa query enums F20; còn exhaustive filters/paging/errors. |
| 30 | ProblemCriteriaController | Chưa sửa đợt này; golden JSON/sort ties. |
| 31 | ProblemDomainController | Đã sửa sort F15/enums F20; còn differential search. |
| 32 | ProblemImportController | Chưa sửa đợt này; complete validation matrix. |
| 33 | ProfileController | Đã sửa F16, 22 ca chung; tiếp tục password/revoke/JSON edge cases. |
| 34 | StudentAccountImportController | Chưa sửa đợt này; duplicate/status/batch counters. |
| 35 | StudentController | Chưa sửa đợt này; paging/enum/errors. |
| 36 | StudentGroupGradeController | Chưa sửa đợt này; legacy/null/zero denominator. |
| 37 | TaskBoardController | Chưa sửa đợt này; full alias/validation golden responses. |

## Tái chạy / bằng chứng

```powershell
dotnet test tests/Flowzy.Tests/Flowzy.Tests.csproj --no-restore --verbosity quiet -p:WarningLevel=0 --logger "trx;LogFileName=controller-parity-matrix.trx"
node scripts/Verify-ControllerAudit.cjs --allow-runtime-changes
```

- [Kết quả full suite 190/0/0](D:/Flowzy-api/tests/Flowzy.Tests/TestResults/controller-parity-matrix.trx), [11 ca matrix tập trung trước khi bổ sung 3 ca](D:/Flowzy-api/tests/Flowzy.Tests/TestResults/grade-matrix-parity.trx). Các kết quả cũ [176/0/0](D:/Flowzy-api/tests/Flowzy.Tests/TestResults/controller-parity-timelines.trx), [158/0/0](D:/Flowzy-api/tests/Flowzy.Tests/TestResults/controller-parity-meeting-board.trx), [134/0/0](D:/Flowzy-api/tests/Flowzy.Tests/TestResults/controller-parity-terms.trx) và [125/0/0](D:/Flowzy-api/tests/Flowzy.Tests/TestResults/controller-parity-implementation.trx) giữ nguyên. `WarningLevel=0` chỉ giảm output của lần build test; không chứng minh đã xử lý analyzer warnings.
- Script kiểm tài liệu vẫn so **413 Java SHA-256**, **37 controller/192 operations**, **174 method excerpts/102 DTO excerpts** với baseline. Option `--allow-runtime-changes` báo riêng các C# đã thay đổi, không coi chúng là lỗi baseline; mặc định vẫn kiểm nghiêm cả Java và C#.
- Manifest lịch sử và TRX `controller-audit.trx` (66 test cũ) không bị thay bằng kết quả mới. Script đó không phải parity test hay kiểm kê đầy đủ các file C# mới thêm.
- Chưa chạy Java-vs-.NET differential cho toàn 192 route, chưa frontend acceptance, chưa port toàn bộ 861 Java tests. Chưa deploy/restart backend đang phục vụ người dùng.

Ưu tiên tiếp theo: F03/F04 nhóm; F12 dashboard; F18 realtime, gồm notification kỳ học/meeting/timeline/grade matrix. Tiếp tục bổ sung coverage và differential cho các controller đã sửa. Mọi mục phải có test và ghi rõ phần còn thiếu trước khi nghiệm thu.
