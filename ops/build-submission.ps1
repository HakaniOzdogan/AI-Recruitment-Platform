param(
    [string]$OutDir = "submission"
)

$ErrorActionPreference = "Stop"
$rootDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$submissionDir = Join-Path $rootDir $OutDir
$version = (Get-Content (Join-Path $rootDir "VERSION") -Raw).Trim()
$zipName = "ik-otomasyon-submission-v$version.zip"
$zipPath = Join-Path $rootDir $zipName

function Copy-TreeFiltered {
    param(
        [string]$Source,
        [string]$Destination
    )

    if (!(Test-Path $Source)) { return }
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null

    $excludeDirNames = @("node_modules", "dist", "bin", "obj", "evidence", ".git", "submission")
    $excludeFilePatterns = @(".env*", "*.user", "*.suo", "*.zip")

    Get-ChildItem -Path $Source -Recurse -Force | ForEach-Object {
        $rel = $_.FullName.Substring($Source.Length).TrimStart('\','/')
        if ([string]::IsNullOrWhiteSpace($rel)) { return }

        foreach ($part in ($rel -split '[\\/]')) {
            if ($excludeDirNames -contains $part) { return }
        }
        foreach ($pattern in $excludeFilePatterns) {
            if ($_.Name -like $pattern) { return }
        }

        $target = Join-Path $Destination $rel
        if ($_.PSIsContainer) {
            New-Item -ItemType Directory -Force -Path $target | Out-Null
        } else {
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
            Copy-Item -Path $_.FullName -Destination $target -Force
        }
    }
}

Write-Host "[1/6] Clean submission directory"
if (Test-Path $submissionDir) {
    Remove-Item -Path $submissionDir -Recurse -Force
}
New-Item -ItemType Directory -Path $submissionDir | Out-Null

Write-Host "[2/6] Copy repository content (filtered)"
Copy-TreeFiltered -Source (Join-Path $rootDir "backend") -Destination (Join-Path $submissionDir "backend")
Copy-TreeFiltered -Source (Join-Path $rootDir "frontend") -Destination (Join-Path $submissionDir "frontend")
Copy-TreeFiltered -Source (Join-Path $rootDir "docs") -Destination (Join-Path $submissionDir "docs")
Copy-TreeFiltered -Source (Join-Path $rootDir "ops") -Destination (Join-Path $submissionDir "ops")

$rootFiles = @("README.md", "CHANGELOG.md", "VERSION", "docker-compose.yml", "docker-compose.prod.yml")
foreach ($f in $rootFiles) {
    $p = Join-Path $rootDir $f
    if (Test-Path $p) {
        Copy-Item $p -Destination (Join-Path $submissionDir $f) -Force
    }
}

Write-Host "[3/6] Optional PDF generation"
$pandoc = Get-Command pandoc -ErrorAction SilentlyContinue
$pdfStatusPath = Join-Path $submissionDir "docs/pdf-status.txt"
if ($pandoc) {
    try {
        pandoc (Join-Path $submissionDir "docs/final-report.md") -o (Join-Path $submissionDir "docs/final-report.pdf")
        pandoc (Join-Path $submissionDir "docs/slides-outline.md") -o (Join-Path $submissionDir "docs/slides-outline.pdf")
        "pandoc: PDF generation attempted." | Out-File -FilePath $pdfStatusPath -Encoding utf8
    } catch {
        "pandoc failed: $($_.Exception.Message)" | Out-File -FilePath $pdfStatusPath -Encoding utf8
    }
} else {
    "SKIP (pandoc not found)" | Out-File -FilePath $pdfStatusPath -Encoding utf8
}

Write-Host "[4/6] Build MANIFEST"
$manifestPath = Join-Path $submissionDir "MANIFEST.txt"
if (Test-Path $manifestPath) { Remove-Item $manifestPath -Force }
Get-ChildItem -Path $submissionDir -Recurse -File |
    Where-Object { $_.FullName -ne $manifestPath } |
    Sort-Object FullName |
    ForEach-Object {
        $hash = (Get-FileHash -Algorithm SHA256 -Path $_.FullName).Hash.ToLowerInvariant()
        $relative = $_.FullName.Substring($submissionDir.Length).TrimStart('\')
        "$hash  $relative" | Out-File -FilePath $manifestPath -Append -Encoding utf8
    }

Write-Host "[5/6] Create zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path $submissionDir -DestinationPath $zipPath -CompressionLevel Optimal -Force

Write-Host "[6/6] Done"
Write-Host "Output: $zipPath"
