# F-Spark — phụ lục HTTP contract và DTO

Ngày chụp source: 2026-09-25. Bao phủ 37 controller, 192 method–route; giữ riêng alias. [Đặc tả nghiệp vụ](D:/Flowzy-api/docs/FSPARK_CONTROLLER_BUSINESS_SPEC.md) giải thích service/guard. Các block sau là trích source Java thực tế, không phải ví dụ tự dựng. Chữ ký giữ tên tham số, required/default và multipart; body giữ status/message/content type/file header. Method không có @PreAuthorize vẫn chịu SecurityConfig và service ownership. Body không thay cho đặc tả service.

## Contract từng method

## 1. AcademicTermController

<a id="c01-m1"></a>

### 1.1. listAvailableTerms

- `GET /api/terms/available`

Nguồn: [AcademicTermController:26](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AcademicTermController.java:26).

```java
public ResponseEntity<APIResponse<List<AcademicTermResponseDto>>> listAvailableTerms()
{
        List<AcademicTermResponseDto> terms = academicTermService.listAvailableTerms();
        return ResponseEntity.ok(APIResponse.success("Available academic terms retrieved successfully", terms));
    }
```

## 2. AdminFeedbackController

<a id="c02-m1"></a>

### 2.1. listFeedbacks

- `GET /api/admin/feedback`

Nguồn: [AdminFeedbackController:34](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminFeedbackController.java:34).

Annotation quyền: `@PreAuthorize("hasRole('ADMIN')")`.

```java
public ResponseEntity<APIResponse<PageResponse<AdminFeedbackResponseDto>>> listFeedbacks(
            @RequestParam(value = "page", defaultValue = "0") int page,
            @RequestParam(value = "size", defaultValue = "20") int size,
            @RequestParam(value = "term", required = false) String term,
            @RequestParam(value = "courseCode", required = false) String courseCode,
            @RequestParam(value = "targetType", required = false) FeedbackTargetType targetType,
            @RequestParam(value = "targetId", required = false) Long targetId,
            @RequestParam(value = "targetSearch", required = false) String targetSearch,
            @RequestParam(value = "status", required = false) FeedbackStatus status
    )
{
        if (page < 0) {
            throw new BadRequestException("Page index must be zero or greater");
        }
        if (size < 1 || size > 100) {
            throw new BadRequestException("Page size must be between 1 and 100");
        }

        Pageable pageable = PageRequest.of(page, size, Sort.by(
                Sort.Order.desc("createdAt"),
                Sort.Order.desc("id")
        ));

        Page<AdminFeedbackResponseDto> feedbacks = feedbackService.getAdminFeedbacks(
                term, courseCode, targetType, targetId, targetSearch, status, pageable
        );

        return ResponseEntity.ok(APIResponse.success("Feedbacks retrieved successfully", PageResponse.from(feedbacks)));
    }
```

<a id="c02-m2"></a>

### 2.2. exportFeedbacks

- `GET /api/admin/feedback/export.xlsx`

Nguồn: [AdminFeedbackController:65](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminFeedbackController.java:65).

Annotation quyền: `@PreAuthorize("hasRole('ADMIN')")`.

```java
public ResponseEntity<byte[]> exportFeedbacks(
            @RequestParam(value = "term", required = false) String term
    )
{
        byte[] body = feedbackService.exportAdminFeedbackXlsx(term);
        String safeTerm = term == null
                ? "term"
                : term.trim().toUpperCase(java.util.Locale.ROOT).replaceAll("[^A-Z0-9_-]", "-");
        return ResponseEntity.ok()
                .header(HttpHeaders.CONTENT_DISPOSITION,
                        "attachment; filename=\"feedback-" + safeTerm + ".xlsx\"")
                .contentType(MediaType.parseMediaType(
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"))
                .body(body);
    }
```

## 3. AdminGroupController

<a id="c03-m1"></a>

### 3.1. listGroups

- `GET /api/admin/groups`

Nguồn: [AdminGroupController:34](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminGroupController.java:34).

Annotation quyền: `@PreAuthorize("hasRole('ADMIN')")`.

```java
public ResponseEntity<APIResponse<PageResponse<GroupSummaryDto>>> listGroups(
            @RequestParam(value = "page", defaultValue = "0") int page,
            @RequestParam(value = "size", defaultValue = "20") int size,
            @RequestParam(value = "search", required = false) String search,
            @RequestParam(value = "status", defaultValue = "ACTIVE") String status
    )
{
        if (page < 0) {
            throw new BadRequestException("Page index must be zero or greater");
        }
        if (size < 1 || size > 100) {
            throw new BadRequestException("Page size must be between 1 and 100");
        }

        GroupStatus statusFilter = parseStatusFilter(status);
        Pageable pageable = PageRequest.of(page, size, Sort.by(
                Sort.Order.desc("createdAt"),
                Sort.Order.desc("id")
        ));
        Page<GroupSummaryDto> groups = groupService.getAdminGroups(search, statusFilter, pageable);
        return ResponseEntity.ok(APIResponse.success("Groups retrieved successfully", PageResponse.from(groups)));
    }
```

## 4. AdminProblemController

<a id="c04-m1"></a>

### 4.1. createProblemDomain

- `POST /api/admin/problem-domains`

Nguồn: [AdminProblemController:25](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:25).

Annotation quyền: `@PreAuthorize("hasRole('ADMIN')")`.

```java
public ResponseEntity<APIResponse<ProblemDomainDto>> createProblemDomain(
            @Valid @RequestBody CreateProblemDomainRequest request
    )
{
        ProblemDomainDto domain = problemBankService.createProblemDomain(request);
        return ResponseEntity.ok(APIResponse.success("Problem domain created successfully", domain));
    }
```

<a id="c04-m2"></a>

### 4.2. updateProblemDomain

- `PATCH /api/admin/problem-domains/{id}`

Nguồn: [AdminProblemController:35](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:35).

Annotation quyền: `@PreAuthorize("hasRole('ADMIN')")`.

```java
public ResponseEntity<APIResponse<ProblemDomainDto>> updateProblemDomain(
            @PathVariable("id") Long id,
            @Valid @RequestBody UpdateProblemDomainRequest request
    )
{
        ProblemDomainDto domain = problemBankService.updateProblemDomain(id, request);
        return ResponseEntity.ok(APIResponse.success("Problem domain updated successfully", domain));
    }
```

<a id="c04-m3"></a>

### 4.3. createOfficialProblem

- `POST /api/admin/problems`

Nguồn: [AdminProblemController:46](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:46).

Annotation quyền: `@PreAuthorize("hasRole('ADMIN')")`.

```java
public ResponseEntity<APIResponse<ProblemDetailDto>> createOfficialProblem(
            @Valid @RequestBody CreateProblemRequest request
    )
{
        ProblemDetailDto problem = problemBankService.createOfficialProblem(request);
        return ResponseEntity.ok(APIResponse.success("Official problem created successfully", problem));
    }
```

<a id="c04-m4"></a>

### 4.4. updateProblem

- `PATCH /api/admin/problems/{id}`

Nguồn: [AdminProblemController:56](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:56).

Annotation quyền: `@PreAuthorize("hasRole('ADMIN')")`.

```java
public ResponseEntity<APIResponse<ProblemDetailDto>> updateProblem(
            @PathVariable("id") Long id,
            @Valid @RequestBody UpdateProblemRequest request
    )
{
        ProblemDetailDto problem = problemBankService.updateProblem(id, request);
        return ResponseEntity.ok(APIResponse.success("Problem updated successfully", problem));
    }
```

<a id="c04-m5"></a>

### 4.5. updateProblemStatus

- `PATCH /api/admin/problems/{id}/status`

Nguồn: [AdminProblemController:67](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:67).

Annotation quyền: `@PreAuthorize("hasRole('ADMIN')")`.

```java
public ResponseEntity<APIResponse<ProblemDetailDto>> updateProblemStatus(
            @PathVariable("id") Long id,
            @Valid @RequestBody UpdateProblemStatusRequest request
    )
{
        ProblemDetailDto problem = problemBankService.updateProblemStatus(id, request);
        return ResponseEntity.ok(APIResponse.success("Problem status updated successfully", problem));
    }
```

<a id="c04-m6"></a>

### 4.6. reviewProblem

- `PATCH /api/admin/problems/{id}/review`

Nguồn: [AdminProblemController:78](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminProblemController.java:78).

Annotation quyền: `@PreAuthorize("hasRole('ADMIN')")`.

```java
public ResponseEntity<APIResponse<ProblemDetailDto>> reviewProblem(
            @PathVariable("id") Long id,
            @Valid @RequestBody ReviewProblemRequest request,
            Principal principal
    )
{
        ProblemDetailDto problem = problemBankService.reviewProblem(id, request, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Proposed problem reviewed successfully", problem));
    }
```

## 5. AdminTermController

<a id="c05-m1"></a>

### 5.1. listTerms

- `GET /api/admin/terms`

Nguồn: [AdminTermController:35](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminTermController.java:35).

Annotation quyền: `@PreAuthorize("hasRole('ADMIN')")`.

```java
public ResponseEntity<APIResponse<PageResponse<AcademicTermResponseDto>>> listTerms(
            @RequestParam(value = "page", defaultValue = "0") int page,
            @RequestParam(value = "size", defaultValue = "10") int size
    )
{
        if (page < 0) {
            throw new BadRequestException("Page index must be zero or greater");
        }
        if (size < 1 || size > 100) {
            throw new BadRequestException("Page size must be between 1 and 100");
        }

        Pageable pageable = PageRequest.of(page, size, Sort.by(
                Sort.Order.desc("createdAt"),
                Sort.Order.desc("id")
        ));
        Page<AcademicTermResponseDto> terms = academicTermService.listTerms(pageable);
        return ResponseEntity.ok(APIResponse.success("Academic terms retrieved successfully", PageResponse.from(terms)));
    }
```

<a id="c05-m2"></a>

### 5.2. createTerm

- `POST /api/admin/terms`

Nguồn: [AdminTermController:56](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminTermController.java:56).

Annotation quyền: `@PreAuthorize("hasRole('ADMIN')")`.

```java
public ResponseEntity<APIResponse<AcademicTermResponseDto>> createTerm(
            @Valid @RequestBody CreateAcademicTermRequest request
    )
{
        AcademicTermResponseDto term = academicTermService.createTerm(request);
        return ResponseEntity.ok(APIResponse.success("Academic term created successfully", term));
    }
```

<a id="c05-m3"></a>

### 5.3. closeTerm

- `PATCH /api/admin/terms/{term}/close`

Nguồn: [AdminTermController:65](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminTermController.java:65).

Annotation quyền: `@PreAuthorize("hasRole('ADMIN')")`.

```java
public ResponseEntity<APIResponse<AcademicTermResponseDto>> closeTerm(
            @PathVariable("term") String termCode,
            Principal principal
    )
{
        AcademicTermResponseDto closedTerm = academicTermService.closeTerm(termCode, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Academic term closed successfully", closedTerm));
    }
```

<a id="c05-m4"></a>

### 5.4. archiveStudents

- `POST /api/admin/terms/{term}/archive-students`

Nguồn: [AdminTermController:77](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminTermController.java:77).

Annotation quyền: `@PreAuthorize("hasRole('ADMIN')")`.

```java
public ResponseEntity<APIResponse<ArchiveTermStudentsResponseDto>> archiveStudents(
            @PathVariable("term") String termCode
    )
{
        ArchiveTermStudentsResponseDto result = academicTermService.archiveStudents(termCode);
        return ResponseEntity.ok(APIResponse.success("Eligible students archived successfully", result));
    }
```

<a id="c05-m5"></a>

### 5.5. deleteEmptyTerm

- `DELETE /api/admin/terms/{term}`

Nguồn: [AdminTermController:88](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminTermController.java:88).

Annotation quyền: `@PreAuthorize("hasRole('ADMIN')")`.

```java
public ResponseEntity<APIResponse<Void>> deleteEmptyTerm(@PathVariable("term") String termCode)
{
        academicTermService.deleteEmptyTerm(termCode);
        return ResponseEntity.ok(APIResponse.success("Academic term deleted successfully", null));
    }
```

## 6. AdminUserController

<a id="c06-m1"></a>

### 6.1. listUsers

- `GET /api/admin/users`

Nguồn: [AdminUserController:38](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:38).

```java
public ResponseEntity<APIResponse<PageResponse<AdminUserSummaryDto>>> listUsers(
            @RequestParam(value = "page", defaultValue = "0") int page,
            @RequestParam(value = "size", defaultValue = "10") int size,
            @RequestParam(value = "search", required = false) String search,
            @RequestParam(value = "role", required = false) Role role,
            @RequestParam(value = "status", required = false) AccountStatus status
    )
{
        if (page < 0) {
            throw new BadRequestException("Page index must be zero or greater");
        }
        if (size < 1 || size > 100) {
            throw new BadRequestException("Page size must be between 1 and 100");
        }
        Pageable pageable = PageRequest.of(page, size, Sort.by(
                Sort.Order.desc("createdAt"),
                Sort.Order.desc("id")
        ));
        Page<AdminUserSummaryDto> users = adminUserService.listUsers(search, role, status, pageable);
        return ResponseEntity.ok(APIResponse.success("Users retrieved successfully", PageResponse.from(users)));
    }
```

<a id="c06-m2"></a>

### 6.2. getUserById

- `GET /api/admin/users/{id}`

Nguồn: [AdminUserController:61](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:61).

```java
public ResponseEntity<APIResponse<AdminUserDetailDto>> getUserById(@PathVariable("id") Long id)
{
        AdminUserDetailDto user = adminUserService.getUserById(id);
        return ResponseEntity.ok(APIResponse.success("User details retrieved successfully", user));
    }
```

<a id="c06-m3"></a>

### 6.3. createUser

- `POST /api/admin/users`

Nguồn: [AdminUserController:68](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:68).

```java
public ResponseEntity<APIResponse<AdminUserDetailDto>> createUser(
            @Valid @RequestBody CreateAdminUserRequest request
    )
{
        AdminUserDetailDto user = adminUserService.createUser(request);
        return ResponseEntity.ok(APIResponse.success("User created successfully", user));
    }
```

<a id="c06-m4"></a>

### 6.4. updateUser

- `PATCH /api/admin/users/{id}`

Nguồn: [AdminUserController:77](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:77).

```java
public ResponseEntity<APIResponse<AdminUserDetailDto>> updateUser(
            @PathVariable("id") Long id,
            @Valid @RequestBody UpdateAdminUserRequest request,
            Principal principal
    )
{
        AdminUserDetailDto user = adminUserService.updateUser(id, request, principal.getName());
        return ResponseEntity.ok(APIResponse.success("User updated successfully", user));
    }
```

<a id="c06-m5"></a>

### 6.5. deleteUser

- `DELETE /api/admin/users/{id}`

Nguồn: [AdminUserController:88](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:88).

```java
public ResponseEntity<APIResponse<Void>> deleteUser(
            @PathVariable("id") Long id,
            Principal principal
    )
{
        adminUserService.softDeleteUser(id, principal.getName());
        return ResponseEntity.ok(APIResponse.success("User deleted successfully", null));
    }
```

<a id="c06-m6"></a>

### 6.6. resetPassword

- `POST /api/admin/users/{id}/reset-password`

Nguồn: [AdminUserController:98](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:98).

```java
public ResponseEntity<APIResponse<Void>> resetPassword(
            @PathVariable("id") Long id,
            @Valid @RequestBody ResetUserPasswordRequest request
    )
{
        adminUserService.resetPassword(id, request);
        return ResponseEntity.ok(APIResponse.success("Password reset successfully", null));
    }
```

<a id="c06-m7"></a>

### 6.7. changePasswordByEmail

- `POST /api/admin/users/change-password`

Nguồn: [AdminUserController:108](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AdminUserController.java:108).

```java
public ResponseEntity<APIResponse<Void>> changePasswordByEmail(
            @Valid @RequestBody AdminChangePasswordRequest request
    )
{
        adminUserService.changePasswordByEmail(request);
        return ResponseEntity.ok(APIResponse.success("Password changed successfully", null));
    }
```

## 7. AuthController

<a id="c07-m1"></a>

### 7.1. login

- `POST /api/auth/login`

Nguồn: [AuthController:58](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java:58).

```java
public ResponseEntity<APIResponse<TokenResponse>> login(@Valid @RequestBody LoginRequest request)
{
        Authentication authentication;
        try {
            authentication = authenticationManager.authenticate(
                    new UsernamePasswordAuthenticationToken(request.email(), request.password())
            );
        } catch (Exception ex) {
            throw new UnauthorizedException("Invalid email or password");
        }

        CustomUserDetails userDetails = (CustomUserDetails) authentication.getPrincipal();
        Account account = userDetails.getAccount();

        // Update last login
        account.setLastLoginAt(Instant.now());
        accountRepository.save(account);

        String accessToken = jwtService.generateAccessToken(userDetails);
        String refreshToken = refreshTokenService.createRefreshToken(account);

        TokenResponse tokenResponse = new TokenResponse(
                accessToken,
                refreshToken,
                "Bearer",
                jwtExpiration / 1000
        );

        return ResponseEntity.ok(APIResponse.success("Login successful", tokenResponse));
    }
```

<a id="c07-m2"></a>

### 7.2. loginWithGoogle

- `POST /api/auth/google`

Nguồn: [AuthController:90](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java:90).

```java
public ResponseEntity<APIResponse<TokenResponse>> loginWithGoogle(@Valid @RequestBody GoogleLoginRequest request)
{
        TokenResponse tokenResponse = googleAuthService.login(request.idToken());
        return ResponseEntity.ok(APIResponse.success("Google login successful", tokenResponse));
    }
```

<a id="c07-m3"></a>

### 7.3. refresh

- `POST /api/auth/refresh`

Nguồn: [AuthController:97](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java:97).

```java
public ResponseEntity<APIResponse<TokenResponse>> refresh(@Valid @RequestBody RefreshTokenRequest request)
{
        RefreshToken token = refreshTokenService.validateRefreshToken(request.refreshToken());
        Account account = token.getAccount();
        CustomUserDetails userDetails = new CustomUserDetails(account);

        String accessToken = jwtService.generateAccessToken(userDetails);
        String newRefreshToken = refreshTokenService.createRefreshToken(account);

        TokenResponse tokenResponse = new TokenResponse(
                accessToken,
                newRefreshToken,
                "Bearer",
                jwtExpiration / 1000
        );

        return ResponseEntity.ok(APIResponse.success("Token refreshed successfully", tokenResponse));
    }
```

<a id="c07-m4"></a>

### 7.4. me

- `GET /api/auth/me`

Nguồn: [AuthController:117](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java:117).

```java
public ResponseEntity<APIResponse<UserInfoResponse>> me(Principal principal)
{
        if (principal == null) {
            throw new UnauthorizedException("Not authenticated");
        }
        Account account = accountRepository.findByEmailIgnoreCase(principal.getName())
                .orElseThrow(() -> new UnauthorizedException("Account not found"));

        InstructorProfileDto instructorProfile = null;
        if (account.getRole() == Role.INSTRUCTOR && account.getInstructor() != null) {
            instructorProfile = new InstructorProfileDto(
                    account.getInstructor().getId(),
                    account.getInstructor().getInstructorCode(),
                    account.getInstructor().getFullName(),
                    account.getEmail(),
                    account.getInstructor().getPhone(),
                    account.getInstructor().getDepartment(),
                    account.getInstructor().getExpertise(),
                    account.getInstructor().getStatus()
            );
        }

        UserInfoResponse userInfo = new UserInfoResponse(
                account.getId(),
                account.getEmail(),
                account.getRole(),
                account.getStatus(),
                account.getMustChangePassword(),
                instructorProfile
        );

        return ResponseEntity.ok(APIResponse.success(userInfo));
    }
```

<a id="c07-m5"></a>

### 7.5. logout

- `POST /api/auth/logout`

Nguồn: [AuthController:152](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/AuthController.java:152).

