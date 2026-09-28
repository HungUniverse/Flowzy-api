# Java to .NET port status

Last verification: 2026-09-25. Java source remains unchanged.

## Evidence, not completion claims

- Java inventory: **413 production files, 37 controllers**.
- Runtime .NET routing: **192/192 Java HTTP operations** have concrete MVC controller actions (33 C# controller files; five catalog controllers are consolidated into one).
- The synthetic compatibility fallback has been removed. Unknown routes no longer return schema-shaped success data.
- The embedded Java OpenAPI is a documentation/verification oracle, not proof that response schemas and behavior match.
- Latest full test run: **190 passed, 0 failed, 0 skipped** (2026-09-25 matrix implementation; `tests/Flowzy.Tests/TestResults/controller-parity-matrix.trx`). Previous 176/158/134/125-test results are retained; the historical audit run remains 66 tests in `controller-audit.trx`. Integration tests use disposable PostgreSQL 16 containers with the original V1–V33 SQL migrations.
- Build passes. Analyzer warnings remain.
- This is **not a production-complete or 100%-equivalent backend**. The 861-test Java baseline has not yet been fully ported or rerun side by side against .NET.

## This implementation increment

### Revised-document parity corrections (2026-09-25)

Grade-matrix follow-up: six actions now use GradeMatrixService → GradeMatrixRepository. F10/F11 corrections cover transactions/locking, exact active-member contribution sets, revision/response workflow, first-grade agreement versus existing-grade updates, closed-term/access/error precedence, column-based completeness, staged HALF_UP rounding, scoped CSV and numeric RAW XLSX export. Notifications persist atomically; STOMP is still pending. Fourteen new PostgreSQL/HTTP tests pass, including races and injected failures. Full suite: 190/0/0. Fifteen Java controllers have targeted revised-document changes, not full parity sign-off; runtime differential, legacy/decimal/file edge cases and cross-domain races remain explicit gaps.

Timeline follow-up: CourseMilestone's actions and GroupController's timeline route now share CourseMilestoneService → CourseMilestoneRepository. F09 corrections include DTO dueDate alias/ranges/messages, prefix-specific authorization, Java list-versus-detail visibility, exact membership filters, case-insensitive owner scope, title/active-weight invariants, maximum-grade protection, deadline-driven late flags, archive preservation and transactional notification persistence. Eighteen new PostgreSQL/HTTP cases pass, including concurrent weight allocation and failure-injection rollback. Full suite: 176/0/0. Fourteen Java controllers have targeted revised-document changes, not full sign-off; STOMP, runtime differential, exhaustive JSON cases and cross-domain races remain open.

Latest follow-up: GroupMeeting's eight actions and InstructorGroupBoard's two actions now use dedicated repositories. Fourteen meeting tests cover access, time/DTO rules, booking, evidence, cancellation, confirmation, notification persistence/dedup, concurrent slot/mentor/quota allocation and rollback. Ten board tests cover eligibility, member search, summary/course scope, sort/paging, enum/role/claim guards and concurrent/idempotent claims. All 24 new cases and the full 158-test suite pass. Twelve Java controllers now have targeted revised-document corrections, not complete parity sign-off. STOMP delivery, cross-domain races, exhaustive DTO cases, terminal meeting no-op authorization and runtime Java differential remain explicit verification gaps; see the tracker.

Follow-up: AdminTermController's five actions now use AcademicTermService → AcademicTermRepository. F01/F02 corrections cover create/close idempotency, stable group/term row locks, pending cancellation, feedback snapshots, persisted notification deduplication, archive/reactivation/token revocation and history-protected deletion. Nine new PostgreSQL tests pass, including concurrent close/create and failure-injection rollback for close/archive. Total targeted controller count is now ten, not a complete parity sign-off; realtime delivery and cross-domain races remain open.

See the [37-controller implementation tracker](D:/Flowzy-api/docs/CONTROLLER_PARITY_IMPLEMENTATION_PROGRESS.md) for current status. Added 59 PostgreSQL-backed test cases and corrected membership state/authorization/expiry/quota/scope/locking, group problem transaction/code/raw text/owner errors, profile validation/contracts, catalog enum/domain sort/PATCH, and legacy submission access after instructor reassignment. Three new domain service/repository pairs replace the old combined membership, proposal and submission implementations. Notifications are persisted but realtime delivery remains pending. Nine Java controllers have targeted changes, not full parity sign-off.

### Task API and membership effects

GroupTaskController now invokes real service logic for board/detail, create/update/move/reorder, assignees, archive/restore, checklist, comments, activities and assigned-task filtering.

Tests cover leader/assignee/member permissions, stale versions and competing moves, no-op version preservation, multi-board separation, closed-term 409 responses, assignment cleanup on member removal and archive restoration, activity history, notification recipients/event keys/action parameters, and all principal task mutation routes.

TaskBoardController now uses a dedicated TaskBoardService → TaskBoardRepository. Group row locks serialize lazy default creation and default promotion. Demotions are flushed before promotion to satisfy the PostgreSQL partial unique index. Tests cover reverse promotion, invalid/default archival/deletion, populated-board deletion and validation rollback.

Group removal/leave now join task cleanup in the same transaction. Active assignments are removed with system activity records; archived assignments are retained until restore, matching Java. Admin removal bypasses the student closed-term gate. Notification persistence is present; realtime delivery remains pending.

### Import API

ImportController, StudentAccountImportController and ProblemImportController use an actual background queue and worker, temporary upload files, persisted batch state/errors, admission locking, startup recovery, and isolated row transactions.

Implemented flows: student/mentor account CSV/XLSX, roster-only student account creation/reactivation, problem/domain upserts, workbook-based group membership/leader/mentor creation, templates, batch status and filtered error pagination.

Tests cover mixed successful/invalid/duplicate rows, reactivation preserving password hashes, no group creation from roster-only uploads, problem updates, mentor experience labels, group workbook roundtrip, authorization, downloadable workbook types, strict roster headers, BOM/quoted CSV, row numbers, multi-sheet selection, inherited group fields and Java SMART date parsing.

## Controller audit

The 2026-09-25 source audit is documented in [the Java business specification](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md), [the exact HTTP/DTO appendix](D:/Flowzy-api/docs/FSPARK_HTTP_CONTRACT_APPENDIX.md), and [the Flowzy parity report](D:/Flowzy-api/docs/FLOWZY_CONTROLLER_PARITY_AUDIT.md). All 37 controllers and 192 active method-route pairs are catalogued. The audit records 20 source-backed finding groups affecting 18 controllers; the other 19 controllers are not fully signed off. Critical differences include invalid join/cancel states against SQL constraints, submission authorization after instructor reassignment, term lifecycle, meeting state transitions, grade-matrix transactions/export, and missing notification delivery. No business implementation was changed during this audit.

All 37 Java controllers are mapped to real C# actions in java-source-inventory.csv. **No controller is signed off as fully equivalent solely because its routes exist.**

The [second documentation review](D:/Flowzy-api/docs/CONTROLLER_DOCUMENTATION_REVIEW_2.md) rechecked all 37 controller sections against Java DTOs, services, guards and queries. It corrects the baseline (including mandatory meeting link/task version, membership versus ACTIVE status, term/isLock versus row locks, and grade export/completeness). The previous F08 claim about omitted meetLink was withdrawn; both HTTP contracts require it. Findings F03/F07/F14 were refined with further source evidence. Runtime Java/C# source was not changed and the 66-test result remains the earlier run, not a fresh test execution in this documentation review.

Focused HTTP/PostgreSQL coverage exists for auth smoke, admin users/groups/feedback, feedback, grading, student discovery, instructor problem/submission access, mentor availability/report, and the new task/import flows. Coverage depth varies; remaining authorization branches, error messages, concurrency, side effects and response fields need differential verification.

Dashboard, instructor group board and grade matrix implementations still need substantial business-level verification. Other broad ports also require per-API comparison.

### Backup increment (2026-09-25)

BackupController now uses BackupService → BackupRepository and an injectable PostgreSQL command runner. It implements real cron/timezone scheduling, queued/running admission exclusion, restore exclusion, startup/restore recovery, captured directory/retention settings, bounded 4000-character errors, retention cleanup, and the Java download header/filename shape.

The container now installs PostgreSQL client 16 from the signed [official PostgreSQL APT repository](https://www.postgresql.org/download/linux/debian/). Runtime verification reports pg_dump and pg_restore 16.15 against PostgreSQL server 16.

Cron uses [Cronos](https://github.com/HangfireIO/Cronos) behind a Spring-compatibility adapter (six fields, Spring macros, special-day unions, rejected reversed ranges, IANA/fixed-offset zones, DST gaps/repeated times). Expected timestamps were also evaluated with Spring CronExpression from locally cached Java dependencies. This is targeted differential coverage, not an exhaustive proof for every possible cron/timezone combination.

New PostgreSQL-backed tests cover real dump/download/restore, interrupted historical jobs after restore, manual concurrency, restore exclusion and failure release, scheduled jobs, retention scope, disabled/invalid schedules, exact validation messages, and missing/busy due-run rescheduling.

The standalone scripts/Test-DockerBackup.ps1 smoke test passed with the production API image and a fresh disposable PostgreSQL container: all migrations/seed, login, real production pg_dump, download, real production pg_restore, restored data and recovered job state. It uses no local database volume and removes only its own generated containers/network. The user's local database was not restored or cleared.

## Remaining release blockers

1. Complete per-domain unit/repository/authorization/E2E tests and Java-vs-.NET response/error comparison for all 192 operations.
2. Backup now has working single-instance scheduling/restore and genuine PostgreSQL tests. Cross-instance restore coordination, ownership-aware startup recovery, operational restore access control/maintenance procedures, and exhaustive cron/timezone parity remain release checks. Never exercise restore against a user's database during verification.
3. STOMP currently authenticates CONNECT/SUBSCRIBE only; subscription fan-out, server MESSAGE delivery and SockJS compatibility remain incomplete. Persisted task notifications do not prove frontend realtime compatibility.
4. Group join/invitation student/group locking, quota and same-scope cleanup now have focused concurrency tests. Remaining error/edge paths and realtime notification side effects still require full parity verification.
5. Import recovery/admission, cancellation, all validation/group edge cases, parent-vs-row transaction boundaries and workbook visual formatting are not exhaustively verified. Startup recovery needs a multi-instance ownership design before scaling.
6. Some new services still query FlowzyDbContext directly; complete repository extraction to satisfy strict API → Service → Repository separation.
7. Generated DTO/enum and merged repository inventory entries are not proof of runtime parity. Remaining Pending mappings and generic mappings must be resolved honestly.
8. Run latest frontend acceptance with only NEXT_PUBLIC_API_BASE_URL changed; it has not been performed for this increment.
9. Empty-database Docker startup and backup/restore smoke now pass; repeat on the final release candidate along with full frontend acceptance and local debugging checks.

## Reproduce verification

```powershell
dotnet build Flowzy.sln --no-restore
dotnet test tests/Flowzy.Tests/Flowzy.Tests.csproj --no-restore
```

Docker must be running for PostgreSQL/Testcontainers tests. A transient Docker endpoint discovery failure occurred during one full run; rerunning with Docker reachable passed. No tests were disabled or changed to an in-memory provider.
