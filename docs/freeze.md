# Freeze Procedure

## Release Freeze Scope
- Branch: `main`
- Release version: `1.0.0`
- Commit hash: `N/A (environment has no git history)`
- Alternate reference: `submission/MANIFEST.txt` timestamp + release zip `ik-otomasyon-submission-v1.0.0.zip`
- Release tag (manual): `v1.0.0`

## Freeze Rules
1. No further feature changes after freeze.
2. Only critical hotfix is allowed.
3. Critical hotfix requires minimum 2 approvals.
4. Every hotfix must update:
- `VERSION`
- `CHANGELOG.md`
- Related acceptance evidence docs

## Approval Record
- Freeze requested by: ____________________
- Approved by Reviewer-1: ________________ Date: __________
- Approved by Reviewer-2: ________________ Date: __________

## Release Tagging (manual)
1. `git checkout main`
2. `git pull`
3. `git rev-parse --short HEAD` ile release commit hash'ini bu dokumana yaz.
4. Eger ortamda git history yoksa (`Needed a single revision`), commit hash alanini `N/A (no git history in this environment)` olarak birak ve `submission/MANIFEST.txt` referansini kullan.
5. `git tag -a v1.0.0 -m "Release 1.0.0"`
6. `git push origin v1.0.0`
7. Ayrintili adimlar icin: `docs/release-steps.md`

## Release Bundle Creation
1. Run pre-freeze checks:
- Linux/Mac: `bash ops/pre-freeze-check.sh`
- Windows: `powershell -ExecutionPolicy Bypass -File ops/pre-freeze-check.ps1`
2. Generate evidence snapshot:
- Linux/Mac: `bash ops/evidence.sh`
- Windows: `powershell -ExecutionPolicy Bypass -File ops/evidence.ps1`
3. Create zip archive excluding bulky/transient folders:
- exclude: `frontend/node_modules`, `frontend/dist`, `backend/bin`, `backend/obj`, `evidence`
4. Attach docs outputs (report/slides pdf/doc if exported).