```java
public ResponseEntity<APIResponse<Void>> logout(jakarta.servlet.http.HttpServletRequest request, Principal principal)
{
        if (principal != null) {
            Account account = accountRepository.findByEmailIgnoreCase(principal.getName()).orElse(null);
            if (account != null) {
                refreshTokenService.revokeAllForAccount(account);
            }
        }
        String authHeader = request.getHeader("Authorization");
        if (authHeader != null && authHeader.startsWith("Bearer ")) {
            String jwt = authHeader.substring(7);
            tokenBlacklistService.blacklistToken(jwt);
        }
        SecurityContextHolder.clearContext();
        return ResponseEntity.ok(APIResponse.success("Logged out successfully", null));
    }
```

## 8. BackupController

<a id="c08-m1"></a>

### 8.1. createBackup

- `POST /api/admin/backups`

Nguồn: [BackupController:44](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:44).

```java
public ResponseEntity<APIResponse<BackupJobDto>> createBackup(Principal principal)
{
        BackupJobDto job = backupService.createManualBackup(principal.getName());
        return ResponseEntity.ok(APIResponse.success("Backup job queued successfully", job));
    }
```

<a id="c08-m2"></a>

### 8.2. restoreBackup

- `POST /api/admin/backups/restore`

Nguồn: [BackupController:50](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:50).

```java
public ResponseEntity<APIResponse<RestoreBackupResponseDto>> restoreBackup(
            @RequestParam("file") MultipartFile file,
            @RequestParam("confirmation") String confirmation,
            Principal principal
    )
{
        RestoreBackupResponseDto result = backupService.restoreBackup(file, confirmation, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Database restored successfully", result));
    }
```

<a id="c08-m3"></a>

### 8.3. listBackups

- `GET /api/admin/backups`

Nguồn: [BackupController:60](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:60).

```java
public ResponseEntity<APIResponse<PageResponse<BackupJobDto>>> listBackups(
            @RequestParam(value = "status", required = false) BackupJobStatus status,
            @RequestParam(value = "page", defaultValue = "0") int page,
            @RequestParam(value = "size", defaultValue = "10") int size
    )
{
        if (page < 0) {
            throw new BadRequestException("Page index must be zero or greater");
        }
        if (size < 1 || size > 100) {
            throw new BadRequestException("Page size must be between 1 and 100");
        }
        Pageable pageable = PageRequest.of(page, size, Sort.by(
                Sort.Order.desc("createdAt"),
                Sort.Order.desc("id")
        ));
        Page<BackupJobDto> jobs = backupService.listJobs(status, pageable);
        return ResponseEntity.ok(APIResponse.success("Backup jobs retrieved successfully", PageResponse.from(jobs)));
    }
```

<a id="c08-m4"></a>

### 8.4. getSchedule

- `GET /api/admin/backups/schedule`

Nguồn: [BackupController:80](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:80).

```java
public ResponseEntity<APIResponse<BackupScheduleSettingsDto>> getSchedule()
{
        return ResponseEntity.ok(APIResponse.success(
                "Backup schedule retrieved successfully",
                backupService.getScheduleSettings()));
    }
```

<a id="c08-m5"></a>

### 8.5. updateSchedule

- `PUT /api/admin/backups/schedule`

Nguồn: [BackupController:87](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:87).

```java
public ResponseEntity<APIResponse<BackupScheduleSettingsDto>> updateSchedule(
            @Valid @RequestBody UpdateBackupScheduleRequest request,
            Principal principal
    )
{
        BackupScheduleSettingsDto settings = backupService.updateScheduleSettings(request, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Backup schedule updated successfully", settings));
    }
```

<a id="c08-m6"></a>

### 8.6. getBackup

- `GET /api/admin/backups/{jobId}`

Nguồn: [BackupController:96](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:96).

```java
public ResponseEntity<APIResponse<BackupJobDto>> getBackup(@PathVariable Long jobId)
{
        return ResponseEntity.ok(APIResponse.success(
                "Backup job retrieved successfully",
                backupService.getJob(jobId)));
    }
```

<a id="c08-m7"></a>

### 8.7. downloadBackup

- `GET /api/admin/backups/{jobId}/download`

Nguồn: [BackupController:103](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/BackupController.java:103).

```java
public ResponseEntity<Resource> downloadBackup(@PathVariable Long jobId)
{
        BackupJob job = backupService.getDownloadableJob(jobId);
        Resource resource = backupService.loadBackupFile(job);
        return ResponseEntity.ok()
                .header(HttpHeaders.CONTENT_DISPOSITION, "attachment; filename=\"" + job.getFileName() + "\"")
                .contentType(MediaType.APPLICATION_OCTET_STREAM)
                .body(resource);
    }
```

## 9. CourseMilestoneController

<a id="c09-m1"></a>

### 9.1. createCourseMilestone

- `POST /api/course-milestones`
- `POST /api/instructor/milestones`

Nguồn: [CourseMilestoneController:29](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:29).

```java
public ResponseEntity<APIResponse<CourseMilestoneDto>> createCourseMilestone(
            @Valid @RequestBody CreateCourseMilestoneRequest request,
            Principal principal
    )
{
        CourseMilestoneDto dto = courseMilestoneService.createCourseMilestone(request, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Course milestone created successfully", dto));
    }
```

<a id="c09-m2"></a>

### 9.2. getCourseMilestones

- `GET /api/course-milestones`
- `GET /api/instructor/milestones`

Nguồn: [CourseMilestoneController:41](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:41).

```java
public ResponseEntity<APIResponse<List<CourseMilestoneDto>>> getCourseMilestones(
            @RequestParam(value = "term", required = false) String term,
            @RequestParam(value = "courseCode", required = false) String courseCode,
            Principal principal
    )
{
        List<CourseMilestoneDto> list = courseMilestoneService.getCourseMilestones(term, courseCode, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Course milestones retrieved successfully", list));
    }
```

<a id="c09-m3"></a>

### 9.3. getCourseMilestoneById

- `GET /api/course-milestones/{id}`
- `GET /api/instructor/milestones/{id}`

Nguồn: [CourseMilestoneController:54](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:54).

```java
public ResponseEntity<APIResponse<CourseMilestoneDto>> getCourseMilestoneById(
            @PathVariable("id") Long id,
            Principal principal
    )
{
        CourseMilestoneDto dto = courseMilestoneService.getCourseMilestoneById(id, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Course milestone retrieved successfully", dto));
    }
```

<a id="c09-m4"></a>

### 9.4. updateCourseMilestone

- `PUT /api/course-milestones/{id}`
- `PATCH /api/course-milestones/{id}`
- `PUT /api/instructor/milestones/{id}`
- `PATCH /api/instructor/milestones/{id}`

Nguồn: [CourseMilestoneController:66](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:66).

```java
public ResponseEntity<APIResponse<CourseMilestoneDto>> updateCourseMilestone(
            @PathVariable("id") Long id,
            @Valid @RequestBody UpdateCourseMilestoneRequest request,
            Principal principal
    )
{
        CourseMilestoneDto dto = courseMilestoneService.updateCourseMilestone(id, request, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Course milestone updated successfully", dto));
    }
```

<a id="c09-m5"></a>

### 9.5. deleteCourseMilestone

- `DELETE /api/course-milestones/{id}`
- `DELETE /api/instructor/milestones/{id}`

Nguồn: [CourseMilestoneController:79](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:79).

```java
public ResponseEntity<APIResponse<Void>> deleteCourseMilestone(
            @PathVariable("id") Long id,
            Principal principal
    )
{
        courseMilestoneService.deleteCourseMilestone(id, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Course milestone deleted successfully", null));
    }
```

<a id="c09-m6"></a>

### 9.6. deprecatedOutcomes

- `GET /api/course-milestones/{milestoneId}/outcomes`
- `POST /api/course-milestones/{milestoneId}/outcomes`
- `GET /api/instructor/milestones/{milestoneId}/outcomes`
- `POST /api/instructor/milestones/{milestoneId}/outcomes`

Nguồn: [CourseMilestoneController:91](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/CourseMilestoneController.java:91).

```java
public ResponseEntity<APIResponse<Void>> deprecatedOutcomes()
{
        return ResponseEntity.status(HttpStatus.GONE)
                .body(APIResponse.error(
                        HttpStatus.GONE.value(),
                        "Outcome types were removed; use a generically named timeline milestone"));
    }
```

## 10. DashboardController

<a id="c10-m1"></a>

### 10.1. getAdminGroups

- `GET /api/dashboard/admin/groups`

Nguồn: [DashboardController:38](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:38).

```java
public ResponseEntity<APIResponse<List<DashboardGroupProgressDto>>> getAdminGroups()
{
        return ResponseEntity.ok(APIResponse.success("Admin dashboard groups retrieved successfully", dashboardService.getAdminGroups()));
    }
```

<a id="c10-m2"></a>

### 10.2. getAdminProjects

- `GET /api/dashboard/admin/projects`

Nguồn: [DashboardController:44](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:44).

```java
public ResponseEntity<APIResponse<List<DashboardProjectDto>>> getAdminProjects()
{
        return ResponseEntity.ok(APIResponse.success("Admin dashboard projects retrieved successfully", dashboardService.getAdminProjects()));
    }
```

<a id="c10-m3"></a>

### 10.3. getAdminMentors

- `GET /api/dashboard/admin/mentors`

Nguồn: [DashboardController:50](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:50).

```java
public ResponseEntity<APIResponse<List<DashboardMentorDto>>> getAdminMentors()
{
        return ResponseEntity.ok(APIResponse.success("Admin dashboard mentors retrieved successfully", dashboardService.getAdminMentors()));
    }
```

<a id="c10-m4"></a>

### 10.4. getAdminTimeline

- `GET /api/dashboard/admin/timeline`

Nguồn: [DashboardController:56](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:56).

```java
public ResponseEntity<APIResponse<List<DashboardMeetingDto>>> getAdminTimeline()
{
        return ResponseEntity.ok(APIResponse.success("Admin dashboard timeline retrieved successfully", dashboardService.getAdminTimeline()));
    }
```

<a id="c10-m5"></a>

### 10.5. getAdminExecutionStatus

- `GET /api/dashboard/admin/execution-status`

Nguồn: [DashboardController:62](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:62).

```java
public ResponseEntity<APIResponse<DashboardExecutionStatusDto>> getAdminExecutionStatus()
{
        return ResponseEntity.ok(APIResponse.success("Admin dashboard execution status retrieved successfully", dashboardService.getAdminExecutionStatus()));
    }
```

<a id="c10-m6"></a>

### 10.6. getAdminOverview

- `GET /api/dashboard/admin/overview`

Nguồn: [DashboardController:68](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:68).

```java
public ResponseEntity<APIResponse<AdminDashboardOverviewDto>> getAdminOverview(
            @RequestParam(required = false) String term,
            @RequestParam(required = false) String courseCode,
            @RequestParam(defaultValue = "5") int limit)
{

        if (limit < 1 || limit > 50) {
            throw new BadRequestException("Limit must be between 1 and 50");
        }

        AdminDashboardOverviewDto overview = dashboardService.getAdminOverview(term, courseCode, limit);
        return ResponseEntity.ok(APIResponse.success("Admin dashboard overview retrieved successfully", overview));
    }
```

<a id="c10-m7"></a>

### 10.7. getTvShowcaseProjects

- `GET /api/dashboard/tv-showcase/projects`

Nguồn: [DashboardController:83](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:83).

```java
public ResponseEntity<APIResponse<TvShowcaseDataDto<TvShowcaseDataDto.Project>>> getTvShowcaseProjects(
            @RequestParam(required = false) String term,
            @RequestParam(required = false) String courseCode,
            @RequestParam(defaultValue = "0") int page,
            @RequestParam(defaultValue = "20") int size)
{
        validatePageRequest(page, size);
        TvShowcaseDataDto<TvShowcaseDataDto.Project> showcase = dashboardService.getTvShowcaseProjects(term, courseCode, page, size);
        return ResponseEntity.ok(APIResponse.success("TV showcase projects retrieved successfully", showcase));
    }
```

<a id="c10-m8"></a>

### 10.8. getTvShowcaseRecruitments

- `GET /api/dashboard/tv-showcase/recruitments`

Nguồn: [DashboardController:95](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:95).

```java
public ResponseEntity<APIResponse<TvShowcaseDataDto<TvShowcaseDataDto.Recruitment>>> getTvShowcaseRecruitments(
            @RequestParam(required = false) String term,
            @RequestParam(required = false) String courseCode,
            @RequestParam(defaultValue = "0") int page,
            @RequestParam(defaultValue = "20") int size)
{
        validatePageRequest(page, size);
        TvShowcaseDataDto<TvShowcaseDataDto.Recruitment> showcase = dashboardService.getTvShowcaseRecruitments(term, courseCode, page, size);
        return ResponseEntity.ok(APIResponse.success("TV showcase recruitments retrieved successfully", showcase));
    }
```

<a id="c10-m9"></a>

### 10.9. getMentorGroups

- `GET /api/dashboard/mentor/groups`

Nguồn: [DashboardController:107](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:107).

```java
public ResponseEntity<APIResponse<List<DashboardGroupProgressDto>>> getMentorGroups(Principal principal)
{
        return ResponseEntity.ok(APIResponse.success("Mentor dashboard groups retrieved successfully", dashboardService.getMentorGroups(principal.getName())));
    }
```

<a id="c10-m10"></a>

### 10.10. getMentorMeetings

- `GET /api/dashboard/mentor/meetings`

Nguồn: [DashboardController:113](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:113).

```java
public ResponseEntity<APIResponse<List<DashboardMeetingDto>>> getMentorMeetings(
            @RequestParam(defaultValue = "ALL") DashboardMeetingStatusFilter status,
            Principal principal)
{
        return ResponseEntity.ok(APIResponse.success("Mentor dashboard meetings retrieved successfully", dashboardService.getMentorMeetings(principal.getName(), status)));
    }
```

<a id="c10-m11"></a>

### 10.11. getInstructorMilestones

- `GET /api/dashboard/instructor/milestones`

Nguồn: [DashboardController:121](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:121).

```java
public ResponseEntity<APIResponse<List<DashboardMilestoneStatusDto>>> getInstructorMilestones(
            @RequestParam(required = false) String term,
            @RequestParam(required = false) String courseCode,
            @RequestParam(required = false) Long groupId,
            Principal principal)
{
        return ResponseEntity.ok(APIResponse.success(
                "Instructor dashboard milestones retrieved successfully",
                dashboardService.getInstructorMilestones(term, courseCode, groupId, principal.getName())));
    }
```

<a id="c10-m12"></a>

### 10.12. getStudentGroups

- `GET /api/dashboard/student/groups`

Nguồn: [DashboardController:133](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:133).

```java
public ResponseEntity<APIResponse<List<DashboardGroupProgressDto>>> getStudentGroups(Principal principal)
{
        return ResponseEntity.ok(APIResponse.success("Student dashboard groups retrieved successfully", dashboardService.getStudentGroups(principal.getName())));
    }
```

<a id="c10-m13"></a>

### 10.13. getStudentProgress

- `GET /api/dashboard/student/progress`

Nguồn: [DashboardController:139](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:139).

```java
public ResponseEntity<APIResponse<DashboardStudentProgressDto>> getStudentProgress(Principal principal)
{
        return ResponseEntity.ok(APIResponse.success("Student dashboard progress retrieved successfully", dashboardService.getStudentProgress(principal.getName())));
    }
```

<a id="c10-m14"></a>

### 10.14. getStudentProjects

- `GET /api/dashboard/student/projects`

Nguồn: [DashboardController:145](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:145).

```java
public ResponseEntity<APIResponse<List<DashboardProjectDto>>> getStudentProjects(Principal principal)
{
        return ResponseEntity.ok(APIResponse.success("Student dashboard projects retrieved successfully", dashboardService.getStudentProjects(principal.getName())));
    }
```

<a id="c10-m15"></a>

### 10.15. getStudentMilestones

- `GET /api/dashboard/student/milestones`

Nguồn: [DashboardController:151](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/DashboardController.java:151).

```java
public ResponseEntity<APIResponse<List<DashboardMilestoneStatusDto>>> getStudentMilestones(
            @RequestParam Long groupId,
            Principal principal)
{
        return ResponseEntity.ok(APIResponse.success(
                "Student dashboard milestones retrieved successfully",
                dashboardService.getStudentMilestones(groupId, principal.getName())));
    }
```

## 11. FeedbackController

<a id="c11-m1"></a>

### 11.1. getOwnFeedbacks

- `GET /api/feedback/me`

Nguồn: [FeedbackController:28](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/FeedbackController.java:28).

Annotation quyền: `@PreAuthorize("hasRole('STUDENT')")`.

```java
public ResponseEntity<APIResponse<List<TermFeedbackResponseDto>>> getOwnFeedbacks(
            @Parameter(description = "Academic term code (e.g. FALL2026)")
            @RequestParam(value = "term", required = false) String term,
            @Parameter(description = "Feedback status (PENDING or SUBMITTED)")
            @RequestParam(value = "status", required = false) FeedbackStatus status,
            Principal principal
    )
{
        List<TermFeedbackResponseDto> response = feedbackService.getOwnFeedbacks(term, status, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Feedbacks retrieved successfully", response));
    }
```

<a id="c11-m2"></a>

### 11.2. submitOrUpdateFeedback

- `PUT /api/feedback/{id}`

Nguồn: [FeedbackController:42](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/FeedbackController.java:42).

Annotation quyền: `@PreAuthorize("hasRole('STUDENT')")`.

```java
public ResponseEntity<APIResponse<TermFeedbackResponseDto>> submitOrUpdateFeedback(
            @PathVariable("id") Long id,
            @Valid @RequestBody SubmitFeedbackRequest request,
            Principal principal
    )
{
        TermFeedbackResponseDto response = feedbackService.submitOrUpdateFeedback(id, request, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Feedback submitted successfully", response));
    }
```

<a id="c11-m3"></a>

### 11.3. getReceivedFeedbacks

- `GET /api/feedback/received`

Nguồn: [FeedbackController:54](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/FeedbackController.java:54).

Annotation quyền: `@PreAuthorize("hasAnyRole('MENTOR', 'INSTRUCTOR')")`.

```java
public ResponseEntity<APIResponse<FeedbackReceivedSummaryDto>> getReceivedFeedbacks(
            @RequestParam(value = "term", required = false) String term,
            @RequestParam(value = "courseCode", required = false) String courseCode,
            Principal principal
    )
{
        FeedbackReceivedSummaryDto response = feedbackService.getReceivedFeedbacks(principal.getName(), term, courseCode);
        return ResponseEntity.ok(APIResponse.success("Feedbacks retrieved successfully", response));
    }
```

## 12. GroupController

<a id="c12-m1"></a>

### 12.1. getGroups

- `GET /api/groups`

Nguồn: [GroupController:34](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:34).

```java
public ResponseEntity<APIResponse<List<GroupSummaryDto>>> getGroups(
            @RequestParam(value = "search", required = false) String search,
            @RequestParam(value = "status", required = false) String status,
            @RequestParam(value = "neededRole", required = false) String neededRole,
            @RequestParam(value = "roleCategory", required = false) String roleCategory
    )
{
        List<GroupSummaryDto> groups = groupService.getGroups(search, status, neededRole, roleCategory);
        return ResponseEntity.ok(APIResponse.success("Groups retrieved successfully", groups));
    }
```

<a id="c12-m2"></a>

### 12.2. discoverGroups

- `GET /api/groups/discover`

Nguồn: [GroupController:46](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:46).

```java
public ResponseEntity<APIResponse<PageResponse<GroupSummaryDto>>> discoverGroups(
            @RequestParam(value = "page", defaultValue = "0") int page,
            @RequestParam(value = "size", defaultValue = "12") int size,
            @RequestParam(value = "name", required = false) String name,
            @RequestParam(value = "studentGpa", required = false) BigDecimal studentGpa,
            @RequestParam(value = "neededRole", required = false) String neededRole,
            Principal principal
    )
{
        if (page < 0) {
            throw new BadRequestException("Page index must be zero or greater");
        }
        if (size < 1 || size > 100) {
            throw new BadRequestException("Page size must be between 1 and 100");
        }
        if (studentGpa != null && (studentGpa.compareTo(BigDecimal.ZERO) < 0
                || studentGpa.compareTo(new BigDecimal("4.00")) > 0)) {
            throw new BadRequestException("Student GPA must be between 0.00 and 4.00");
        }

        Pageable pageable = PageRequest.of(page, size, Sort.by(
                Sort.Order.desc("createdAt"),
                Sort.Order.desc("id")
        ));
        Page<GroupSummaryDto> groups = groupService.getDiscoverGroups(
                principal.getName(), name, studentGpa, neededRole, pageable);
        return ResponseEntity.ok(APIResponse.success(
                "Discover groups retrieved successfully",
                PageResponse.from(groups)));
    }
```

