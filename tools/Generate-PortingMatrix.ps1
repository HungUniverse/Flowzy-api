param(
    [string]$JavaRoot = 'D:\f-spark\f-spark-api\src\main\java',
    [string]$Output = (Join-Path $PSScriptRoot '..\docs\java-source-inventory.csv')
)

$implementedControllers = @('AuthController','ProfileController','NotificationController','AcademicTermController',
    'ProblemController','ProblemCriteriaController','ProblemDomainController','GroupRecruitmentRoleController','AdminUserController')
$implementedServices = @('JwtService','JwtServiceImpl','RefreshTokenService','RefreshTokenServiceImpl','TokenBlacklistService',
    'TokenBlacklistServiceImpl','GoogleTokenVerifier','GoogleTokenVerifierImpl','GoogleAuthService','GoogleAuthServiceImpl',
    'ProfileService','ProfileServiceImpl','NotificationService','NotificationServiceImpl','AcademicTermService','AcademicTermServiceImpl',
    'AdminUserService','AdminUserServiceImpl','ProblemBankService','ProblemBankServiceImpl','AdminSeedService','AdminSeedServiceImpl')

$rows = Get-ChildItem $JavaRoot -Recurse -Filter *.java | Sort-Object FullName | ForEach-Object {
    $relative = $_.FullName.Substring($JavaRoot.Length + 1).Replace('\','/')
    $name = $_.BaseName
    $category = ($relative -split '/')[3]
    $status = 'Pending'
    $mapping = ''
    $note = 'Requires a domain-specific behavioral port and tests.'
    switch ($category) {
        'models' {
            $entity = Join-Path $PSScriptRoot "..\src\Flowzy.Repository\Entities\$name.cs"
            if (Test-Path $entity) { $status='Implemented'; $mapping="src/Flowzy.Repository/Entities/$name.cs"; $note='EF Core entity mapped from the migrated PostgreSQL schema.' }
            else { $status='Merged'; $mapping='src/Flowzy.Repository/Data/FlowzyDbContext.cs'; $note='Persistence-only model represented by the DbContext/schema rather than a standalone entity.' }
        }
        'dtos' { $status='Generated'; $mapping='src/Flowzy.Service/Contracts/Generated/FsparkContracts.g.cs'; $note='Generated from the running Java OpenAPI oracle; hand-written implemented DTOs override this where behavior is ported.' }
        'enums' { $status='Generated'; $mapping='src/Flowzy.Service/Contracts/Generated/FsparkContracts.g.cs'; $note='OpenAPI enum values retained exactly in generated contracts and persisted as uppercase strings.' }
        'controllers' {
            if ($implementedControllers -contains $name) { $status='Implemented'; $mapping='src/Flowzy.Api/Controllers'; $note='Concrete ASP.NET controller and service behavior implemented.' }
            else { $status='Pending'; $mapping='src/Flowzy.Api/Compatibility/LegacyContract.cs'; $note='HTTP route/schema is available through the temporary compatibility fallback; business behavior is not yet complete.' }
        }
        'repositories' { $status='Merged'; $mapping='src/Flowzy.Repository/Data/FlowzyDbContext.cs'; $note='Spring Data repository/query is consolidated into EF Core repositories; domain-specific queries remain pending where their service is pending.' }
        'services' {
            if ($implementedServices -contains $name) { $status='Implemented'; $mapping='src/Flowzy.Service'; $note='Merged into the corresponding .NET application service.' }
            else { $status='Pending'; $mapping='src/Flowzy.Service'; $note='Contract exists, but domain business behavior still requires a real .NET service implementation.' }
        }
        'configs' { $status='Merged'; $mapping='src/Flowzy.Api/Program.cs'; $note='Merged into startup, middleware, authentication, Swagger, CORS, migration, and WebSocket configuration.' }
        'exceptions' { $status='Merged'; $mapping='src/Flowzy.Service/Exceptions/ApiException.cs'; $note='Consolidated into typed API exceptions and global error middleware.' }
        'mapping' { $status='Merged'; $mapping='src/Flowzy.Service'; $note='Mapping is colocated with the corresponding .NET application service.' }
        'utils' { $status='Pending'; $mapping='src/Flowzy.Service'; $note='Utility will be ported with its owning import/export/report domain.' }
        default { $status='Merged'; $mapping='src/Flowzy.Api'; $note='Application bootstrap merged into ASP.NET startup.' }
    }
    [pscustomobject]@{ JavaFile=$relative; Category=$category; Status=$status; CSharpMapping=$mapping; Notes=$note }
}

$rows | Export-Csv -Path $Output -NoTypeInformation -Encoding utf8
Write-Output "Wrote $($rows.Count) Java production mappings to $Output"
