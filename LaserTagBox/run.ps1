$exePath = "C:\projects\LaserTagBox\LaserTagBox\bin\Debug\net8.0\LaserTagBox.exe"
$numRuns = 500
$timeoutSeconds = 60

for ($i = 1; $i -le $numRuns; $i++) {
    Write-Host "`n=== Run $i/$numRuns ==="
    $proc = Start-Process -FilePath $exePath -NoNewWindow -PassThru -RedirectStandardOutput "stdout.txt" -RedirectStandardError "stderr.txt"
    $start = Get-Date
    $timedOut = $false
    $lastStdoutLines = 0
    $lastStderrLines = 0

    while (-not $proc.HasExited) {
        Start-Sleep -Milliseconds 200
        $elapsed = (Get-Date) - $start
        if ($elapsed.TotalSeconds -ge $timeoutSeconds) {
            Write-Host "Timeout reached, killing process."
            $proc.Kill()
            $timedOut = $true
            break
        }
        # Zeige nur neue Zeilen aus stdout.txt
        if (Test-Path "stdout.txt") {
            $stdout = Get-Content "stdout.txt"
            $newLines = $stdout.Count - $lastStdoutLines
            if ($newLines -gt 0) {
                $stdout[-$newLines..-1] | ForEach-Object { Write-Host $_ }
                $lastStdoutLines = $stdout.Count
            }
        }
        # Zeige nur neue Zeilen aus stderr.txt
        if (Test-Path "stderr.txt") {
            $stderr = Get-Content "stderr.txt"
            $newLines = $stderr.Count - $lastStderrLines
            if ($newLines -gt 0) {
                $stderr[-$newLines..-1] | ForEach-Object { Write-Host "ERR: $_" -ForegroundColor Red }
                $lastStderrLines = $stderr.Count
            }
        }
    }
    # Am Ende alle Ausgaben noch einmal komplett zeigen
    if (Test-Path "stdout.txt") {
        Write-Host "== STDOUT =="
        Get-Content "stdout.txt" | ForEach-Object { Write-Host $_ }
        Remove-Item "stdout.txt" -ErrorAction SilentlyContinue
    }
    if (Test-Path "stderr.txt") {
        Write-Host "== STDERR =="
        Get-Content "stderr.txt" | ForEach-Object { Write-Host "ERR: $_" -ForegroundColor Red }
        Remove-Item "stderr.txt" -ErrorAction SilentlyContinue
    }
    Write-Host "Exit code: $($proc.ExitCode)"
    if ($timedOut) {
        Write-Host "Run $i skipped due to timeout."
    }
}