<a id="c12-m3"></a>

### 12.3. getGroupDetail

- `GET /api/groups/{id}`

Nguồn: [GroupController:78](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:78).

```java
public ResponseEntity<APIResponse<GroupDetailDto>> getGroupDetail(
            @PathVariable("id") Long id
    )
{
        GroupDetailDto groupDetail = groupService.getGroupDetail(id);
        return ResponseEntity.ok(APIResponse.success("Group details retrieved successfully", groupDetail));
    }
```

<a id="c12-m4"></a>

### 12.4. updateGroupCriteria

- `PATCH /api/groups/{id}/criteria`

Nguồn: [GroupController:87](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:87).

```java
public ResponseEntity<APIResponse<GroupDetailDto>> updateGroupCriteria(
            @PathVariable("id") Long id,
            @Valid @RequestBody UpdateGroupCriteriaRequest request,
            Principal principal
    )
{
        String email = principal.getName();
        GroupDetailDto updatedGroup = groupService.updateGroupCriteria(id, request, email);
        return ResponseEntity.ok(APIResponse.success("Group criteria updated successfully", updatedGroup));
    }
```

<a id="c12-m5"></a>

### 12.5. getMyAssignedGroups

- `GET /api/groups/mentor/me`

Nguồn: [GroupController:99](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:99).

```java
public ResponseEntity<APIResponse<List<GroupSummaryDto>>> getMyAssignedGroups(Principal principal)
{
        String email = principal.getName();
        List<GroupSummaryDto> groups = groupService.getGroupsByMentorEmail(email);
        return ResponseEntity.ok(APIResponse.success("Assigned groups retrieved successfully", groups));
    }
```

<a id="c12-m6"></a>

### 12.6. getMyStudentGroup

- `GET /api/groups/student/me`

Nguồn: [GroupController:107](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:107).

```java
public ResponseEntity<APIResponse<List<GroupSummaryDto>>> getMyStudentGroup(Principal principal)
{
        String email = principal.getName();
        List<GroupSummaryDto> groups = groupService.getGroupsByStudentEmail(email);
        return ResponseEntity.ok(APIResponse.success("Student group retrieved successfully", groups));
    }
```

<a id="c12-m7"></a>

### 12.7. createGroup

- `POST /api/groups`

Nguồn: [GroupController:115](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:115).

```java
public ResponseEntity<APIResponse<GroupDetailDto>> createGroup(
            @Valid @RequestBody CreateGroupRequest request,
            Principal principal
    )
{
        String email = principal.getName();
        GroupDetailDto createdGroup = groupService.createGroup(request, email);
        return ResponseEntity.ok(APIResponse.success("Group created successfully", createdGroup));
    }
```

<a id="c12-m8"></a>

### 12.8. updateGroup

- `PATCH /api/groups/{id}`

Nguồn: [GroupController:126](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:126).

```java
public ResponseEntity<APIResponse<GroupDetailDto>> updateGroup(
            @PathVariable("id") Long id,
            @Valid @RequestBody UpdateGroupRequest request,
            Principal principal
    )
{
        String email = principal.getName();
        GroupDetailDto updatedGroup = groupService.updateGroup(id, request, email);
        return ResponseEntity.ok(APIResponse.success("Group updated successfully", updatedGroup));
    }
```

<a id="c12-m9"></a>

### 12.9. removeMember

- `DELETE /api/groups/{groupId}/members/{studentId}`

Nguồn: [GroupController:138](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:138).

```java
public ResponseEntity<APIResponse<Void>> removeMember(
            @PathVariable("groupId") Long groupId,
            @PathVariable("studentId") Long studentId,
            Principal principal
    )
{
        groupService.removeMember(groupId, studentId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Member removed successfully", null));
    }
```

<a id="c12-m10"></a>

### 12.10. leaveGroup

- `DELETE /api/groups/{groupId}/members/me`

Nguồn: [GroupController:149](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:149).

```java
public ResponseEntity<APIResponse<Void>> leaveGroup(
            @PathVariable("groupId") Long groupId,
            Principal principal
    )
{
        groupService.leaveGroup(groupId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Left group successfully", null));
    }
```

<a id="c12-m11"></a>

### 12.11. leaveGroupPost

- `POST /api/groups/{groupId}/leave`

Nguồn: [GroupController:159](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:159).

```java
public ResponseEntity<APIResponse<Void>> leaveGroupPost(
            @PathVariable("groupId") Long groupId,
            Principal principal
    )
{
        groupService.leaveGroup(groupId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Left group successfully", null));
    }
```

<a id="c12-m12"></a>

### 12.12. transferLeader

- `PATCH /api/groups/{groupId}/leader`

Nguồn: [GroupController:169](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:169).

```java
public ResponseEntity<APIResponse<Void>> transferLeader(
            @PathVariable("groupId") Long groupId,
            @Valid @RequestBody TransferLeaderRequest request,
            Principal principal
    )
{
        groupService.transferLeader(groupId, request, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Leadership transferred successfully", null));
    }
```

<a id="c12-m13"></a>

### 12.13. assignInstructor

- `PATCH /api/groups/{groupId}/instructor`

Nguồn: [GroupController:180](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:180).

```java
public ResponseEntity<APIResponse<GroupDetailDto>> assignInstructor(
            @PathVariable("groupId") Long groupId,
            @Valid @RequestBody AssignInstructorRequest request
    )
{
        GroupDetailDto updatedGroup = groupService.assignInstructorToGroup(groupId, request.instructorId());
        return ResponseEntity.ok(APIResponse.success("Instructor assigned to group successfully", updatedGroup));
    }
```

<a id="c12-m14"></a>

### 12.14. unassignInstructor

- `DELETE /api/groups/{groupId}/instructor`

Nguồn: [GroupController:193](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:193).

```java
public ResponseEntity<APIResponse<GroupDetailDto>> unassignInstructor(
            @PathVariable("groupId") Long groupId
    )
{
        GroupDetailDto updatedGroup = groupService.unassignInstructorFromGroup(groupId);
        return ResponseEntity.ok(APIResponse.success("Instructor unassigned from group successfully", updatedGroup));
    }
```

<a id="c12-m15"></a>

### 12.15. assignMentor

- `PATCH /api/groups/{groupId}/mentor`

Nguồn: [GroupController:205](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:205).

```java
public ResponseEntity<APIResponse<GroupDetailDto>> assignMentor(
            @PathVariable("groupId") Long groupId,
            @Valid @RequestBody AssignMentorRequest request
    )
{
        GroupDetailDto updatedGroup = groupService.assignMentorToGroup(groupId, request.mentorId());
        return ResponseEntity.ok(APIResponse.success("Mentor assigned to group successfully", updatedGroup));
    }
```

<a id="c12-m16"></a>

### 12.16. unassignMentor

- `DELETE /api/groups/{groupId}/mentor`

Nguồn: [GroupController:218](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:218).

```java
public ResponseEntity<APIResponse<GroupDetailDto>> unassignMentor(
            @PathVariable("groupId") Long groupId
    )
{
        GroupDetailDto updatedGroup = groupService.unassignMentorFromGroup(groupId);
        return ResponseEntity.ok(APIResponse.success("Mentor unassigned from group successfully", updatedGroup));
    }
```

<a id="c12-m17"></a>

### 12.17. getMyAssignedGroups

- `GET /api/groups/instructor/me`

Nguồn: [GroupController:230](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:230).

```java
public ResponseEntity<APIResponse<List<GroupSummaryDto>>> getMyAssignedGroups(
            @RequestParam(value = "term", required = false) String term,
            @RequestParam(value = "courseCode", required = false) String courseCode,
            Principal principal
    )
{
        String email = principal.getName();
        List<GroupSummaryDto> groups = groupService.getGroupsByInstructorEmail(email, term, courseCode);
        return ResponseEntity.ok(APIResponse.success("Assigned groups retrieved successfully", groups));
    }
```

<a id="c12-m18"></a>

### 12.18. getGroupMilestones

- `GET /api/groups/{groupId}/milestones`

Nguồn: [GroupController:245](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:245).

```java
public ResponseEntity<APIResponse<List<CourseMilestoneDto>>> getGroupMilestones(
            @PathVariable("groupId") Long groupId,
            Principal principal
    )
{
        List<CourseMilestoneDto> milestones =
                courseMilestoneService.getGroupMilestones(groupId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Group timeline retrieved successfully", milestones));
    }
```

<a id="c12-m19"></a>

### 12.19. updateGroupLock

- `PATCH /api/groups/{groupId}/lock`

Nguồn: [GroupController:259](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupController.java:259).

```java
public ResponseEntity<APIResponse<GroupDetailDto>> updateGroupLock(
            @PathVariable("groupId") Long groupId,
            @Valid @RequestBody UpdateGroupLockRequest request,
            Principal principal
    )
{
        GroupDetailDto group =
                groupService.updateGroupLock(groupId, request, principal.getName());
        return ResponseEntity.ok(APIResponse.success(
                request.isLock() ? "Group locked successfully" : "Group unlocked successfully",
                group));
    }
```

## 13. GroupInvitationController

<a id="c13-m1"></a>

### 13.1. createInvitation

- `POST /api/groups/{groupId}/invitations`

Nguồn: [GroupInvitationController:27](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:27).

```java
public ResponseEntity<APIResponse<InvitationDto>> createInvitation(
            @PathVariable("groupId") Long groupId,
            @Valid @RequestBody CreateInvitationRequest request,
            Principal principal
    )
{
        InvitationDto invitation = groupInvitationService.createInvitation(groupId, request, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Invitation created successfully", invitation));
    }
```

<a id="c13-m2"></a>

### 13.2. getGroupInvitations

- `GET /api/groups/{groupId}/invitations`

Nguồn: [GroupInvitationController:38](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:38).

```java
public ResponseEntity<APIResponse<List<InvitationDto>>> getGroupInvitations(
            @PathVariable("groupId") Long groupId,
            Principal principal
    )
{
        List<InvitationDto> invitations = groupInvitationService.getGroupInvitations(groupId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Invitations retrieved successfully", invitations));
    }
```

<a id="c13-m3"></a>

### 13.3. getMyPendingInvitations

- `GET /api/groups/invitations/me`

Nguồn: [GroupInvitationController:48](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:48).

```java
public ResponseEntity<APIResponse<List<InvitationDto>>> getMyPendingInvitations(Principal principal)
{
        List<InvitationDto> invitations = groupInvitationService.getPendingInvitationsForMe(principal.getName());
        return ResponseEntity.ok(APIResponse.success("Pending invitations retrieved successfully", invitations));
    }
```

<a id="c13-m4"></a>

### 13.4. acceptInvitation

- `POST /api/groups/invitations/{invitationId}/accept`

Nguồn: [GroupInvitationController:55](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:55).

```java
public ResponseEntity<APIResponse<Void>> acceptInvitation(
            @PathVariable("invitationId") Long invitationId,
            Principal principal
    )
{
        groupInvitationService.acceptInvitation(invitationId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Invitation accepted successfully", null));
    }
```

<a id="c13-m5"></a>

### 13.5. declineInvitation

- `POST /api/groups/invitations/{invitationId}/decline`

Nguồn: [GroupInvitationController:65](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:65).

```java
public ResponseEntity<APIResponse<Void>> declineInvitation(
            @PathVariable("invitationId") Long invitationId,
            Principal principal
    )
{
        groupInvitationService.declineInvitation(invitationId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Invitation declined successfully", null));
    }
```

<a id="c13-m6"></a>

### 13.6. cancelInvitation

- `POST /api/groups/invitations/{invitationId}/cancel`

Nguồn: [GroupInvitationController:75](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupInvitationController.java:75).

```java
public ResponseEntity<APIResponse<Void>> cancelInvitation(
            @PathVariable("invitationId") Long invitationId,
            Principal principal
    )
{
        groupInvitationService.cancelInvitation(invitationId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Invitation cancelled successfully", null));
    }
```

## 14. GroupJoinRequestController

<a id="c14-m1"></a>

### 14.1. getMyJoinRequests

- `GET /api/groups/join-requests/me`

Nguồn: [GroupJoinRequestController:27](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:27).

```java
public ResponseEntity<APIResponse<List<GroupJoinRequestDto>>> getMyJoinRequests(Principal principal)
{
        List<GroupJoinRequestDto> list = groupJoinRequestService.getMyJoinRequests(principal.getName());
        return ResponseEntity.ok(APIResponse.success("My join requests retrieved successfully", list));
    }
```

<a id="c14-m2"></a>

### 14.2. createJoinRequest

- `POST /api/groups/{groupId}/join-requests`

Nguồn: [GroupJoinRequestController:34](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:34).

```java
public ResponseEntity<APIResponse<GroupJoinRequestDto>> createJoinRequest(
            @PathVariable("groupId") Long groupId,
            @Valid @RequestBody CreateJoinRequestDto request,
            Principal principal
    )
{
        GroupJoinRequestDto dto = groupJoinRequestService.createJoinRequest(groupId, request, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Join request submitted successfully", dto));
    }
```

<a id="c14-m3"></a>

### 14.3. getJoinRequests

- `GET /api/groups/{groupId}/join-requests`

Nguồn: [GroupJoinRequestController:45](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:45).

```java
public ResponseEntity<APIResponse<List<GroupJoinRequestDto>>> getJoinRequests(
            @PathVariable("groupId") Long groupId,
            Principal principal
    )
{
        List<GroupJoinRequestDto> list = groupJoinRequestService.getJoinRequests(groupId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Join requests retrieved successfully", list));
    }
```

<a id="c14-m4"></a>

### 14.4. approveJoinRequest

- `POST /api/groups/{groupId}/join-requests/{requestId}/approve`
- `POST /api/groups/join-requests/{requestId}/approve`

Nguồn: [GroupJoinRequestController:55](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:55).

```java
public ResponseEntity<APIResponse<Void>> approveJoinRequest(
            @PathVariable(value = "groupId", required = false) Long groupId,
            @PathVariable("requestId") Long requestId,
            Principal principal
    )
{
        groupJoinRequestService.approveJoinRequest(groupId, requestId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Join request approved successfully", null));
    }
```

<a id="c14-m5"></a>

### 14.5. rejectJoinRequest

- `POST /api/groups/{groupId}/join-requests/{requestId}/reject`
- `POST /api/groups/join-requests/{requestId}/reject`

Nguồn: [GroupJoinRequestController:69](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:69).

```java
public ResponseEntity<APIResponse<Void>> rejectJoinRequest(
            @PathVariable(value = "groupId", required = false) Long groupId,
            @PathVariable("requestId") Long requestId,
            Principal principal
    )
{
        groupJoinRequestService.rejectJoinRequest(groupId, requestId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Join request rejected successfully", null));
    }
```

<a id="c14-m6"></a>

### 14.6. cancelJoinRequest

- `POST /api/groups/{groupId}/join-requests/{requestId}/cancel`
- `POST /api/groups/join-requests/{requestId}/cancel`

Nguồn: [GroupJoinRequestController:83](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupJoinRequestController.java:83).

```java
public ResponseEntity<APIResponse<Void>> cancelJoinRequest(
            @PathVariable(value = "groupId", required = false) Long groupId,
            @PathVariable("requestId") Long requestId,
            Principal principal
    )
{
        groupJoinRequestService.cancelJoinRequest(groupId, requestId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Join request cancelled successfully", null));
    }
```

## 15. GroupMeetingController

<a id="c15-m1"></a>

### 15.1. getAvailableMentorSlots

- `GET /api/groups/{groupId}/mentor/availability`

Nguồn: [GroupMeetingController:34](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:34).

```java
public ResponseEntity<APIResponse<List<MentorAvailabilitySlotDto>>> getAvailableMentorSlots(
            @PathVariable("groupId") Long groupId,
            Principal principal
    )
{
        List<MentorAvailabilitySlotDto> slots = meetingService.getAvailableMentorSlots(groupId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Available slots retrieved successfully", slots));
    }
```

<a id="c15-m2"></a>

### 15.2. createMeeting

- `POST /api/groups/{groupId}/mentor/meetings`

Nguồn: [GroupMeetingController:45](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:45).

```java
public ResponseEntity<APIResponse<MentorMeetingDto>> createMeeting(
            @PathVariable("groupId") Long groupId,
            @Valid @RequestBody CreateOrBookMentorMeetingRequest request,
            Principal principal
    )
{
        if (request.slotId() != null) {
            MentorMeetingDto meeting = meetingService.bookMeeting(
                    groupId, new BookMeetingRequest(request.slotId()), principal.getName());
            return ResponseEntity.ok(APIResponse.success("Meeting booked successfully", meeting));
        }
        MentorMeetingDto meeting = meetingService.createMeeting(
                groupId,
                new CreateMentorMeetingRequest(
                        request.startAt(), request.endAt(), request.meetLink(), request.note()),
                principal.getName());
        return ResponseEntity.ok(APIResponse.success("Meeting created successfully", meeting));
    }
```

<a id="c15-m3"></a>

### 15.3. updateMeeting

- `PATCH /api/groups/{groupId}/mentor/meetings/{meetingId}`

Nguồn: [GroupMeetingController:66](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:66).

```java
public ResponseEntity<APIResponse<MentorMeetingDto>> updateMeeting(
            @PathVariable Long groupId, @PathVariable Long meetingId,
            @Valid @RequestBody UpdateMentorMeetingRequest request, Principal principal)
{
        return ResponseEntity.ok(APIResponse.success("Meeting updated successfully",
                meetingService.updateMeeting(groupId, meetingId, request, principal.getName())));
    }
```

<a id="c15-m4"></a>

### 15.4. submitEvidence

- `PUT /api/groups/{groupId}/mentor/meetings/{meetingId}/evidence`

Nguồn: [GroupMeetingController:74](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:74).

```java
public ResponseEntity<APIResponse<MentorMeetingDto>> submitEvidence(
            @PathVariable Long groupId, @PathVariable Long meetingId,
            @Valid @RequestBody SubmitMeetingEvidenceRequest request, Principal principal)
{
        return ResponseEntity.ok(APIResponse.success("Meeting evidence submitted successfully",
                meetingService.submitEvidence(groupId, meetingId, request, principal.getName())));
    }
```

<a id="c15-m5"></a>

### 15.5. cancelMeeting

- `PATCH /api/groups/{groupId}/mentor/meetings/{meetingId}/cancel`

Nguồn: [GroupMeetingController:82](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:82).

```java
public ResponseEntity<APIResponse<MentorMeetingDto>> cancelMeeting(
            @PathVariable("groupId") Long groupId,
            @PathVariable("meetingId") Long meetingId,
            @Valid @RequestBody CancelMeetingRequest request,
            Principal principal
    )
{
        MentorMeetingDto meeting = meetingService.cancelMeeting(groupId, meetingId, request, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Meeting canceled successfully", meeting));
    }
```

<a id="c15-m6"></a>

### 15.6. confirmMeeting

- `PATCH /api/groups/{groupId}/mentor/meetings/{meetingId}/confirm`

Nguồn: [GroupMeetingController:95](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:95).

```java
public ResponseEntity<APIResponse<MentorMeetingDto>> confirmMeeting(
            @PathVariable("groupId") Long groupId,
            @PathVariable("meetingId") Long meetingId,
            Principal principal
    )
{
        MentorMeetingDto meeting = meetingService.confirmMeeting(groupId, meetingId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Meeting confirmation saved successfully", meeting));
    }
```

<a id="c15-m7"></a>

### 15.7. getGroupMeetings

- `GET /api/groups/{groupId}/mentor/meetings`

Nguồn: [GroupMeetingController:107](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:107).

```java
public ResponseEntity<APIResponse<List<MentorMeetingDto>>> getGroupMeetings(
            @PathVariable("groupId") Long groupId,
            Principal principal
    )
{
        List<MentorMeetingDto> meetings = meetingService.getGroupMeetings(groupId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Group meetings retrieved successfully", meetings));
    }
```

<a id="c15-m8"></a>

### 15.8. getGroupMeetingDetail

