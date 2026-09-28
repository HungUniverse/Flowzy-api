param([string]$Image = "flowzy-api-api:latest")
$ErrorActionPreference = "Stop"
$checkId = [Guid]::NewGuid().ToString("N").Substring(0, 10)
$networkName = "flowzy-backup-check-$checkId"
$databaseName = "$networkName-db"
$apiName = "$networkName-api"
$createdNetwork = $false
$createdDatabase = $false
$createdApi = $false
try {
    docker network create $networkName | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Cannot create isolated test network" }
    $createdNetwork = $true
    docker run -d --name $databaseName --network $networkName -e POSTGRES_DB=backup_check -e POSTGRES_USER=backup_check -e POSTGRES_PASSWORD=disposable_test_password postgres:16-alpine | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Cannot start disposable PostgreSQL" }
    $createdDatabase = $true
    for ($i = 0; $i -lt 30; $i++) {
        docker exec $databaseName pg_isready -U backup_check -d backup_check *> $null
        if ($LASTEXITCODE -eq 0) { break }
        Start-Sleep -Seconds 1
    }
    docker run -d --name $apiName --network $networkName -p "127.0.0.1::8080" `
        -e ASPNETCORE_URLS=http://+:8080 `
        -e "ConnectionStrings__Default=Host=$databaseName;Database=backup_check;Username=backup_check;Password=disposable_test_password" `
        -e Admin__Email=backup.smoke@local.test -e Admin__Password=SmokePassword123 `
        -e Jwt__SecretKey=dGVzdC1qd3Qtc2VjcmV0LWtleS10aGF0LWlzLWxvbmctZW5vdWdoLWZvci1oczI1Ng== `
        -e Backup__Directory=/app/backup-check -e Backup__SchedulerEnabled=false $Image | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Cannot start isolated API" }
    $createdApi = $true
    $address = (docker port $apiName 8080/tcp).Trim()
    $baseUrl = "http://$address"
    $ready = $false
    for ($i = 0; $i -lt 45; $i++) {
        try { if ((Invoke-RestMethod "$baseUrl/actuator/health" -TimeoutSec 2).status -eq "UP") { $ready = $true; break } } catch { }
        Start-Sleep -Seconds 1
    }
    if (-not $ready) { docker logs --tail 25 $apiName; throw "Isolated API did not become healthy" }
    # This SQL runs only in the freshly created disposable container, never the local Compose database.
    docker exec $databaseName psql -U backup_check -d backup_check -c "UPDATE accounts SET must_change_password=false WHERE email='backup.smoke@local.test'; INSERT INTO academic_terms(code,status) VALUES ('BACKUP_CHECK','OPEN');" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Cannot prepare isolated test data" }
    $login = Invoke-RestMethod "$baseUrl/api/auth/login" -Method Post -ContentType application/json -Body '{"email":"backup.smoke@local.test","password":"SmokePassword123"}'
    $headers = @{Authorization = "Bearer $($login.data.accessToken)"}
    $queued = Invoke-RestMethod "$baseUrl/api/admin/backups" -Method Post -Headers $headers
    $jobId = $queued.data.id
    for ($i = 0; $i -lt 100; $i++) {
        $job = (Invoke-RestMethod "$baseUrl/api/admin/backups/$jobId" -Headers $headers).data
        if ($job.status -in @("SUCCEEDED", "FAILED")) { break }
        Start-Sleep -Milliseconds 200
    }
    if ($job.status -ne "SUCCEEDED") { throw "Real API dump failed: $($job.errorMessage)" }
    $download = Invoke-WebRequest "$baseUrl/api/admin/backups/$jobId/download" -Headers $headers
    $bytes = [byte[]]$download.Content
    if ([Text.Encoding]::ASCII.GetString($bytes, 0, 5) -ne "PGDMP") { throw "Invalid custom dump signature" }
    docker exec $databaseName psql -U backup_check -d backup_check -c "UPDATE academic_terms SET status='CLOSED' WHERE code='BACKUP_CHECK';" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Cannot prepare restore assertion" }
    $client = [Net.Http.HttpClient]::new()
    $client.DefaultRequestHeaders.Authorization = [Net.Http.Headers.AuthenticationHeaderValue]::new("Bearer", $login.data.accessToken)
    $form = [Net.Http.MultipartFormDataContent]::new()
    try {
        $form.Add([Net.Http.ByteArrayContent]::new($bytes), "file", "backup.dump")
        $form.Add([Net.Http.StringContent]::new("RESTORE_DATABASE"), "confirmation")
        $response = $client.PostAsync("$baseUrl/api/admin/backups/restore", $form).GetAwaiter().GetResult()
        $body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if (-not $response.IsSuccessStatusCode) { throw "Real API restore failed: $body" }
    } finally { $form.Dispose(); $client.Dispose() }
    $restored = (docker exec $databaseName psql -U backup_check -d backup_check -tAc "SELECT status FROM academic_terms WHERE code='BACKUP_CHECK'").Trim()
    if ($restored -ne "OPEN") { throw "Data was not restored" }
    $interrupted = (docker exec $databaseName psql -U backup_check -d backup_check -tAc "SELECT status FROM backup_jobs WHERE id=$jobId").Trim()
    if ($interrupted -ne "FAILED") { throw "Dump's historical RUNNING job was not recovered" }
    Write-Output "PASS: empty PostgreSQL migrations, real API pg_dump, download, pg_restore and recovered job state."
    docker exec $apiName pg_dump --version
    docker exec $apiName pg_restore --version
} finally {
    # Exact generated names only; no volumes, user databases or broad container cleanup.
    if ($createdApi) { docker rm -f $apiName | Out-Null }
    if ($createdDatabase) { docker rm -f $databaseName | Out-Null }
    if ($createdNetwork) { docker network rm $networkName | Out-Null }
}
