$ErrorActionPreference = "Stop"
$rootDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$pdfPath = Join-Path $rootDir "sample-data/sample-candidate.pdf"
$docxPath = Join-Path $rootDir "sample-data/sample-candidate.docx"

if (!(Test-Path $pdfPath)) { throw "Eksik dosya: $pdfPath" }
if (!(Test-Path $docxPath)) { throw "Eksik dosya: $docxPath" }

function Read-FileHeaderWithRetry {
    param(
        [string]$Path,
        [int]$Length
    )

    for ($attempt = 1; $attempt -le 3; $attempt++) {
        $stream = $null
        try {
            $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
            if ($stream.Length -lt $Length) {
                throw "Dosya cok kisa: $Path"
            }

            $buffer = New-Object byte[] $Length
            [void]$stream.Read($buffer, 0, $Length)
            return $buffer
        } catch {
            if ($attempt -eq 3) { throw }
            Start-Sleep -Milliseconds 200
        } finally {
            if ($null -ne $stream) { $stream.Dispose() }
        }
    }
}

$pdfBytes = Read-FileHeaderWithRetry -Path $pdfPath -Length 5
$docxBytes = Read-FileHeaderWithRetry -Path $docxPath -Length 2

$pdfSig = [System.Text.Encoding]::ASCII.GetString($pdfBytes, 0, 5)
$docxSig = [System.Text.Encoding]::ASCII.GetString($docxBytes, 0, 2)

if ($pdfSig -ne "%PDF-") { throw "PDF signature fail. Beklenen: %PDF-" }
if ($docxSig -ne "PK") { throw "DOCX signature fail. Beklenen: PK" }

Write-Host "PASS: sample PDF/DOCX signatures are valid."