- `GET /api/groups/{groupId}/mentor/meetings/{meetingId}`

Nguồn: [GroupMeetingController:118](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupMeetingController.java:118).

```java
public ResponseEntity<APIResponse<MentorMeetingDto>> getGroupMeetingDetail(
            @PathVariable("groupId") Long groupId,
            @PathVariable("meetingId") Long meetingId,
            Principal principal
    )
{
        MentorMeetingDto meeting = meetingService.getGroupMeetingDetail(groupId, meetingId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Group meeting detail retrieved successfully", meeting));
    }
```

## 16. GroupProblemController

<a id="c16-m1"></a>

### 16.1. selectProblem

- `POST /api/groups/{groupId}/problems/select`

Nguồn: [GroupProblemController:27](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:27).

```java
public ResponseEntity<APIResponse<GroupDetailDto>> selectProblem(
            @PathVariable("groupId") Long groupId,
            @Valid @RequestBody SelectProblemRequest request,
            Principal principal
    )
{
        if (principal == null) {
            throw new UnauthorizedException("Not authenticated");
        }
        GroupDetailDto groupDetail = groupProblemService.selectProblem(groupId, request.problemId(), principal.getName());
        return ResponseEntity.ok(APIResponse.success("Problem selected successfully", groupDetail));
    }
```

<a id="c16-m2"></a>

### 16.2. clearProblem

- `DELETE /api/groups/{groupId}/problems/select`

Nguồn: [GroupProblemController:42](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:42).

```java
public ResponseEntity<APIResponse<Void>> clearProblem(
            @PathVariable("groupId") Long groupId,
            Principal principal
    )
{
        if (principal == null) {
            throw new UnauthorizedException("Not authenticated");
        }
        groupProblemService.clearProblem(groupId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Selected problem cleared successfully", null));
    }
```

<a id="c16-m3"></a>

### 16.3. proposeProblem

- `POST /api/groups/{groupId}/problems/propose`

Nguồn: [GroupProblemController:56](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:56).

```java
public ResponseEntity<APIResponse<ProblemDetailDto>> proposeProblem(
            @PathVariable("groupId") Long groupId,
            @Valid @RequestBody ProposeGroupProblemRequest request,
            Principal principal
    )
{
        if (principal == null) {
            throw new UnauthorizedException("Not authenticated");
        }
        ProblemDetailDto problemDetail = groupProblemService.proposeProblem(groupId, request, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Problem proposed successfully", problemDetail));
    }
```

<a id="c16-m4"></a>

### 16.4. updatePendingProposal

- `PUT /api/groups/{groupId}/problems/proposals/{problemId}`

Nguồn: [GroupProblemController:71](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:71).

```java
public ResponseEntity<APIResponse<ProblemDetailDto>> updatePendingProposal(
            @PathVariable("groupId") Long groupId,
            @PathVariable("problemId") Long problemId,
            @Valid @RequestBody ProposeGroupProblemRequest request,
            Principal principal
    )
{
        if (principal == null) {
            throw new UnauthorizedException("Not authenticated");
        }
        ProblemDetailDto problemDetail = groupProblemService.updatePendingProposal(groupId, problemId, request, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Problem proposal updated successfully", problemDetail));
    }
```

<a id="c16-m5"></a>

### 16.5. deletePendingProposal

- `DELETE /api/groups/{groupId}/problems/proposals/{problemId}`

Nguồn: [GroupProblemController:87](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:87).

```java
public ResponseEntity<APIResponse<Void>> deletePendingProposal(
            @PathVariable("groupId") Long groupId,
            @PathVariable("problemId") Long problemId,
            Principal principal
    )
{
        if (principal == null) {
            throw new UnauthorizedException("Not authenticated");
        }
        groupProblemService.deletePendingProposal(groupId, problemId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Problem proposal deleted successfully", null));
    }
```

<a id="c16-m6"></a>

### 16.6. getGroupProposals

- `GET /api/groups/{groupId}/problems/proposals`

Nguồn: [GroupProblemController:102](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupProblemController.java:102).

```java
public ResponseEntity<APIResponse<List<ProblemSummaryDto>>> getGroupProposals(
            @PathVariable("groupId") Long groupId,
            Principal principal
    )
{
        if (principal == null) {
            throw new UnauthorizedException("Not authenticated");
        }
        List<ProblemSummaryDto> proposals = groupProblemService.getGroupProposals(groupId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Group proposals retrieved successfully", proposals));
    }
```

## 17. GroupRecruitmentRoleController

<a id="c17-m1"></a>

### 17.1. getRecruitmentRoles

- `GET /api/group-recruitment-roles`

Nguồn: [GroupRecruitmentRoleController:22](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupRecruitmentRoleController.java:22).

```java
public ResponseEntity<APIResponse<List<RecruitmentRoleDto>>> getRecruitmentRoles()
{
        List<RecruitmentRoleDto> roles = Arrays.stream(RecruitmentRole.values())
                .map(role -> new RecruitmentRoleDto(
                        role.getCode(),
                        role.getCategory().name(),
                        role.getDisplayNameVi(),
                        role.getDisplayNameEn()
                ))
                .collect(Collectors.toList());
        return ResponseEntity.ok(APIResponse.success("Recruitment roles retrieved successfully", roles));
    }
```

## 18. GroupTaskController

<a id="c18-m1"></a>

### 18.1. getBoard

- `GET /api/groups/{groupId}/board`
- `GET /api/groups/{groupId}/boards/{boardId}`

Nguồn: [GroupTaskController:34](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:34).

```java
public ResponseEntity<APIResponse<GroupTaskBoardDto>> getBoard(
            @PathVariable("groupId") Long groupId,
            @PathVariable(value = "boardId", required = false) Long pathBoardId,
            @RequestParam(value = "boardId", required = false) Long queryBoardId,
            @RequestParam(value = "priority", required = false) String priority,
            @RequestParam(value = "assigneeStudentId", required = false) Long assigneeStudentId,
            @RequestParam(value = "search", required = false) String search,
            @RequestParam(value = "includeArchived", defaultValue = "false") Boolean includeArchived,
            Principal principal
    )
{
        Long boardId = pathBoardId != null ? pathBoardId : queryBoardId;
        String email = getEmail(principal);
        GroupTaskBoardDto board = groupTaskService.getBoard(groupId, boardId, priority, assigneeStudentId, search, includeArchived, email);
        return ResponseEntity.ok(APIResponse.success("Group task board retrieved successfully", board));
    }
```

<a id="c18-m2"></a>

### 18.2. createTask

- `POST /api/groups/{groupId}/tasks`

Nguồn: [GroupTaskController:53](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:53).

```java
public ResponseEntity<APIResponse<TaskDetailDto>> createTask(
            @PathVariable("groupId") Long groupId,
            @Valid @RequestBody CreateTaskRequest request,
            Principal principal
    )
{
        String email = getEmail(principal);
        TaskDetailDto task = groupTaskService.createTask(groupId, request, email);
        return ResponseEntity.status(HttpStatus.CREATED).body(APIResponse.success("Task created successfully", task));
    }
```

<a id="c18-m3"></a>

### 18.3. getTaskDetail

- `GET /api/groups/{groupId}/tasks/{taskId}`

Nguồn: [GroupTaskController:65](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:65).

```java
public ResponseEntity<APIResponse<TaskDetailDto>> getTaskDetail(
            @PathVariable("groupId") Long groupId,
            @PathVariable("taskId") Long taskId,
            Principal principal
    )
{
        String email = getEmail(principal);
        TaskDetailDto task = groupTaskService.getTaskDetail(groupId, taskId, email);
        return ResponseEntity.ok(APIResponse.success("Task details retrieved successfully", task));
    }
```

<a id="c18-m4"></a>

### 18.4. updateTask

- `PATCH /api/groups/{groupId}/tasks/{taskId}`

Nguồn: [GroupTaskController:77](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:77).

```java
public ResponseEntity<APIResponse<TaskDetailDto>> updateTask(
            @PathVariable("groupId") Long groupId,
            @PathVariable("taskId") Long taskId,
            @Valid @RequestBody UpdateTaskRequest request,
            Principal principal
    )
{
        String email = getEmail(principal);
        TaskDetailDto task = groupTaskService.updateTaskInfo(groupId, taskId, request, email);
        return ResponseEntity.ok(APIResponse.success("Task updated successfully", task));
    }
```

<a id="c18-m5"></a>

### 18.5. replaceAssignees

- `PUT /api/groups/{groupId}/tasks/{taskId}/assignees`

Nguồn: [GroupTaskController:90](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:90).

```java
public ResponseEntity<APIResponse<TaskDetailDto>> replaceAssignees(
            @PathVariable("groupId") Long groupId,
            @PathVariable("taskId") Long taskId,
            @Valid @RequestBody ReplaceTaskAssigneesRequest request,
            Principal principal
    )
{
        String email = getEmail(principal);
        TaskDetailDto task = groupTaskService.replaceTaskAssignees(groupId, taskId, request, email);
        return ResponseEntity.ok(APIResponse.success("Task assignees replaced successfully", task));
    }
```

<a id="c18-m6"></a>

### 18.6. moveTask

- `PATCH /api/groups/{groupId}/tasks/{taskId}/move`

Nguồn: [GroupTaskController:103](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:103).

```java
public ResponseEntity<APIResponse<TaskDetailDto>> moveTask(
            @PathVariable("groupId") Long groupId,
            @PathVariable("taskId") Long taskId,
            @Valid @RequestBody MoveTaskRequest request,
            Principal principal
    )
{
        String email = getEmail(principal);
        TaskDetailDto task = groupTaskService.moveTask(groupId, taskId, request, email);
        return ResponseEntity.ok(APIResponse.success("Task moved successfully", task));
    }
```

<a id="c18-m7"></a>

### 18.7. archiveTask

- `DELETE /api/groups/{groupId}/tasks/{taskId}`

Nguồn: [GroupTaskController:116](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:116).

```java
public ResponseEntity<APIResponse<TaskDetailDto>> archiveTask(
            @PathVariable("groupId") Long groupId,
            @PathVariable("taskId") Long taskId,
            Principal principal
    )
{
        String email = getEmail(principal);
        TaskDetailDto task = groupTaskService.archiveTask(groupId, taskId, email);
        return ResponseEntity.ok(APIResponse.success("Task archived successfully", task));
    }
```

<a id="c18-m8"></a>

### 18.8. restoreTask

- `POST /api/groups/{groupId}/tasks/{taskId}/restore`

Nguồn: [GroupTaskController:128](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:128).

```java
public ResponseEntity<APIResponse<TaskDetailDto>> restoreTask(
            @PathVariable("groupId") Long groupId,
            @PathVariable("taskId") Long taskId,
            Principal principal
    )
{
        String email = getEmail(principal);
        TaskDetailDto task = groupTaskService.restoreTask(groupId, taskId, email);
        return ResponseEntity.ok(APIResponse.success("Task restored successfully", task));
    }
```

<a id="c18-m9"></a>

### 18.9. addChecklistItem

- `POST /api/groups/{groupId}/tasks/{taskId}/checklist-items`

Nguồn: [GroupTaskController:140](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:140).

```java
public ResponseEntity<APIResponse<ChecklistItemDto>> addChecklistItem(
            @PathVariable("groupId") Long groupId,
            @PathVariable("taskId") Long taskId,
            @Valid @RequestBody CreateChecklistItemRequest request,
            Principal principal
    )
{
        String email = getEmail(principal);
        ChecklistItemDto item = groupTaskService.addChecklistItem(groupId, taskId, request, email);
        return ResponseEntity.status(HttpStatus.CREATED).body(APIResponse.success("Checklist item added successfully", item));
    }
```

<a id="c18-m10"></a>

### 18.10. updateChecklistItem

- `PATCH /api/groups/{groupId}/tasks/{taskId}/checklist-items/{itemId}`

Nguồn: [GroupTaskController:153](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:153).

```java
public ResponseEntity<APIResponse<ChecklistItemDto>> updateChecklistItem(
            @PathVariable("groupId") Long groupId,
            @PathVariable("taskId") Long taskId,
            @PathVariable("itemId") Long itemId,
            @Valid @RequestBody UpdateChecklistItemRequest request,
            Principal principal
    )
{
        String email = getEmail(principal);
        ChecklistItemDto item = groupTaskService.updateChecklistItem(groupId, taskId, itemId, request, email);
        return ResponseEntity.ok(APIResponse.success("Checklist item updated successfully", item));
    }
```

<a id="c18-m11"></a>

### 18.11. deleteChecklistItem

- `DELETE /api/groups/{groupId}/tasks/{taskId}/checklist-items/{itemId}`

Nguồn: [GroupTaskController:167](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:167).

```java
public ResponseEntity<APIResponse<Void>> deleteChecklistItem(
            @PathVariable("groupId") Long groupId,
            @PathVariable("taskId") Long taskId,
            @PathVariable("itemId") Long itemId,
            Principal principal
    )
{
        String email = getEmail(principal);
        groupTaskService.deleteChecklistItem(groupId, taskId, itemId, email);
        return ResponseEntity.ok(APIResponse.success("Checklist item deleted successfully", null));
    }
```

<a id="c18-m12"></a>

### 18.12. getComments

- `GET /api/groups/{groupId}/tasks/{taskId}/comments`

Nguồn: [GroupTaskController:180](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:180).

```java
public ResponseEntity<APIResponse<PageResponse<TaskCommentDto>>> getComments(
            @PathVariable("groupId") Long groupId,
            @PathVariable("taskId") Long taskId,
            @RequestParam(value = "page", defaultValue = "0") int page,
            @RequestParam(value = "size", defaultValue = "20") int size,
            Principal principal
    )
{
        String email = getEmail(principal);
        PageResponse<TaskCommentDto> comments = groupTaskService.getComments(groupId, taskId, page, size, email);
        return ResponseEntity.ok(APIResponse.success("Comments retrieved successfully", comments));
    }
```

<a id="c18-m13"></a>

### 18.13. createComment

- `POST /api/groups/{groupId}/tasks/{taskId}/comments`

Nguồn: [GroupTaskController:194](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:194).

```java
public ResponseEntity<APIResponse<TaskCommentDto>> createComment(
            @PathVariable("groupId") Long groupId,
            @PathVariable("taskId") Long taskId,
            @Valid @RequestBody CreateTaskCommentRequest request,
            Principal principal
    )
{
        String email = getEmail(principal);
        TaskCommentDto comment = groupTaskService.createComment(groupId, taskId, request, email);
        return ResponseEntity.status(HttpStatus.CREATED).body(APIResponse.success("Comment added successfully", comment));
    }
```

<a id="c18-m14"></a>

### 18.14. updateComment

- `PATCH /api/groups/{groupId}/tasks/{taskId}/comments/{commentId}`

Nguồn: [GroupTaskController:207](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:207).

```java
public ResponseEntity<APIResponse<TaskCommentDto>> updateComment(
            @PathVariable("groupId") Long groupId,
            @PathVariable("taskId") Long taskId,
            @PathVariable("commentId") Long commentId,
            @Valid @RequestBody UpdateTaskCommentRequest request,
            Principal principal
    )
{
        String email = getEmail(principal);
        TaskCommentDto comment = groupTaskService.updateComment(groupId, taskId, commentId, request, email);
        return ResponseEntity.ok(APIResponse.success("Comment updated successfully", comment));
    }
```

<a id="c18-m15"></a>

### 18.15. deleteComment

- `DELETE /api/groups/{groupId}/tasks/{taskId}/comments/{commentId}`

Nguồn: [GroupTaskController:221](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:221).

```java
public ResponseEntity<APIResponse<Void>> deleteComment(
            @PathVariable("groupId") Long groupId,
            @PathVariable("taskId") Long taskId,
            @PathVariable("commentId") Long commentId,
            Principal principal
    )
{
        String email = getEmail(principal);
        groupTaskService.deleteComment(groupId, taskId, commentId, email);
        return ResponseEntity.ok(APIResponse.success("Comment deleted successfully", null));
    }
```

<a id="c18-m16"></a>

### 18.16. getActivities

- `GET /api/groups/{groupId}/tasks/{taskId}/activities`

Nguồn: [GroupTaskController:234](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:234).

```java
public ResponseEntity<APIResponse<PageResponse<TaskActivityDto>>> getActivities(
            @PathVariable("groupId") Long groupId,
            @PathVariable("taskId") Long taskId,
            @RequestParam(value = "page", defaultValue = "0") int page,
            @RequestParam(value = "size", defaultValue = "20") int size,
            Principal principal
    )
{
        String email = getEmail(principal);
        PageResponse<TaskActivityDto> activities = groupTaskService.getActivities(groupId, taskId, page, size, email);
        return ResponseEntity.ok(APIResponse.success("Activities retrieved successfully", activities));
    }
```

<a id="c18-m17"></a>

### 18.17. getMyAssignedTasks

- `GET /api/tasks/me`

Nguồn: [GroupTaskController:248](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:248).

```java
public ResponseEntity<APIResponse<PageResponse<TaskSummaryDto>>> getMyAssignedTasks(
            @RequestParam(value = "groupId", required = false) Long groupId,
            @RequestParam(value = "status", required = false) String status,
            @RequestParam(value = "priority", required = false) String priority,
            @RequestParam(value = "overdue", required = false) Boolean overdue,
            @RequestParam(value = "dueBefore", required = false) Instant dueBefore,
            @RequestParam(value = "page", defaultValue = "0") int page,
            @RequestParam(value = "size", defaultValue = "20") int size,
            Principal principal
    )
{
        String email = getEmail(principal);
        PageResponse<TaskSummaryDto> tasks = groupTaskService.getMyAssignedTasks(groupId, status, priority, overdue, dueBefore, page, size, email);
        return ResponseEntity.ok(APIResponse.success("My assigned tasks retrieved successfully", tasks));
    }
```

<a id="c18-m18"></a>

### 18.18. reorderTask

- `POST /api/groups/{groupId}/tasks/reorder`

Nguồn: [GroupTaskController:265](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/GroupTaskController.java:265).

```java
public ResponseEntity<APIResponse<Void>> reorderTask(
            @PathVariable("groupId") Long groupId,
            @Valid @RequestBody ReorderTaskRequest request,
            Principal principal
    )
{
        String email = getEmail(principal);
        groupTaskService.reorderTask(groupId, request, email);
        return ResponseEntity.ok(APIResponse.success("Task reordered successfully", null));
    }
```

## 19. ImportController

<a id="c19-m1"></a>

### 19.1. importStudents

- `POST /api/imports/students`

Nguồn: [ImportController:43](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:43).

```java
public ResponseEntity<APIResponse<ImportResultResponse>> importStudents(
            @RequestPart("file") MultipartFile file,
            Principal principal
    ) throws Exception
{
        String email = principal.getName();
        ImportResultResponse response = importService.queueImportAccounts(file, ImportTargetType.STUDENT, email);
        return ResponseEntity.accepted().body(new APIResponse<>(202, "Student import job queued successfully", response));
    }
```

<a id="c19-m2"></a>

### 19.2. importMentors

- `POST /api/imports/mentors`

Nguồn: [ImportController:54](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:54).

```java
public ResponseEntity<APIResponse<ImportResultResponse>> importMentors(
            @RequestPart("file") MultipartFile file,
            Principal principal
    ) throws Exception
{
        String email = principal.getName();
        ImportResultResponse response = importService.queueImportAccounts(file, ImportTargetType.MENTOR, email);
        return ResponseEntity.accepted().body(new APIResponse<>(202, "Mentor import job queued successfully", response));
    }
```

<a id="c19-m3"></a>

### 19.3. getBatchStatus

- `GET /api/imports/{batchId}`

Nguồn: [ImportController:65](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:65).

```java
public ResponseEntity<APIResponse<ImportBatch>> getBatchStatus(
            @PathVariable("batchId") Long batchId
    )
{
        ImportBatch batch = importService.getBatchStatus(batchId);
        return ResponseEntity.ok(APIResponse.success(batch));
    }
```

<a id="c19-m4"></a>

### 19.4. getBatchErrors

- `GET /api/imports/{batchId}/errors`

Nguồn: [ImportController:74](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:74).

