$ErrorActionPreference = "Stop"
$rootDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$reportsDir = Join-Path $rootDir "docs/reports"

if (!(Test-Path $reportsDir)) {
    throw "Reports directory not found: $reportsDir"
}

$pandoc = Get-Command pandoc -ErrorAction SilentlyContinue
if (-not $pandoc) {
    Write-Host "SKIP (pandoc not found)"
    Write-Host "Manual: install pandoc and run this script again."
    exit 0
}

pandoc (Join-Path $reportsDir "final-report.md") -o (Join-Path $reportsDir "final-report.pdf")
pandoc (Join-Path $reportsDir "slides-outline.md") -o (Join-Path $reportsDir "slides-outline.pdf")
pandoc (Join-Path $reportsDir "evaluation-matrix.md") -o (Join-Path $reportsDir "evaluation-matrix.pdf")

Write-Host "Reports PDF generated in $reportsDir"
