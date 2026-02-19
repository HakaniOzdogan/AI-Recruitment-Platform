# Submission Verify

Date: 2026-02-19
Bundle: `ik-otomasyon-submission-v1.0.0.zip`
Manifest: `submission/MANIFEST.txt`

## Build Result
- [x] `powershell -ExecutionPolicy Bypass -File ops/build-submission.ps1` calisti.
- [x] Zip olustu: `ik-otomasyon-submission-v1.0.0.zip`
- [x] Manifest olustu: `submission/MANIFEST.txt` (`321` satir)

## Zip Content Check
Var olmasi gerekenler:
- [x] `submission/backend/`
- [x] `submission/frontend/`
- [x] `submission/docs/`
- [x] `submission/ops/`
- [x] `submission/docker-compose.prod.yml`
- [x] `submission/VERSION`
- [x] `submission/CHANGELOG.md`
- [x] `submission/README.md`
- [x] `submission/MANIFEST.txt`

Olmamasi gerekenler:
- [x] `node_modules/`
- [x] `dist/`
- [x] `bin/`, `obj/`
- [x] `evidence/`
- [x] `submission/submission/`
- [x] `.env*`
- [x] obvious secret artifacts

## Verification Commands
Windows:
```powershell
powershell -ExecutionPolicy Bypass -File ops/build-submission.ps1
```

Linux/Mac:
```bash
bash ops/build-submission.sh
```

Zip quick forbidden scan (PowerShell):
```powershell
Add-Type -AssemblyName System.IO.Compression.FileSystem
$z=[IO.Compression.ZipFile]::OpenRead((Resolve-Path 'ik-otomasyon-submission-v1.0.0.zip'))
$z.Entries.FullName | Where-Object { $_ -match '\\.env|node_modules|\\dist\\|\\bin\\|\\obj\\|\\evidence\\' }
$z.Dispose()
```