```java
public ResponseEntity<APIResponse<PageResponse<ImportRowErrorDto>>> getBatchErrors(
            @PathVariable("batchId") Long batchId,
            @RequestParam(value = "page", defaultValue = "0") int page,
            @RequestParam(value = "size", defaultValue = "20") int size,
            @RequestParam(value = "search", required = false) String search,
            @RequestParam(value = "rowNumber", required = false) Integer rowNumber,
            @RequestParam(value = "fieldName", required = false) String fieldName,
            @RequestParam(value = "errorCode", required = false) ImportErrorCode errorCode
    )
{
        if (page < 0) {
            throw new BadRequestException("Page index must be zero or greater");
        }
        if (size < 1 || size > 100) {
            throw new BadRequestException("Page size must be between 1 and 100");
        }
        if (rowNumber != null && rowNumber < 1) {
            throw new BadRequestException("Row number must be greater than zero");
        }

        Pageable pageable = PageRequest.of(page, size, Sort.by(
                Sort.Order.asc("rowNumber"),
                Sort.Order.asc("id")
        ));
        Page<ImportRowErrorDto> errors = importService.getBatchErrors(
                batchId,
                search,
                rowNumber,
                fieldName,
                errorCode,
                pageable
        );
        return ResponseEntity.ok(APIResponse.success(PageResponse.from(errors)));
    }
```

<a id="c19-m5"></a>

### 19.5. getStudentTemplate

- `GET /api/imports/templates/students`

Nguồn: [ImportController:110](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:110).

```java
public ResponseEntity<byte[]> getStudentTemplate()
{
        return xlsxTemplateResponse(
                "SU26_EXE101_Group_List_template.xlsx",
                templateWorkbookFactory.createStudentTemplate()
        );
    }
```

<a id="c19-m6"></a>

### 19.6. getMentorTemplate

- `GET /api/imports/templates/mentors`

Nguồn: [ImportController:119](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:119).

```java
public ResponseEntity<byte[]> getMentorTemplate()
{
        return xlsxTemplateResponse(
                "mentor_ID_matrix_template.xlsx",
                templateWorkbookFactory.createMentorTemplate()
        );
    }
```

<a id="c19-m7"></a>

### 19.7. getProblemBankTemplate

- `GET /api/imports/templates/problem-bank`

Nguồn: [ImportController:128](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ImportController.java:128).

```java
public ResponseEntity<byte[]> getProblemBankTemplate()
{
        return xlsxTemplateResponse(
                "Guideline_EXE101_problem_bank_template.xlsx",
                templateWorkbookFactory.createProblemBankTemplate()
        );
    }
```

## 20. InstructorGroupBoardController

<a id="c20-m1"></a>

### 20.1. getBoard

- `GET /api/instructor/groups/board`

Nguồn: [InstructorGroupBoardController:36](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorGroupBoardController.java:36).

Annotation quyền: `@PreAuthorize("hasRole('INSTRUCTOR')")`.

```java
public ResponseEntity<APIResponse<InstructorGroupBoardResponseDto>> getBoard(
            @RequestParam(defaultValue = "0") int page,
            @RequestParam(defaultValue = "12") int size,
            @RequestParam(required = false) String term,
            @RequestParam(required = false) String courseCode,
            @RequestParam(required = false) String search,
            @RequestParam(defaultValue = "ALL") InstructorGroupAssignmentFilter assignment,
            Principal principal
    )
{
        if (page < 0) {
            throw new BadRequestException("Page index must be zero or greater");
        }
        if (size < 1 || size > 100) {
            throw new BadRequestException("Page size must be between 1 and 100");
        }

        Pageable pageable = PageRequest.of(page, size, Sort.by(
                Sort.Order.desc("createdAt"),
                Sort.Order.desc("id")
        ));
        InstructorGroupBoardResponseDto board = instructorGroupBoardService.getBoard(
                principal.getName(), term, courseCode, search, assignment, pageable);
        return ResponseEntity.ok(APIResponse.success("Instructor group board retrieved successfully", board));
    }
```

<a id="c20-m2"></a>

### 20.2. claimGroup

- `POST /api/instructor/groups/{groupId}/claim`

Nguồn: [InstructorGroupBoardController:63](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorGroupBoardController.java:63).

Annotation quyền: `@PreAuthorize("hasRole('INSTRUCTOR')")`.

```java
public ResponseEntity<APIResponse<InstructorGroupBoardItemDto>> claimGroup(
            @PathVariable Long groupId,
            Principal principal
    )
{
        InstructorGroupBoardItemDto group =
                instructorGroupBoardService.claimGroup(groupId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Group claimed successfully", group));
    }
```

## 21. InstructorProblemController

<a id="c21-m1"></a>

### 21.1. getPendingProblems

- `GET /api/instructor/problems/pending`

Nguồn: [InstructorProblemController:33](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorProblemController.java:33).

```java
public ResponseEntity<APIResponse<List<ProblemSummaryDto>>> getPendingProblems(Principal principal)
{
        List<ProblemSummaryDto> problems = problemBankService.getPendingProblemsForInstructor(principal.getName());
        return ResponseEntity.ok(APIResponse.success("Pending problems retrieved successfully", problems));
    }
```

<a id="c21-m2"></a>

### 21.2. reviewProblem

- `PATCH /api/instructor/problems/{problemId}/review`

Nguồn: [InstructorProblemController:40](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorProblemController.java:40).

```java
public ResponseEntity<APIResponse<ProblemDetailDto>> reviewProblem(
            @PathVariable Long problemId,
            @Valid @RequestBody ReviewProblemRequest request,
            Principal principal
    )
{
        ProblemDetailDto problem = problemBankService.reviewProblemAsInstructor(problemId, request, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Problem reviewed successfully", problem));
    }
```

## 22. InstructorSubmissionController

<a id="c22-m1"></a>

### 22.1. getSubmissions

- `GET /api/instructor/submissions`

Nguồn: [InstructorSubmissionController:29](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/InstructorSubmissionController.java:29).

```java
public ResponseEntity<APIResponse<List<MilestoneSubmissionDto>>> getSubmissions(
            @RequestParam(required = false) String term,
            @RequestParam(required = false) String courseCode,
            @RequestParam(required = false) Long milestoneId,
            @RequestParam(required = false) Long groupId,
            @RequestParam(required = false) SubmissionStatus status,
            @RequestParam(required = false) Boolean late,
            Principal principal)
{
        List<MilestoneSubmissionDto> submissions =
                submissionService.getInstructorSubmissions(
                        term, courseCode, milestoneId, groupId, status, late,
                        principal.getName());
        return ResponseEntity.ok(
                APIResponse.success("Instructor submissions retrieved successfully", submissions));
    }
```

## 23. MentorAvailabilityController

<a id="c23-m1"></a>

### 23.1. createSlot

- `POST /api/mentor/availability`

Nguồn: [MentorAvailabilityController:36](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorAvailabilityController.java:36).

```java
public ResponseEntity<APIResponse<MentorAvailabilitySlotDto>> createSlot(
            @Valid @RequestBody CreateAvailabilitySlotRequest request,
            Principal principal
    )
{
        MentorAvailabilitySlotDto slot = availabilityService.createSlot(request, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Availability slot created successfully", slot));
    }
```

<a id="c23-m2"></a>

### 23.2. listSlots

- `GET /api/mentor/availability`

Nguồn: [MentorAvailabilityController:48](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorAvailabilityController.java:48).

```java
public ResponseEntity<APIResponse<List<MentorAvailabilitySlotDto>>> listSlots(Principal principal)
{
        List<MentorAvailabilitySlotDto> slots = availabilityService.listSlots(principal.getName());
        return ResponseEntity.ok(APIResponse.success("Availability slots retrieved successfully", slots));
    }
```

<a id="c23-m3"></a>

### 23.3. listMySlots

- `GET /api/mentor/availability/me`

Nguồn: [MentorAvailabilityController:57](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorAvailabilityController.java:57).

```java
public ResponseEntity<APIResponse<List<MentorAvailabilitySlotDto>>> listMySlots(Principal principal)
{
        return listSlots(principal);
    }
```

<a id="c23-m4"></a>

### 23.4. updateSlot

- `PATCH /api/mentor/availability/{id}`

Nguồn: [MentorAvailabilityController:65](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorAvailabilityController.java:65).

```java
public ResponseEntity<APIResponse<MentorAvailabilitySlotDto>> updateSlot(
            @PathVariable("id") Long id,
            @Valid @RequestBody UpdateAvailabilitySlotRequest request,
            Principal principal
    )
{
        MentorAvailabilitySlotDto slot = availabilityService.updateSlot(id, request, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Availability slot updated successfully", slot));
    }
```

<a id="c23-m5"></a>

### 23.5. cancelSlot

- `DELETE /api/mentor/availability/{id}`

Nguồn: [MentorAvailabilityController:78](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorAvailabilityController.java:78).

```java
public ResponseEntity<APIResponse<Void>> cancelSlot(
            @PathVariable("id") Long id,
            Principal principal
    )
{
        availabilityService.cancelSlot(id, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Availability slot canceled successfully", null));
    }
```

## 24. MentorMeetingReportController

<a id="c24-m1"></a>

### 24.1. listReportTerms

- `GET /api/mentor/meeting-reports/terms`

Nguồn: [MentorMeetingReportController:33](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorMeetingReportController.java:33).

Annotation quyền: `@PreAuthorize("hasRole('MENTOR')")`.

```java
public ResponseEntity<APIResponse<List<MentorReportTermDto>>> listReportTerms(Principal principal)
{
        return ResponseEntity.ok(APIResponse.success(
                "Meeting report terms retrieved successfully",
                meetingReportService.listReportTerms(principal.getName())
        ));
    }
```

<a id="c24-m2"></a>

### 24.2. exportReport

- `GET /api/mentor/meeting-reports/export.xlsx`

Nguồn: [MentorMeetingReportController:42](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MentorMeetingReportController.java:42).

Annotation quyền: `@PreAuthorize("hasRole('MENTOR')")`.

```java
public ResponseEntity<byte[]> exportReport(
            @RequestParam(value = "term", required = false) String term,
            Principal principal
    )
{
        byte[] body = meetingReportService.exportReport(term, principal.getName());
        return ResponseEntity.ok()
                .header(HttpHeaders.CONTENT_DISPOSITION,
                        "attachment; filename=\"mentor-meeting-report-" + safeTerm(term) + ".xlsx\"")
                .contentType(MediaType.parseMediaType(
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"))
                .body(body);
    }
```

## 25. MilestoneGradeController

<a id="c25-m1"></a>

### 25.1. getGradeBySubmissionId

- `GET /api/milestone-submissions/{submissionId}/grades`

Nguồn: [MilestoneGradeController:62](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeController.java:62).

```java
public ResponseEntity<APIResponse<MilestoneGradeDto>> getGradeBySubmissionId(
            @PathVariable("submissionId") Long submissionId,
            Principal principal
    )
{
        MilestoneGradeDto dto = milestoneGradeService.getGradeBySubmissionId(submissionId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Milestone grade retrieved successfully", dto));
    }
```

<a id="c25-m2"></a>

### 25.2. getGradesByGroupId

- `GET /api/milestone-submissions/groups/{groupId}/grades`

Nguồn: [MilestoneGradeController:74](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeController.java:74).

```java
public ResponseEntity<APIResponse<List<MilestoneGradeDto>>> getGradesByGroupId(
            @PathVariable("groupId") Long groupId,
            Principal principal
    )
{
        List<MilestoneGradeDto> list = milestoneGradeService.getGradesByGroupId(groupId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Grades retrieved successfully", list));
    }
```

<a id="c25-m3"></a>

### 25.3. bulkGrade

- `POST /api/milestone-submissions/bulk-grade`

Nguồn: [MilestoneGradeController:86](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeController.java:86).

```java
public ResponseEntity<APIResponse<Void>> bulkGrade()
{
        return ResponseEntity.status(404).body(APIResponse.error(404, "Bulk grading not supported"));
    }
```

## 26. MilestoneGradeMatrixController

<a id="c26-m1"></a>

### 26.1. upsertGroupGrade

- `PUT /api/instructor/milestones/{milestoneId}/groups/{groupId}/grade`

Nguồn: [MilestoneGradeMatrixController:42](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:42).

```java
public ResponseEntity<APIResponse<MilestoneGroupGradeDto>> upsertGroupGrade(
            @PathVariable Long milestoneId,
            @PathVariable Long groupId,
            @Valid @RequestBody UpsertMilestoneGroupGradeRequest request,
            Principal principal
    )
{
        MilestoneGroupGradeDto dto = gradeMatrixService.upsertGroupGrade(milestoneId, groupId, request, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Group milestone grade saved successfully", dto));
    }
```

<a id="c26-m2"></a>

### 26.2. upsertContributions

- `PUT /api/groups/{groupId}/milestones/{milestoneId}/contributions`

Nguồn: [MilestoneGradeMatrixController:54](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:54).

```java
public ResponseEntity<APIResponse<List<MilestoneMemberScoreDto>>> upsertContributions(
            @PathVariable Long groupId,
            @PathVariable Long milestoneId,
            @Valid @RequestBody UpsertMilestoneContributionsRequest request,
            Principal principal
    )
{
        List<MilestoneMemberScoreDto> dto = gradeMatrixService.upsertContributions(groupId, milestoneId, request, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Milestone contributions saved successfully", dto));
    }
```

<a id="c26-m3"></a>

### 26.3. getGroupGradeMatrix

- `GET /api/groups/{groupId}/grades`

Nguồn: [MilestoneGradeMatrixController:66](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:66).

```java
public ResponseEntity<APIResponse<GroupGradeMatrixDto>> getGroupGradeMatrix(
            @PathVariable Long groupId,
            Principal principal
    )
{
        GroupGradeMatrixDto dto = gradeMatrixService.getGroupGradeMatrix(groupId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Group grade matrix retrieved successfully", dto));
    }
```

<a id="c26-m4"></a>

### 26.4. exportInstructorGradesCsv

- `GET /api/instructor/grades/export.csv`

Nguồn: [MilestoneGradeMatrixController:76](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:76).

```java
public ResponseEntity<byte[]> exportInstructorGradesCsv(
            @RequestParam(required = false) String term,
            @RequestParam(required = false) String courseCode,
            @RequestParam(required = false) Long groupId,
            Principal principal
    )
{
        String csv = gradeMatrixService.exportInstructorGradesCsv(term, courseCode, groupId, principal.getName());
        byte[] csvBytes = csv.getBytes(StandardCharsets.UTF_8);
        byte[] body = new byte[UTF8_BOM.length + csvBytes.length];
        System.arraycopy(UTF8_BOM, 0, body, 0, UTF8_BOM.length);
        System.arraycopy(csvBytes, 0, body, UTF8_BOM.length, csvBytes.length);
        return ResponseEntity.ok()
                .header(HttpHeaders.CONTENT_DISPOSITION, "attachment; filename=\"" + exportFilename("csv", term, courseCode) + "\"")
                .contentType(new MediaType("text", "csv", StandardCharsets.UTF_8))
                .body(body);
    }
```

<a id="c26-m5"></a>

### 26.5. upsertContributionAgreement

- `PUT /api/groups/{groupId}/milestones/{milestoneId}/contribution-agreement`

Nguồn: [MilestoneGradeMatrixController:95](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:95).

```java
public ResponseEntity<APIResponse<ContributionAgreementDto>> upsertContributionAgreement(
            @PathVariable Long groupId,
            @PathVariable Long milestoneId,
            @Valid @RequestBody UpsertContributionAgreementRequest request,
            Principal principal)
{
        return ResponseEntity.ok(APIResponse.success("Contribution response saved successfully",
                gradeMatrixService.upsertContributionAgreement(groupId, milestoneId, request, principal.getName())));
    }
```

<a id="c26-m6"></a>

### 26.6. exportInstructorGradesXlsx

- `GET /api/instructor/grades/export.xlsx`

Nguồn: [MilestoneGradeMatrixController:106](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneGradeMatrixController.java:106).

```java
public ResponseEntity<byte[]> exportInstructorGradesXlsx(
            @RequestParam(required = false) String term,
            @RequestParam(required = false) String courseCode,
            @RequestParam(required = false) Long groupId,
            Principal principal
    )
{
        byte[] body = gradeMatrixService.exportInstructorGradesXlsx(term, courseCode, groupId, principal.getName());
        return ResponseEntity.ok()
                .header(HttpHeaders.CONTENT_DISPOSITION, "attachment; filename=\"" + exportFilename("xlsx", term, courseCode) + "\"")
                .contentType(MediaType.parseMediaType("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"))
                .body(body);
    }
```

## 27. MilestoneSubmissionController

<a id="c27-m1"></a>

### 27.1. getSubmissionById

- `GET /api/milestone-submissions/{submissionId}`

Nguồn: [MilestoneSubmissionController:61](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneSubmissionController.java:61).

```java
public ResponseEntity<APIResponse<MilestoneSubmissionDto>> getSubmissionById(
            @PathVariable("submissionId") Long submissionId,
            Principal principal
    )
{
        MilestoneSubmissionDto dto = milestoneSubmissionService.getSubmissionById(submissionId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Milestone submission retrieved successfully", dto));
    }
```

<a id="c27-m2"></a>

### 27.2. getSubmissionsByGroupId

- `GET /api/milestone-submissions/groups/{groupId}`

Nguồn: [MilestoneSubmissionController:73](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneSubmissionController.java:73).

```java
public ResponseEntity<APIResponse<List<MilestoneSubmissionDto>>> getSubmissionsByGroupId(
            @PathVariable("groupId") Long groupId,
            Principal principal
    )
{
        List<MilestoneSubmissionDto> list = milestoneSubmissionService.getSubmissionsByGroupId(groupId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Milestone submissions retrieved successfully", list));
    }
```

<a id="c27-m3"></a>

### 27.3. getSubmissionsByMilestoneId

- `GET /api/milestone-submissions/milestones/{milestoneId}`

Nguồn: [MilestoneSubmissionController:85](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/MilestoneSubmissionController.java:85).

```java
public ResponseEntity<APIResponse<List<MilestoneSubmissionDto>>> getSubmissionsByMilestoneId(
            @PathVariable("milestoneId") Long milestoneId,
            Principal principal
    )
{
        List<MilestoneSubmissionDto> list = milestoneSubmissionService.getSubmissionsByMilestoneId(milestoneId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Milestone submissions retrieved successfully", list));
    }
```

## 28. NotificationController

<a id="c28-m1"></a>

### 28.1. getNotifications

- `GET /api/notifications`

Nguồn: [NotificationController:25](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/NotificationController.java:25).

```java
public ResponseEntity<APIResponse<PageResponse<NotificationDto>>> getNotifications(
            @RequestParam(value = "page", defaultValue = "0") int page,
            @RequestParam(value = "size", defaultValue = "20") int size,
            @RequestParam(value = "unreadOnly", defaultValue = "false") boolean unreadOnly,
            Principal principal
    )
{
        PageResponse<NotificationDto> notifications = notificationService.getNotifications(principal.getName(), unreadOnly, page, size);
        return ResponseEntity.ok(APIResponse.success("Notifications retrieved successfully", notifications));
    }
```

<a id="c28-m2"></a>

### 28.2. getUnreadCount

- `GET /api/notifications/unread-count`

Nguồn: [NotificationController:37](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/NotificationController.java:37).

```java
public ResponseEntity<APIResponse<Long>> getUnreadCount(Principal principal)
{
        long count = notificationService.getUnreadCount(principal.getName());
        return ResponseEntity.ok(APIResponse.success("Unread count retrieved successfully", count));
    }
```

<a id="c28-m3"></a>

### 28.3. markAsRead

- `PATCH /api/notifications/{id}/read`

Nguồn: [NotificationController:44](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/NotificationController.java:44).

```java
public ResponseEntity<APIResponse<NotificationDto>> markAsRead(
            @PathVariable("id") Long id,
            Principal principal
    )
{
        NotificationDto notification = notificationService.markAsRead(id, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Notification marked as read successfully", notification));
    }
```

<a id="c28-m4"></a>

### 28.4. markAllAsRead

- `PATCH /api/notifications/read-all`

Nguồn: [NotificationController:54](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/NotificationController.java:54).

```java
public ResponseEntity<APIResponse<Long>> markAllAsRead(Principal principal)
{
        notificationService.markAllAsRead(principal.getName());
        long unreadCount = notificationService.getUnreadCount(principal.getName());
        return ResponseEntity.ok(APIResponse.success("All notifications marked as read successfully", unreadCount));
    }
```

## 29. ProblemController

<a id="c29-m1"></a>

### 29.1. getProblems

- `GET /api/problems`

Nguồn: [ProblemController:36](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemController.java:36).

```java
public ResponseEntity<APIResponse<PageResponse<ProblemSummaryDto>>> getProblems(
            @RequestParam(value = "page", defaultValue = "0") int page,
            @RequestParam(value = "size", defaultValue = "10") int size,
            @RequestParam(value = "search", required = false) String search,
            @RequestParam(value = "domainCode", required = false) String domainCode,
            @RequestParam(value = "difficulty", required = false) DifficultyLevel difficulty,
            @RequestParam(value = "expectedOutput", required = false) String expectedOutput,
            @RequestParam(value = "sourceType", required = false) ProblemSourceType sourceType,
            @RequestParam(value = "status", required = false) ProblemStatus status,
            Principal principal
    )
{
        if (principal == null) {
            throw new UnauthorizedException("Not authenticated");
        }

        if (page < 0) {
            throw new BadRequestException("Page index must be zero or greater");
        }
        if (size < 1 || size > 100) {
            throw new BadRequestException("Page size must be between 1 and 100");
        }

        Pageable pageable = PageRequest.of(page, size, Sort.by(
                Sort.Order.desc("createdAt"),
                Sort.Order.desc("id")
        ));
        String email = principal.getName();

        Page<ProblemSummaryDto> problems = problemBankService.searchProblems(
                search,
                domainCode,
                difficulty,
                expectedOutput,
                sourceType,
                status,
                pageable,
                email
        );

        return ResponseEntity.ok(APIResponse.success("Problems retrieved successfully", PageResponse.from(problems)));
    }
```

<a id="c29-m2"></a>

### 29.2. getProblemById

- `GET /api/problems/{id}`

Nguồn: [ProblemController:80](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemController.java:80).

```java
public ResponseEntity<APIResponse<ProblemDetailDto>> getProblemById(
            @PathVariable("id") Long id,
            Principal principal
    )
{
        if (principal == null) {
            throw new UnauthorizedException("Not authenticated");
        }
        ProblemDetailDto problemDto = problemBankService.getProblemById(id, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Problem details retrieved successfully", problemDto));
    }
```

## 30. ProblemCriteriaController

<a id="c30-m1"></a>

### 30.1. getActiveCriteria

- `GET /api/problem-evaluation-criteria`

Nguồn: [ProblemCriteriaController:27](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemCriteriaController.java:27).

```java
public ResponseEntity<APIResponse<List<ProblemEvaluationCriteriaDto>>> getActiveCriteria()
{
        List<ProblemEvaluationCriteriaDto> criteriaList = problemBankService.getActiveEvaluationCriteria();
        return ResponseEntity.ok(APIResponse.success("Active evaluation criteria retrieved successfully", criteriaList));
    }
```

## 31. ProblemDomainController

<a id="c31-m1"></a>

### 31.1. getProblemDomains

- `GET /api/problem-domains`

Nguồn: [ProblemDomainController:28](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemDomainController.java:28).

```java
public ResponseEntity<APIResponse<List<ProblemDomainDto>>> getProblemDomains(
            @RequestParam(value = "search", required = false) String search,
            @RequestParam(value = "status", required = false) ProblemStatus status
    )
{
        List<ProblemDomainDto> domains = problemBankService.getProblemDomains(search, status);
        return ResponseEntity.ok(APIResponse.success("Problem domains retrieved successfully", domains));
    }
```

## 32. ProblemImportController

<a id="c32-m1"></a>

### 32.1. importProblemBank

- `POST /api/imports/problem-bank`

Nguồn: [ProblemImportController:27](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProblemImportController.java:27).

Annotation quyền: `@PreAuthorize("hasRole('ADMIN')")`.

```java
public ResponseEntity<APIResponse<ImportResultResponse>> importProblemBank(
            @RequestPart("file") MultipartFile file,
            Principal principal
    ) throws Exception
{
        String email = principal.getName();
        ImportResultResponse response = problemImportService.queueProblemBankImport(file, email);
        return ResponseEntity.accepted().body(new APIResponse<>(202, "Problem bank import job queued successfully", response));
    }
```

## 33. ProfileController

<a id="c33-m1"></a>

### 33.1. getMyProfile

- `GET /api/profile/me`

Nguồn: [ProfileController:31](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProfileController.java:31).

```java
public ResponseEntity<APIResponse<SelfProfileResponse>> getMyProfile(Principal principal)
{
        SelfProfileResponse profile = profileService.getMyProfile(principal.getName());
        return ResponseEntity.ok(APIResponse.success("Profile retrieved successfully", profile));
    }
```

<a id="c33-m2"></a>

### 33.2. updateMyProfile

- `PATCH /api/profile/me`

Nguồn: [ProfileController:38](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProfileController.java:38).

```java
public ResponseEntity<APIResponse<SelfProfileResponse>> updateMyProfile(
            @Valid @RequestBody UpdateSelfProfileRequest request,
            Principal principal
    )
{
        SelfProfileResponse profile = profileService.updateMyProfile(principal.getName(), request);
        return ResponseEntity.ok(APIResponse.success("Profile updated successfully", profile));
    }
```

<a id="c33-m3"></a>

### 33.3. changeMyPassword

- `PATCH /api/profile/me/password`

Nguồn: [ProfileController:48](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/ProfileController.java:48).

```java
public ResponseEntity<APIResponse<Void>> changeMyPassword(
            @Valid @RequestBody ChangeOwnPasswordRequest request,
            Principal principal
    )
{
        profileService.changeMyPassword(principal.getName(), request);
        return ResponseEntity.ok(APIResponse.success("Password changed successfully", null));
    }
```

## 34. StudentAccountImportController

<a id="c34-m1"></a>

### 34.1. importStudentAccounts

- `POST /api/imports/student-accounts`

Nguồn: [StudentAccountImportController:39](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentAccountImportController.java:39).

Annotation quyền: `@PreAuthorize("hasRole('ADMIN')")`.

```java
public ResponseEntity<APIResponse<ImportResultResponse>> importStudentAccounts(
            @RequestPart("file") MultipartFile file,
            Principal principal
    ) throws Exception
{
        ImportResultResponse response = importService.queueStudentAccountImport(file, principal.getName());
        return ResponseEntity.accepted().body(new APIResponse<>(
                202,
                "Student account import job queued successfully",
                response
        ));
    }
```

<a id="c34-m2"></a>

### 34.2. getTemplate

- `GET /api/imports/templates/student-accounts`

Nguồn: [StudentAccountImportController:56](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentAccountImportController.java:56).

Annotation quyền: `@PreAuthorize("hasRole('ADMIN')")`.

```java
public ResponseEntity<byte[]> getTemplate()
{
        return ResponseEntity.ok()
                .header(HttpHeaders.CONTENT_DISPOSITION, "attachment; filename=\"student_accounts_template.xlsx\"")
                .contentType(MediaType.parseMediaType(
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                ))
                .body(templateFactory.createTemplate());
    }
```

## 35. StudentController

<a id="c35-m1"></a>

### 35.1. getStudentById

- `GET /api/students/{id}`

Nguồn: [StudentController:25](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentController.java:25).

```java
public ResponseEntity<APIResponse<StudentProfileDto>> getStudentById(
            @Parameter(description = "Student profile ID", required = true)
            @PathVariable("id") Long id,
            Principal principal
    )
{
        StudentProfileDto response = studentService.getStudentById(id, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Student profile retrieved successfully", response));
    }
```

<a id="c35-m2"></a>

### 35.2. getUngroupedStudents

- `GET /api/students/ungrouped`

Nguồn: [StudentController:39](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentController.java:39).

```java
public ResponseEntity<APIResponse<PageResponse<StudentProfileDto>>> getUngroupedStudents(
            @Parameter(description = "The term code (e.g., SU24)", required = true)
            @RequestParam(value = "term", required = false) String term,
            @Parameter(description = "The course code (e.g., EXE101)", required = true)
            @RequestParam(value = "courseCode", required = false) String courseCode,
            @Parameter(description = "Optional search query matching student code, name, or email")
            @RequestParam(value = "search", required = false) String search,
            @Parameter(description = "Zero-based page index")
            @RequestParam(value = "page", defaultValue = "0") int page,
            @Parameter(description = "Number of records per page")
            @RequestParam(value = "size", defaultValue = "20") int size,
            Principal principal
    )
{
        String email = principal != null ? principal.getName() : null;
        PageResponse<StudentProfileDto> response = studentService.getUngroupedStudents(term, courseCode, search, page, size, email);
        return ResponseEntity.ok(APIResponse.success("Ungrouped students retrieved successfully", response));
    }
```

## 36. StudentGroupGradeController

<a id="c36-m1"></a>

### 36.1. calculateAverageGradeForGroup

- `GET /api/student-groups/{groupId}/average-grade`

Nguồn: [StudentGroupGradeController:24](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/StudentGroupGradeController.java:24).

```java
public ResponseEntity<APIResponse<AverageGradeDto>> calculateAverageGradeForGroup(
            @PathVariable("groupId") Long groupId,
            Principal principal
    )
{
        AverageGradeDto dto = milestoneGradeService.calculateAverageGradeForGroup(groupId, principal.getName());
        return ResponseEntity.ok(APIResponse.success("Average grade calculated successfully", dto));
    }
```

## 37. TaskBoardController

<a id="c37-m1"></a>

### 37.1. getBoards

- `GET /api/groups/{groupId}/boards`
- `GET /api/groups/{groupId}/task-boards`

Nguồn: [TaskBoardController:38](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/TaskBoardController.java:38).

```java
public ResponseEntity<APIResponse<List<TaskBoardDto>>> getBoards(
            @Parameter(description = "The ID of the student group", required = true)
            @PathVariable("groupId") Long groupId,
            Principal principal
    )
{
        String email = getEmail(principal);
        List<TaskBoardDto> response = taskBoardService.getBoards(groupId, email);
        return ResponseEntity.ok(APIResponse.success("Task boards retrieved successfully", response));
    }
```

<a id="c37-m2"></a>

### 37.2. createBoard

- `POST /api/groups/{groupId}/boards`
- `POST /api/groups/{groupId}/task-boards`

Nguồn: [TaskBoardController:53](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/TaskBoardController.java:53).

```java
public ResponseEntity<APIResponse<TaskBoardDto>> createBoard(
            @Parameter(description = "The ID of the student group", required = true)
            @PathVariable("groupId") Long groupId,
            @Valid @RequestBody CreateTaskBoardRequest request,
            Principal principal
    )
{
        String email = getEmail(principal);
        TaskBoardDto response = taskBoardService.createBoard(groupId, request, email);
        return ResponseEntity.status(HttpStatus.CREATED).body(APIResponse.success("Task board created successfully", response));
    }
```

<a id="c37-m3"></a>

### 37.3. updateBoard

- `PATCH /api/groups/{groupId}/boards/{boardId}`
- `PATCH /api/groups/{groupId}/task-boards/{boardId}`

Nguồn: [TaskBoardController:69](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/TaskBoardController.java:69).

```java
public ResponseEntity<APIResponse<TaskBoardDto>> updateBoard(
            @Parameter(description = "The ID of the student group", required = true)
            @PathVariable("groupId") Long groupId,
            @Parameter(description = "The ID of the task board to update", required = true)
            @PathVariable("boardId") Long boardId,
            @RequestBody UpdateTaskBoardRequest request,
            Principal principal
    )
{
        String email = getEmail(principal);
        TaskBoardDto response = taskBoardService.updateBoard(groupId, boardId, request, email);
        return ResponseEntity.ok(APIResponse.success("Task board updated successfully", response));
    }
```

<a id="c37-m4"></a>

### 37.4. deleteBoard

- `DELETE /api/groups/{groupId}/boards/{boardId}`
- `DELETE /api/groups/{groupId}/task-boards/{boardId}`

Nguồn: [TaskBoardController:87](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/controllers/TaskBoardController.java:87).

```java
public ResponseEntity<APIResponse<Void>> deleteBoard(
            @Parameter(description = "The ID of the student group", required = true)
            @PathVariable("groupId") Long groupId,
            @Parameter(description = "The ID of the task board to delete", required = true)
            @PathVariable("boardId") Long boardId,
            Principal principal
    )
{
        String email = getEmail(principal);
        taskBoardService.deleteBoard(groupId, boardId, email);
        return ResponseEntity.ok(APIResponse.success("Task board deleted successfully", null));
    }
```

## DTO trực tiếp được các chữ ký controller tham chiếu

Các khai báo bên dưới giữ validation và message nguyên bản; chỉ bỏ package/import. DTO lồng nhau hoặc enum không được chữ ký trực tiếp nhắc đến cần theo source link tương ứng; không suy diễn string tự do thay enum. Các response DTO được giữ cùng request DTO để kiểm shape JSON. Không coi tài liệu OpenAPI nhúng trong Flowzy là bằng chứng runtime đã tuân thủ chúng.

### AcademicTermResponseDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/AcademicTermResponseDto.java).

```java
public record AcademicTermResponseDto(
    Long id,
    String code,
    AcademicTermStatus status,
    Instant closedAt,
    String closedByEmail,
    long groupCount,
    long totalExpectedFeedbacks,
    long totalSubmittedFeedbacks
) {}
```

### AdminChangePasswordRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/AdminChangePasswordRequest.java).

```java
public record AdminChangePasswordRequest(
    @NotBlank(message = "Email is required")
    @Email(message = "Invalid email format")
    String email,

    @NotBlank(message = "New password is required")
    @Size(min = 6, message = "New password must be at least 6 characters")
    String newPassword
) {}
```

### AdminDashboardOverviewDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/AdminDashboardOverviewDto.java).

```java
public record AdminDashboardOverviewDto(
    long totalActiveStudents,
    long totalActiveMentors,
    List<TopProblemDto> topProblems,
    List<TopDomainDto> topDomains
) {}
```

### AdminFeedbackResponseDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/AdminFeedbackResponseDto.java).

```java
public record AdminFeedbackResponseDto(
    Long id,
    FeedbackTermDto academicTerm,
    FeedbackGroupDto group,
    FeedbackStudentDto student,
    FeedbackTargetType targetType,
    FeedbackMentorDto mentor,
    FeedbackInstructorDto instructor,
    Integer rating,
    String comment,
    FeedbackStatus status,
    Instant submittedAt,
    Instant createdAt,
    Instant updatedAt
) {}
```

### AdminUserDetailDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/AdminUserDetailDto.java).

```java
public record AdminUserDetailDto(
    Long id,
    String email,
    Role role,
    AccountStatus status,
    Boolean mustChangePassword,
    Instant createdAt,
    Instant updatedAt,
    Instant lastLoginAt,
    StudentProfileDto studentProfile,
    MentorProfileDto mentorProfile,
    InstructorProfileDto instructorProfile,
    @JsonInclude(JsonInclude.Include.NON_NULL)
    List<StudentGroupMembershipDto> groupMemberships
) {}
```

### AdminUserSummaryDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/AdminUserSummaryDto.java).

```java
public record AdminUserSummaryDto(
    Long id,
    String email,
    Role role,
    AccountStatus status,
    Boolean mustChangePassword,
    String fullName,
    String code,
    Instant createdAt,
    Instant lastLoginAt,
    @JsonInclude(JsonInclude.Include.NON_NULL)
    List<StudentGroupMembershipDto> groupMemberships
) {}
```

### ArchiveTermStudentsResponseDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ArchiveTermStudentsResponseDto.java).

```java
public record ArchiveTermStudentsResponseDto(
        String termCode,
        long archivedStudents,
        long skippedPendingFeedbackStudents,
        long skippedActiveInOpenTerm,
        long alreadyInactiveStudents
) {
}
```

### AssignInstructorRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/AssignInstructorRequest.java).

```java
public record AssignInstructorRequest(
    @NotNull(message = "Instructor account ID is required")
    Long instructorId
) {}
```

### AssignMentorRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/AssignMentorRequest.java).

```java
public record AssignMentorRequest(
        @NotNull(message = "Mentor account ID is required")
        Long mentorId
) {
}
```

### AverageGradeDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/AverageGradeDto.java).

```java
public record AverageGradeDto(
    BigDecimal averageGrade
) {
    public BigDecimal getAverage() {
        return averageGrade;
    }
}
```

### BackupJobDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/BackupJobDto.java).

```java
public record BackupJobDto(
        Long id,
        BackupTriggerType triggerType,
        BackupJobStatus status,
        String fileName,
        Long fileSizeBytes,
        String errorMessage,
        Long requestedByAccountId,
        String requestedByEmail,
        Instant startedAt,
        Instant finishedAt,
        Instant createdAt
) {
}
```

### BackupScheduleSettingsDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/BackupScheduleSettingsDto.java).

```java
public record BackupScheduleSettingsDto(
        boolean enabled,
        String cronExpression,
        String timezone,
        String backupDir,
        int retentionDays,
        Instant lastTriggeredAt,
        Instant nextRunAt,
        Long updatedByAccountId,
        String updatedByEmail,
        Instant updatedAt
) {
}
```

### CancelMeetingRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CancelMeetingRequest.java).

```java
public record CancelMeetingRequest(
    @NotBlank(message = "Cancellation reason is required")
    @Size(max = 500, message = "Cancellation reason cannot exceed 500 characters")
    String reason
) {}
```

### ChangeOwnPasswordRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ChangeOwnPasswordRequest.java).

```java
public record ChangeOwnPasswordRequest(
        @NotBlank(message = "Current password is required")
        String currentPassword,

        @NotBlank(message = "New password is required")
        @Size(min = 6, message = "New password must be at least 6 characters")
        String newPassword
) {}
```

### ChecklistItemDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ChecklistItemDto.java).

```java
public record ChecklistItemDto(
    Long id,
    String title,
    Boolean completed,
    Long completedByAccountId,
    Instant completedAt,
    Long position
) {}
```

### ContributionAgreementDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ContributionAgreementDto.java).

```java
public record ContributionAgreementDto(
        int revision,
        ContributionAgreementStatus status,
        int approvedCount,
        int requiredCount,
        Long studentId,
        ContributionDecision decision,
        String reason,
        Instant respondedAt
) {}
```

### CourseMilestoneDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CourseMilestoneDto.java).

```java
public record CourseMilestoneDto(
    Long id,
    String term,
    String courseCode,
    String title,
    String description,
    Integer weight,
    Instant deadlineAt,
    BigDecimal maxScore,
    Long position,
    MilestoneStatus status,
    Long instructorId,
    String instructorName
) {}
```

### CreateAcademicTermRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateAcademicTermRequest.java).

```java
public record CreateAcademicTermRequest(
        @NotBlank(message = "Term code is required")
        @Size(max = 30, message = "Term code must be at most 30 characters")
        String code
) {}
```

### CreateAdminUserRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateAdminUserRequest.java).

```java
public record CreateAdminUserRequest(
    @NotBlank(message = "Email is required")
    @Email(message = "Invalid email format")
    String email,

    @NotNull(message = "Role is required")
    Role role,

    @NotBlank(message = "Initial password is required")
    @Size(min = 6, message = "Initial password must be at least 6 characters")
    String initialPassword,

    @Valid
    CreateStudentProfileRequest studentProfile,

    @Valid
    CreateMentorProfileRequest mentorProfile,

    @Valid
    CreateInstructorProfileRequest instructorProfile
) {
    public CreateAdminUserRequest(
        String email,
        Role role,
        String initialPassword,
        CreateStudentProfileRequest studentProfile,
        CreateMentorProfileRequest mentorProfile
    ) {
        this(email, role, initialPassword, studentProfile, mentorProfile, null);
    }
}
```

### CreateAvailabilitySlotRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateAvailabilitySlotRequest.java).

```java
public record CreateAvailabilitySlotRequest(
    @NotNull(message = "Start time is required")
    @Future(message = "Start time must be in the future")
    Instant startAt,

    @NotNull(message = "End time is required")
    @Future(message = "End time must be in the future")
    Instant endAt,

    @NotBlank(message = "Google Meet link is required")
    @Pattern(
        regexp = "^https://meet\\.google\\.com/[a-z]{3}-[a-z]{4}-[a-z]{3}$",
        message = "Google Meet link must strictly match format https://meet.google.com/xxx-xxxx-xxx (lowercase letters)"
    )
    String meetLink,

    @Size(max = 500, message = "Note must not exceed 500 characters")
    String note
) {}
```

### CreateChecklistItemRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateChecklistItemRequest.java).

```java
public record CreateChecklistItemRequest(
    @NotBlank(message = "Title must not be blank")
    @Size(max = 500, message = "Title must be at most 500 characters")
    String title
) {}
```

### CreateCourseMilestoneRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateCourseMilestoneRequest.java).

```java
public record CreateCourseMilestoneRequest(
    @NotBlank(message = "Term is required")
    @Size(max = 30, message = "Term must be at most 30 characters")
    String term,

    @NotBlank(message = "Course code is required")
    @Size(max = 30, message = "Course code must be at most 30 characters")
    String courseCode,

    @NotBlank(message = "Title is required")
    @Size(max = 255, message = "Title must be at most 255 characters")
    String title,

    String description,

    @Min(value = 0, message = "Weight must be non-negative")
    @Max(value = 100, message = "Weight cannot exceed 100")
    Integer weight,

    @JsonAlias("dueDate")
    Instant deadlineAt,

    @DecimalMin(value = "0.01", message = "Max score must be greater than 0")
    BigDecimal maxScore,

    @Min(value = 0, message = "Position must be non-negative")
    Long position
) {}
```

### CreateGroupRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateGroupRequest.java).

```java
public record CreateGroupRequest(
    @NotBlank(message = "Term is required")
    @Size(max = 30, message = "Term must be at most 30 characters")
    String term,

    @NotBlank(message = "Course code is required")
    @Size(max = 30, message = "Course code must be at most 30 characters")
    String courseCode,

    @NotBlank(message = "Group name is required")
    @Size(max = 255, message = "Group name must be at most 255 characters")
    String name,

    @Size(max = 255, message = "Project name must be at most 255 characters")
    String projectName,

    String ideaDescription,

    String researchDomain,

    @DecimalMin(value = "0.00", message = "Required GPA must be at least 0.00")
    @DecimalMax(value = "4.00", message = "Required GPA must be at most 4.00")
    @Digits(integer = 1, fraction = 2, message = "Required GPA format must be 1 integer and 2 decimal digits")
    BigDecimal requiredGpa,

    @DecimalMin(value = "0.0", message = "Target grade must be at least 0.0")
    @DecimalMax(value = "10.0", message = "Target grade must be at most 10.0")
    @Digits(integer = 2, fraction = 1, message = "Target grade format must be up to 2 integer and 1 decimal digits")
    BigDecimal targetGrade,

    List<@NotNull(message = "Recruitment need item must not be null") @Valid GroupRecruitmentNeedRequest> recruitmentNeeds
) {}
```

### CreateInvitationRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateInvitationRequest.java).

```java
public record CreateInvitationRequest(
    @NotBlank(message = "Student code or email is required")
    @Size(max = 255, message = "Student code or email must be at most 255 characters")
    String studentCodeOrEmail,

    @Size(max = 500, message = "Message must be at most 500 characters")
    String message
) {}
```

### CreateJoinRequestDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateJoinRequestDto.java).

```java
public record CreateJoinRequestDto(
    @Size(max = 500, message = "Message must be at most 500 characters")
    String message
) {}
```

### CreateOrBookMentorMeetingRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateOrBookMentorMeetingRequest.java).

```java
/**
 * Supports both the current mentor-created meeting flow and the legacy
 * leader-booked availability-slot flow on the same endpoint.
 */
public record CreateOrBookMentorMeetingRequest(
        Long slotId,
        Instant startAt,
        Instant endAt,
        @Pattern(regexp = "^https?://.+", message = "Meet link must use HTTP or HTTPS")
        @Size(max = 500) String meetLink,
        @Size(max = 500) String note
) {
    @AssertTrue(message = "Provide either slotId or both startAt and endAt")
    public boolean isValidRequestShape() {
        if (slotId != null) {
            return startAt == null && endAt == null;
        }
        return startAt != null && endAt != null;
    }

    @AssertTrue(message = "Meet link is required when creating a meeting directly")
    public boolean isMeetLinkProvidedForDirectMeeting() {
        return slotId != null || (meetLink != null && !meetLink.isBlank());
    }
}
```

### CreateProblemDomainRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateProblemDomainRequest.java).

```java
public record CreateProblemDomainRequest(
    @NotBlank(message = "Domain code is required")
    @Size(max = 50, message = "Domain code cannot exceed 50 characters")
    String code,

    @NotBlank(message = "Domain name is required")
    @Size(max = 255, message = "Domain name cannot exceed 255 characters")
    String name,

    String description,
    String macroDomain,
    String subDomain,
    String typicalExamples,
    String primaryDiscipline,
    String supportingDisciplines,
    String bestSources,
    String studentCapabilities,
    String potentialOutputs,
    String notes,

    ProblemStatus status
) {
    public CreateProblemDomainRequest(String code, String name, String description, ProblemStatus status) {
        this(code, name, description, null, null, null, null, null, null, null, null, null, status);
    }
}
```

### CreateProblemRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateProblemRequest.java).

```java
public record CreateProblemRequest(
    String domainCode,

    @NotBlank(message = "Title is required")
    @Size(max = 255, message = "Title cannot exceed 255 characters")
    String title,

    @NotBlank(message = "Statement is required")
    String statement,

    @Size(max = 100, message = "Problem code cannot exceed 100 characters")
    String code,

    @Size(max = 255, message = "Strategic theme cannot exceed 255 characters")
    String strategicTheme,

    @Size(max = 255, message = "Research area cannot exceed 255 characters")
    String researchArea,

    @NotNull(message = "Difficulty level is required")
    DifficultyLevel difficultyLevel,

    String expectedOutput,
    String ownerLab,
    String suggestedCourses,

    @Size(max = 500, message = "Drive folder link cannot exceed 500 characters")
    String driveFolderLink,

    ProblemStatus status
) {
    public CreateProblemRequest(String domainCode, String title, String statement, String code,
                                String strategicTheme, String researchArea, DifficultyLevel difficultyLevel,
                                String expectedOutput, ProblemStatus status) {
        this(domainCode, title, statement, code, strategicTheme, researchArea, difficultyLevel,
                expectedOutput, null, null, null, status);
    }
}
```

### CreateTaskBoardRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateTaskBoardRequest.java).

```java
public record CreateTaskBoardRequest(
    @NotBlank(message = "Name is required")
    @Size(max = 255, message = "Name must be at most 255 characters")
    String name,
    String description
) {}
```

### CreateTaskCommentRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateTaskCommentRequest.java).

```java
public record CreateTaskCommentRequest(
    @NotBlank(message = "Comment content must not be blank")
    String content
) {}
```

### CreateTaskRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/CreateTaskRequest.java).

```java
public record CreateTaskRequest(
    @NotBlank(message = "Title is required")
    @Size(max = 255, message = "Title must be at most 255 characters")
    String title,

    String description,
    String status,
    String priority,
    Instant dueAt,
    List<Long> assigneeStudentIds,
    Long boardId
) {
    public CreateTaskRequest(String title, String description, String status, String priority, Instant dueAt, List<Long> assigneeStudentIds) {
        this(title, description, status, priority, dueAt, assigneeStudentIds, null);
    }
}
```

### DashboardExecutionStatusDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/DashboardExecutionStatusDto.java).

```java
public record DashboardExecutionStatusDto(
    DashboardStatsDto stats,
    Map<GroupStatus, Long> groupStatusCounts,
    Map<TaskStatus, Long> taskStatusCounts,
    List<DashboardGroupProgressDto> groups
) {}
```

### DashboardGroupProgressDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/DashboardGroupProgressDto.java).

```java
public record DashboardGroupProgressDto(
    Long groupId,
    String term,
    String courseCode,
    String groupNo,
    String groupName,
    String projectName,
    GroupStatus status,
    Long mentorId,
    String mentorCode,
    String mentorName,
    int memberCount,
    long totalTasks,
    long completedTasks,
    long inProgressTasks,
    long overdueTasks,
    int progressPercent,
    Instant nextDueAt,
    Instant updatedAt
) {}
```

### DashboardMeetingDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/DashboardMeetingDto.java).

```java
public record DashboardMeetingDto(
    Long id,
    Long groupId,
    String groupName,
    String groupNo,
    String projectName,
    Long selectedProblemId,
    String selectedProblemTitle,
    Long mentorId,
    String mentorCode,
    String mentorName,
    Instant startAt,
    Instant endAt,
    MentorMeetingStatus status
) {}
```

### DashboardMentorDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/DashboardMentorDto.java).

```java
public record DashboardMentorDto(
    Long mentorId,
    String mentorCode,
    String mentorName,
    String email,
    long assignedGroupCount,
    long scheduledMeetingCount
) {}
```

### DashboardMilestoneStatusDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/DashboardMilestoneStatusDto.java).

```java
public record DashboardMilestoneStatusDto(
        Long milestoneId,
        String milestoneTitle,
        Long groupId,
        String groupName,
        String groupNo,
        Instant deadlineAt,
        boolean submitted,
        Instant submittedAt,
        Boolean late,
        SubmissionStatus submissionStatus,
        boolean graded,
        BigDecimal score,
        BigDecimal maxScoreSnapshot,
        boolean contributionsComplete,
        boolean gradeComplete
) {}
```

### DashboardProjectDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/DashboardProjectDto.java).

```java
public record DashboardProjectDto(
    Long groupId,
    String term,
    String courseCode,
    String groupNo,
    String groupName,
    String projectName,
    String ideaDescription,
    String researchDomain,
    GroupStatus groupStatus,
    int progressPercent
) {}
```

### DashboardStudentProgressDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/DashboardStudentProgressDto.java).

```java
public record DashboardStudentProgressDto(
    DashboardStatsDto stats,
    List<DashboardGroupProgressDto> groups,
    List<DashboardCheckpointDto> checkpoints
) {}
```

### FeedbackReceivedSummaryDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/FeedbackReceivedSummaryDto.java).

```java
public record FeedbackReceivedSummaryDto(
    Long targetId,
    String targetCode,
    String targetName,
    FeedbackTargetType targetType,
    String term,
    String courseCode,
    int totalCount,
    double averageRating,
    Map<Integer, Long> ratingDistribution,
    List<ReceivedFeedbackEntryDto> entries
) {}
```

### GoogleLoginRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/GoogleLoginRequest.java).

```java
public record GoogleLoginRequest(
        @NotBlank(message = "Google ID token is required")
        String idToken
) {}
```

### GroupDetailDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/GroupDetailDto.java).

```java
public record GroupDetailDto(
    Long id,
    String term,
    AcademicTermStatus termStatus,
    Instant termClosedAt,
    boolean studentReadOnly,
    String courseCode,
    String groupNo,
    String name,
    String projectName,
    String ideaDescription,
    String researchDomain,
    StudentProfileDto leader,
    BigDecimal requiredGpa,
    BigDecimal targetGrade,
    GroupStatus status,
    MentorProfileDto mentor,
    Long mentorAccountId,
    Long instructorId,
    Long instructorAccountId,
    String instructorCode,
    String instructorName,
    Boolean isLock,
    List<GroupMemberDto> members,
    SelectedProblemSummaryDto selectedProblem,
    List<GroupRecruitmentNeedDto> recruitmentNeeds
) {}
```

### GroupGradeMatrixDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/GroupGradeMatrixDto.java).

```java
public record GroupGradeMatrixDto(
        Long groupId,
        String groupName,
        String groupNo,
        String term,
        String courseCode,
        List<MilestoneColumn> milestones,
        List<MemberRow> members,
        boolean complete
) {
    public record MilestoneColumn(
            Long milestoneId,
            String title,
            Integer weight,
            java.math.BigDecimal maxScore,
            MilestoneGroupGradeDto groupGrade,
            boolean graded,
            boolean contributionsComplete,
            ContributionAgreementStatus contributionAgreementStatus,
            int contributionRevision,
            int approvedCount,
            int requiredCount,
            boolean gradeComplete
    ) {}

    public record MemberRow(
            Long studentId,
            String studentCode,
            String studentName,
            BigDecimal totalScore,
            boolean complete,
            List<MemberMilestoneScore> milestoneScores
    ) {}

    public record MemberMilestoneScore(
            Long milestoneId,
            BigDecimal contributionPercent,
            BigDecimal calculatedScore,
            ContributionDecision agreementDecision,
            String agreementReason,
            Instant agreementRespondedAt,
            boolean complete
    ) {}
}
```

### GroupJoinRequestDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/GroupJoinRequestDto.java).

```java
public record GroupJoinRequestDto(
    Long id,
    Long groupId,
    String groupName,
    String groupNo,
    String courseCode,
    String term,
    
    // Requesting Student info
    Long studentId,
    String studentCode,
    String studentName,
    
    GroupJoinRequestStatus status,
    String message,
    
    // Responder info
    Long respondedById,
    String respondedByCode,
    String respondedByName,
    
    Instant respondedAt,
    Instant createdAt,
    Instant updatedAt
) {}
```

### GroupSummaryDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/GroupSummaryDto.java).

```java
public record GroupSummaryDto(
    Long id,
    String term,
    AcademicTermStatus termStatus,
    Instant termClosedAt,
    boolean studentReadOnly,
    String courseCode,
    String groupNo,
    String name,
    String projectName,
    String leaderName,
    int memberCount,
    BigDecimal requiredGpa,
    BigDecimal targetGrade,
    GroupStatus status,
    Long mentorId,
    Long mentorAccountId,
    String mentorCode,
    String mentorName,
    Long instructorId,
    Long instructorAccountId,
    String instructorCode,
    String instructorName,
    Boolean isLock,
    SelectedProblemSummaryDto selectedProblem,
    List<GroupRecruitmentNeedDto> recruitmentNeeds
) {}
```

### GroupTaskBoardDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/GroupTaskBoardDto.java).

```java
public record GroupTaskBoardDto(
    Long groupId,
    String groupName,
    List<BoardColumnDto> columns,
    Integer activeTaskCount,
    Integer overdueTaskCount
) {}
```

### ImportRowErrorDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ImportRowErrorDto.java).

```java
public record ImportRowErrorDto(
    int rowNumber,
    String fieldName,
    ImportErrorCode errorCode,
    String errorMessage
) {}
```

### InstructorGroupBoardItemDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/InstructorGroupBoardItemDto.java).

```java
public record InstructorGroupBoardItemDto(
        Long id,
        String term,
        String courseCode,
        String groupNo,
        String name,
        String projectName,
        String ideaDescription,
        String researchDomain,
        Boolean isLock,
        int memberCount,
        Long mentorId,
        String mentorCode,
        String mentorName,
        Long instructorId,
        String instructorCode,
        String instructorName,
        InstructorGroupAssignmentState assignmentState,
        List<InstructorGroupBoardMemberDto> members
) {}
```

### InstructorGroupBoardResponseDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/InstructorGroupBoardResponseDto.java).

```java
public record InstructorGroupBoardResponseDto(
        InstructorGroupBoardSummaryDto summary,
        List<InstructorCourseGroupCountDto> courses,
        PageResponse<InstructorGroupBoardItemDto> groups
) {}
```

### InvitationDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/InvitationDto.java).

```java
public record InvitationDto(
    Long id,
    Long groupId,
    String groupName,
    String groupNo,
    String courseCode,
    String term,
    
    // Inviter info
    Long inviterId,
    String inviterCode,
    String inviterName,
    
    // Invitee / Student info
    Long inviteeId,
    String inviteeCode,
    String inviteeName,
    
    // Legacy mapping support
    Long studentId,
    String studentCode,
    String studentName,
    
    GroupInvitationStatus status,
    String message,
    Instant createdAt,
    Instant respondedAt
) {}
```

### LoginRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/LoginRequest.java).

```java
public record LoginRequest(
    @NotBlank(message = "Email is required")
    @Email(message = "Invalid email format")
    String email,

    @NotBlank(message = "Password is required")
    String password
) {}
```

### MentorAvailabilitySlotDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/MentorAvailabilitySlotDto.java).

```java
public record MentorAvailabilitySlotDto(
    Long id,
    Long mentorId,
    String mentorCode,
    String mentorName,
    Instant startAt,
    Instant endAt,
    String meetLink,
    String note,
    MentorAvailabilityStatus status,
    Instant createdAt,
    Instant updatedAt
) {}
```

### MentorMeetingDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/MentorMeetingDto.java).

```java
public record MentorMeetingDto(
    Long id,
    Long slotId,
    Long groupId,
    String groupName,
    String groupNo,
    String projectName,
    Long selectedProblemId,
    String selectedProblemTitle,
    Long mentorId,
    String mentorCode,
    String mentorName,
    Long bookedByStudentId,
    String bookedByStudentCode,
    String bookedByStudentName,
    Instant startAt,
    Instant endAt,
    String meetLink,
    String note,
    MentorMeetingStatus status,
    Long leaderConfirmedByStudentId,
    Instant leaderConfirmedAt,
    Instant mentorConfirmedAt,
    Instant completedAt,
    Instant canceledAt,
    String cancelReason,
    String evidenceImageUrl,
    Long evidenceSubmittedByStudentId,
    String evidenceSubmittedByStudentName,
    Instant evidenceSubmittedAt,
    Instant createdAt,
    Instant updatedAt
) {}
```

### MentorReportTermDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/MentorReportTermDto.java).

```java
public record MentorReportTermDto(
        String code,
        AcademicTermStatus status
) {}
```

### MilestoneGradeDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/MilestoneGradeDto.java).

```java
public record MilestoneGradeDto(
    Long id,
    Long submissionId,
    BigDecimal score,
    BigDecimal maxScore,
    String feedback,
    Long instructorId,
    Instant gradedAt
) {}
```

### MilestoneGroupGradeDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/MilestoneGroupGradeDto.java).

```java
public record MilestoneGroupGradeDto(
        Long id,
        Long milestoneId,
        Long groupId,
        BigDecimal score,
        BigDecimal maxScoreSnapshot,
        Integer weightSnapshot,
        String feedback,
        Long instructorId,
        Instant gradedAt,
        boolean contributionsComplete,
        boolean gradeComplete
) {}
```

### MilestoneMemberScoreDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/MilestoneMemberScoreDto.java).

```java
public record MilestoneMemberScoreDto(
        Long id,
        Long milestoneId,
        Long groupId,
        Long studentId,
        String studentCode,
        String studentName,
        BigDecimal contributionPercent,
        BigDecimal calculatedScore,
        BigDecimal maxScoreSnapshot,
        Integer weightSnapshot
) {}
```

### MilestoneSubmissionDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/MilestoneSubmissionDto.java).

```java
public record MilestoneSubmissionDto(
    Long id,
    Long milestoneId,
    Long groupId,
    String submittedBy,
    String fileUrl,
    String comments,
    Instant submittedAt,
    Boolean late,
    SubmissionStatus status,
    Long version,
    BigDecimal score,
    BigDecimal maxScore,
    String feedback,
    Instant gradedAt,
    Instant createdAt,
    Instant updatedAt
) {}
```

### MoveTaskRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/MoveTaskRequest.java).

```java
public record MoveTaskRequest(
    @NotNull(message = "Status is required")
    String status,

    @NotNull(message = "Position is required")
    @PositiveOrZero(message = "Position must be zero or greater")
    Long position,

    @NotNull(message = "Version is required")
    Long version
) {}
```

### NotificationDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/NotificationDto.java).

```java
public record NotificationDto(
        Long id,
        NotificationType type,
        String title,
        String body,
        String actionUrl,
        String entityType,
        String entityId,
        String payload,
        NotificationActionDto action,
        boolean read,
        Instant readAt,
        Instant createdAt
) {
    public Map<String, String> actionParams() {
        return action == null ? Map.of() : action.params();
    }
}
```

### ProblemDetailDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ProblemDetailDto.java).

```java
public record ProblemDetailDto(
    Long id,
    String code,
    String title,
    String statement,
    String strategicTheme,
    String researchArea,
    DifficultyLevel difficultyLevel,
    String expectedOutput,
    String ownerLab,
    String suggestedCourses,
    String driveFolderLink,
    ProblemSourceType sourceType,
    ProblemStatus status,
    DomainInfo domain,
    GroupInfo proposedByGroup,
    StudentInfo proposedByStudent,
    String reviewComment,
    ReviewerInfo reviewedBy,
    Instant reviewedAt,
    Instant createdAt,
    Instant updatedAt
) {
    public record DomainInfo(
        Long id,
        String code,
        String name
    ) {}

    public record GroupInfo(
        Long id,
        String groupNo,
        String name
    ) {}

    public record StudentInfo(
        Long id,
        String studentCode,
        String fullName
    ) {}

    public record ReviewerInfo(
        Long id,
        String email
    ) {}
}
```

### ProblemDomainDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ProblemDomainDto.java).

```java
public record ProblemDomainDto(
    Long id,
    String code,
    String name,
    String description,
    String macroDomain,
    String subDomain,
    String typicalExamples,
    String primaryDiscipline,
    String supportingDisciplines,
    String bestSources,
    String studentCapabilities,
    String potentialOutputs,
    String notes,
    ProblemStatus status,
    Instant createdAt,
    Instant updatedAt
) {}
```

### ProblemEvaluationCriteriaDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ProblemEvaluationCriteriaDto.java).

```java
public record ProblemEvaluationCriteriaDto(
    Long id,
    String code,
    String category,
    String question,
    String suggestion,
    Integer maxScore,
    Integer displayOrder,
    Boolean active,
    Instant createdAt,
    Instant updatedAt
) {}
```

### ProblemSummaryDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ProblemSummaryDto.java).

```java
public record ProblemSummaryDto(
    Long id,
    String code,
    String title,
    String domainCode,
    String domainName,
    DifficultyLevel difficultyLevel,
    ProblemSourceType sourceType,
    ProblemStatus status,
    String strategicTheme,
    String researchArea,
    Long proposedByGroupId,
    String proposedByGroupNo,
    String proposedByGroupName
) {}
```

### ProposeGroupProblemRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ProposeGroupProblemRequest.java).

```java
public record ProposeGroupProblemRequest(
    @NotBlank(message = "Title is required")
    @Size(max = 255, message = "Title cannot exceed 255 characters")
    String title,

    @NotBlank(message = "Statement is required")
    String statement,

    @Size(max = 255, message = "Strategic theme cannot exceed 255 characters")
    String strategicTheme,

    @Size(max = 255, message = "Research area cannot exceed 255 characters")
    String researchArea,

    @NotNull(message = "Difficulty level is required")
    DifficultyLevel difficultyLevel,

    String expectedOutput,

    @NotBlank(message = "Domain code is required")
    String domainCode
) {}
```

### RecruitmentRoleDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/RecruitmentRoleDto.java).

```java
public record RecruitmentRoleDto(
    String code,
    String category,
    String displayNameVi,
    String displayNameEn
) {}
```

### RefreshTokenRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/RefreshTokenRequest.java).

```java
public record RefreshTokenRequest(
    @NotBlank(message = "Refresh token is required")
    String refreshToken
) {}
```

### ReorderTaskRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ReorderTaskRequest.java).

```java
public record ReorderTaskRequest(
    @NotNull(message = "Task ID is required")
    Long taskId,
    @NotBlank(message = "Target status is required")
    String targetStatus,
    @NotNull(message = "Target index is required")
    Long targetIndex
) {}
```

### ReplaceTaskAssigneesRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ReplaceTaskAssigneesRequest.java).

```java
public record ReplaceTaskAssigneesRequest(
    @NotNull(message = "Assignee student IDs are required")
    List<Long> assigneeStudentIds,
    Long version
) {
    public List<Long> getAssigneeIds() {
        return assigneeStudentIds != null ? assigneeStudentIds : List.of();
    }
}
```

### ResetUserPasswordRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ResetUserPasswordRequest.java).

```java
public record ResetUserPasswordRequest(
    @NotBlank(message = "New password is required")
    @Size(min = 6, message = "New password must be at least 6 characters")
    String newPassword
) {}
```

### RestoreBackupResponseDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/RestoreBackupResponseDto.java).

```java
public record RestoreBackupResponseDto(
        String fileName,
        Long fileSizeBytes,
        Instant restoredAt
) {}
```

### ReviewProblemRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/ReviewProblemRequest.java).

```java
public record ReviewProblemRequest(
    @NotNull(message = "Review status is required")
    ProblemStatus status,

    String comment
) {}
```

### SelectProblemRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/SelectProblemRequest.java).

```java
public record SelectProblemRequest(
    @NotNull(message = "Problem ID is required")
    Long problemId
) {}
```

### StudentProfileDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/StudentProfileDto.java).

```java
public record StudentProfileDto(
    Long id,
    String studentCode,
    String fullName,
    String email,
    String phone,
    LocalDate dateOfBirth,
    Gender gender,
    String address,
    String major,
    String cohort,
    String className,
    AccountStatus status
) {}
```

### SubmitFeedbackRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/SubmitFeedbackRequest.java).

```java
public record SubmitFeedbackRequest(
    @NotNull(message = "Rating is required")
    @Min(value = 1, message = "Rating must be at least 1")
    @Max(value = 5, message = "Rating must be at most 5")
    Integer rating,
    @Size(max = 2000, message = "Comment must not exceed 2000 characters")
    String comment
) {}
```

### SubmitMeetingEvidenceRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/SubmitMeetingEvidenceRequest.java).

```java
public record SubmitMeetingEvidenceRequest(
        @NotBlank
        @Size(max = 2000)
        @Pattern(regexp = "^https?://.+", message = "Evidence URL must use HTTP or HTTPS")
        String imageUrl
) {}
```

### TaskActivityDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/TaskActivityDto.java).

```java
public record TaskActivityDto(
    Long id,
    Long taskId,
    Long groupId,
    ActivityActorDto actor,
    String activityType,
    Map<String, Object> details,
    Instant createdAt
) {}
```

### TaskBoardDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/TaskBoardDto.java).

```java
public record TaskBoardDto(
    Long id,
    Long groupId,
    String name,
    String description,
    Long position,
    Boolean defaultBoard,
    Long createdByStudentId,
    Instant archivedAt,
    Instant createdAt,
    Instant updatedAt
) {}
```

### TaskCommentDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/TaskCommentDto.java).

```java
public record TaskCommentDto(
    Long id,
    Long authorAccountId,
    String authorEmail,
    String authorFullName,
    String authorRole,
    String content,
    Instant editedAt,
    Instant createdAt,
    Instant updatedAt
) {}
```

### TaskDetailDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/TaskDetailDto.java).

```java
public record TaskDetailDto(
    Long id,
    String title,
    String description,
    TaskStatus status,
    TaskPriority priority,
    Instant dueAt,
    Long position,
    Long version,
    Long createdByStudentId,
    String createdByStudentName,
    Instant archivedAt,
    Instant createdAt,
    Instant updatedAt,
    List<TaskAssigneeDto> assignees,
    Integer checklistCount,
    Integer checklistCompletedCount,
    Integer checklistProgressPercent,
    Boolean overdue,
    List<ChecklistItemDto> checklistItems
) {}
```

### TaskSummaryDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/TaskSummaryDto.java).

```java
public record TaskSummaryDto(
    Long id,
    String title,
    TaskStatus status,
    TaskPriority priority,
    Instant dueAt,
    Long position,
    Long version,
    Long createdByStudentId,
    String createdByStudentName,
    Instant archivedAt,
    Instant createdAt,
    Instant updatedAt,
    List<TaskAssigneeDto> assignees,
    Integer checklistCount,
    Integer checklistCompletedCount,
    Integer checklistProgressPercent,
    Boolean overdue
) {}
```

### TermFeedbackResponseDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/TermFeedbackResponseDto.java).

```java
public record TermFeedbackResponseDto(
    Long id,
    Long academicTermId,
    String academicTermCode,
    AcademicTermStatus academicTermStatus,
    Long groupId,
    String groupName,
    Long studentId,
    String studentName,
    String studentCode,
    FeedbackTargetType targetType,
    Long mentorId,
    String mentorName,
    Long instructorId,
    String instructorName,
    Integer rating,
    String comment,
    FeedbackStatus status,
    Instant submittedAt,
    Long version
) {}
```

### TransferLeaderRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/TransferLeaderRequest.java).

```java
public record TransferLeaderRequest(
    @NotNull(message = "Student ID is required")
    Long studentId
) {}
```

### TvShowcaseDataDto

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/TvShowcaseDataDto.java).

```java
public record TvShowcaseDataDto<T>(
        Instant refreshedAt,
        String activeTermCode,
        List<T> content,
        int page,
        int number,
        int size,
        int numberOfElements,
        long totalElements,
        int totalPages,
        boolean hasNext,
        boolean hasPrevious
) {
    public record Project(
            Long groupId,
            String groupNo,
            String courseCode,
            String projectName,
            String instructorName,
            Leader leader,
            int progressPercent,
            long completedTasks,
            long totalTasks,
            long inProgressTasks,
            long overdueTasks,
            Instant nextDueAt
    ) {}

    public record Recruitment(
            Long groupId,
            String groupNo,
            String courseCode,
            String projectName,
            String instructorName,
            Leader leader,
            int totalOpenings,
            List<Position> positions
    ) {}

    public record Leader(
            Long id,
            String studentCode,
            String fullName,
            String email
    ) {}

    public record Position(
            RecruitmentRole role,
            String displayNameVi,
            String displayNameEn,
            Integer quantity
    ) {}
}
```

### UpdateAdminUserRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateAdminUserRequest.java).

```java
public record UpdateAdminUserRequest(
    @NotBlank(message = "Email is required")
    @Email(message = "Invalid email format")
    String email,

    @NotNull(message = "Status is required")
    AccountStatus status,

    @NotNull(message = "mustChangePassword is required")
    Boolean mustChangePassword,

    @Valid
    UpdateStudentProfileRequest studentProfile,

    @Valid
    UpdateMentorProfileRequest mentorProfile,

    @Valid
    UpdateInstructorProfileRequest instructorProfile
) {
    public UpdateAdminUserRequest(
        String email,
        AccountStatus status,
        Boolean mustChangePassword,
        UpdateStudentProfileRequest studentProfile,
        UpdateMentorProfileRequest mentorProfile
    ) {
        this(email, status, mustChangePassword, studentProfile, mentorProfile, null);
    }
}
```

### UpdateAvailabilitySlotRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateAvailabilitySlotRequest.java).

```java
public record UpdateAvailabilitySlotRequest(
    @Future(message = "Start time must be in the future")
    Instant startAt,

    @Future(message = "End time must be in the future")
    Instant endAt,

    @Pattern(
        regexp = "^https://meet\\.google\\.com/[a-z]{3}-[a-z]{4}-[a-z]{3}$",
        message = "Google Meet link must strictly match format https://meet.google.com/xxx-xxxx-xxx (lowercase letters)"
    )
    String meetLink,

    @Size(max = 500, message = "Note must not exceed 500 characters")
    String note
) {}
```

### UpdateBackupScheduleRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateBackupScheduleRequest.java).

```java
public record UpdateBackupScheduleRequest(
        @NotNull(message = "Enabled flag is required")
        Boolean enabled,

        @NotBlank(message = "Cron expression is required")
        String cronExpression,

        @NotBlank(message = "Timezone is required")
        String timezone,

        @Min(value = 1, message = "Retention days must be at least 1")
        @Max(value = 3650, message = "Retention days must not exceed 3650")
        Integer retentionDays
) {
}
```

### UpdateChecklistItemRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateChecklistItemRequest.java).

```java
public record UpdateChecklistItemRequest(
    @Size(max = 500, message = "Title must be at most 500 characters")
    String title,
    Boolean completed,
    @PositiveOrZero(message = "Position must be zero or greater")
    Integer position
) {}
```

### UpdateCourseMilestoneRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateCourseMilestoneRequest.java).

```java
public record UpdateCourseMilestoneRequest(
    @NotBlank(message = "Title is required")
    @Size(max = 255, message = "Title must be at most 255 characters")
    String title,

    String description,

    @Min(value = 0, message = "Weight must be non-negative")
    @Max(value = 100, message = "Weight cannot exceed 100")
    Integer weight,

    @JsonAlias("dueDate")
    Instant deadlineAt,

    @DecimalMin(value = "0.01", message = "Max score must be greater than 0")
    BigDecimal maxScore,

    @Min(value = 0, message = "Position must be non-negative")
    Long position,

    MilestoneStatus status
) {}
```

### UpdateGroupCriteriaRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateGroupCriteriaRequest.java).

```java
public record UpdateGroupCriteriaRequest(
    @DecimalMin(value = "0.00", message = "Required GPA must be at least 0.00")
    @DecimalMax(value = "4.00", message = "Required GPA must be at most 4.00")
    @Digits(integer = 1, fraction = 2, message = "Required GPA format must be 1 integer and 2 decimal digits")
    BigDecimal requiredGpa,

    @DecimalMin(value = "0.0", message = "Target grade must be at least 0.0")
    @DecimalMax(value = "10.0", message = "Target grade must be at most 10.0")
    @Digits(integer = 2, fraction = 1, message = "Target grade format must be up to 2 integer and 1 decimal digits")
    BigDecimal targetGrade,

    List<@NotNull(message = "Recruitment need item must not be null") @Valid GroupRecruitmentNeedRequest> recruitmentNeeds
) {}
```

### UpdateGroupLockRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateGroupLockRequest.java).

```java
public record UpdateGroupLockRequest(
        @NotNull(message = "isLock is required")
        Boolean isLock
) {}
```

### UpdateGroupRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateGroupRequest.java).

```java
public record UpdateGroupRequest(
    @Size(min = 1, max = 255, message = "Group name must be between 1 and 255 characters")
    String name,

    @Size(max = 255, message = "Project name must be at most 255 characters")
    String projectName,

    String ideaDescription,

    String researchDomain,

    @DecimalMin(value = "0.00", message = "Required GPA must be at least 0.00")
    @DecimalMax(value = "4.00", message = "Required GPA must be at most 4.00")
    @Digits(integer = 1, fraction = 2, message = "Required GPA format must be 1 integer and 2 decimal digits")
    BigDecimal requiredGpa,

    @DecimalMin(value = "0.0", message = "Target grade must be at least 0.0")
    @DecimalMax(value = "10.0", message = "Target grade must be at most 10.0")
    @Digits(integer = 2, fraction = 1, message = "Target grade format must be up to 2 integer and 1 decimal digits")
    BigDecimal targetGrade,

    List<@NotNull(message = "Recruitment need item must not be null") @Valid GroupRecruitmentNeedRequest> recruitmentNeeds
) {}
```

### UpdateMentorMeetingRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateMentorMeetingRequest.java).

```java
public record UpdateMentorMeetingRequest(
        Instant startAt,
        Instant endAt,
        @NotBlank(message = "Meet link is required")
        @Pattern(regexp = "^https?://.+", message = "Meet link must use HTTP or HTTPS")
        @Size(max = 500) String meetLink,
        @Size(max = 500) String note
) {}
```

### UpdateProblemDomainRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateProblemDomainRequest.java).

```java
public record UpdateProblemDomainRequest(
    @Size(max = 50, message = "Domain code cannot exceed 50 characters")
    String code,

    @Size(max = 255, message = "Domain name cannot exceed 255 characters")
    String name,

    String description,
    String macroDomain,
    String subDomain,
    String typicalExamples,
    String primaryDiscipline,
    String supportingDisciplines,
    String bestSources,
    String studentCapabilities,
    String potentialOutputs,
    String notes,

    ProblemStatus status
) {
    public UpdateProblemDomainRequest(String code, String name, String description, ProblemStatus status) {
        this(code, name, description, null, null, null, null, null, null, null, null, null, status);
    }
}
```

### UpdateProblemRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateProblemRequest.java).

```java
public record UpdateProblemRequest(
    String domainCode,

    @Size(max = 255, message = "Title cannot exceed 255 characters")
    String title,

    String statement,

    @Size(max = 100, message = "Problem code cannot exceed 100 characters")
    String code,

    @Size(max = 255, message = "Strategic theme cannot exceed 255 characters")
    String strategicTheme,

    @Size(max = 255, message = "Research area cannot exceed 255 characters")
    String researchArea,

    DifficultyLevel difficultyLevel,

    String expectedOutput,
    String ownerLab,
    String suggestedCourses,

    @Size(max = 500, message = "Drive folder link cannot exceed 500 characters")
    String driveFolderLink,

    ProblemStatus status
) {
    public UpdateProblemRequest(String domainCode, String title, String statement, String code,
                                String strategicTheme, String researchArea, DifficultyLevel difficultyLevel,
                                String expectedOutput, ProblemStatus status) {
        this(domainCode, title, statement, code, strategicTheme, researchArea, difficultyLevel,
                expectedOutput, null, null, null, status);
    }
}
```

### UpdateProblemStatusRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateProblemStatusRequest.java).

```java
public record UpdateProblemStatusRequest(
    @NotNull(message = "Status is required")
    ProblemStatus status
) {}
```

### UpdateSelfProfileRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateSelfProfileRequest.java).

```java
public record UpdateSelfProfileRequest(
        @Size(max = 255, message = "Full name must not exceed 255 characters")
        String fullName,

        @Size(max = 30, message = "Phone must not exceed 30 characters")
        String phone,

        LocalDate dateOfBirth,
        Gender gender,
        String address,

        @Size(max = 150, message = "Major must not exceed 150 characters")
        String major,

        @Size(max = 50, message = "Cohort must not exceed 50 characters")
        String cohort,

        @Size(max = 100, message = "Class name must not exceed 100 characters")
        String className,

        @Size(max = 150, message = "Job title must not exceed 150 characters")
        String jobTitle,

        @Size(max = 150, message = "Company must not exceed 150 characters")
        String company,

        String expertise,

        @Min(value = 0, message = "Years of experience must be at least 0")
        Integer yearsOfExperience,

        @Size(max = 500, message = "LinkedIn URL must not exceed 500 characters")
        String linkedinUrl,

        @Size(max = 150, message = "Department must not exceed 150 characters")
        String department
) {}
```

### UpdateTaskBoardRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateTaskBoardRequest.java).

```java
public record UpdateTaskBoardRequest(
    String name,
    String description,
    Long position,
    Boolean archived,
    Boolean defaultBoard
) {}
```

### UpdateTaskCommentRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateTaskCommentRequest.java).

```java
public record UpdateTaskCommentRequest(
    @NotBlank(message = "Comment content must not be blank")
    String content
) {}
```

### UpdateTaskRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpdateTaskRequest.java).

```java
public record UpdateTaskRequest(
    @Size(max = 255, message = "Title must be at most 255 characters")
    String title,

    String description,
    String priority,
    Instant dueAt,
    Boolean clearDueAt,

    @NotNull(message = "Version is required for optimistic concurrency control")
    Long version
) {
    public UpdateTaskRequest(String title, String description, String priority, Instant dueAt, Long version) {
        this(title, description, priority, dueAt, false, version);
    }
}
```

### UpsertContributionAgreementRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpsertContributionAgreementRequest.java).

```java
public record UpsertContributionAgreementRequest(
        @NotNull ContributionDecision decision,
        @Size(max = 1000) String reason
) {}
```

### UpsertMilestoneContributionsRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpsertMilestoneContributionsRequest.java).

```java
public record UpsertMilestoneContributionsRequest(
        @NotEmpty(message = "Contribution items are required")
        List<@Valid Item> items
) {
    public record Item(
            @NotNull(message = "Student ID is required")
            Long studentId,

            @NotNull(message = "Contribution percent is required")
            @DecimalMin(value = "0.00", message = "Contribution percent must be at least 0")
            @DecimalMax(value = "100.00", message = "Contribution percent must be at most 100")
            BigDecimal contributionPercent
    ) {}
}
```

### UpsertMilestoneGroupGradeRequest

[Source DTO](D:/f-spark/f-spark-api/src/main/java/com/fspark/api/dtos/UpsertMilestoneGroupGradeRequest.java).

```java
public record UpsertMilestoneGroupGradeRequest(
        @NotNull(message = "Score is required")
        @DecimalMin(value = "0.00", message = "Score must be non-negative")
        BigDecimal score,

        @Size(max = 5000, message = "Feedback must be at most 5000 characters")
        String feedback
) {}
```

