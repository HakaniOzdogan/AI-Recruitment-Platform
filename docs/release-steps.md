# Release Steps (Manual)

Not: Tag islemini sadece `docs/acceptance-signoff.md` tum kritik maddeler PASS olduktan sonra yapin.

## 1) Pre-check
1. `git status`
2. `git checkout main`
3. `git pull`
4. `cat VERSION` (beklenen: `1.0.0`)

## 2) Docs/metadata commit
1. `git add VERSION CHANGELOG.md docs/acceptance-signoff.md docs/risk-register.md docs/freeze.md docs/release-steps.md`
2. `git commit -m "docs: finalize freeze and signoff for v1.0.0"`
3. `git rev-parse --short HEAD` cikisini `docs/freeze.md` icindeki release commit alanina yaz.
4. Eger `HEAD` hatasi aliyorsan (`Needed a single revision`), bu ortamda commit hash'i `N/A (no git history in this environment)` olarak belgeleyin ve `submission/MANIFEST.txt` referansi ile devam edin.

## 3) Tag
1. Signoff kontrolu: `docs/acceptance-signoff.md` kritik satirlar PASS olmali.
2. `git tag -a v1.0.0 -m "Release 1.0.0"`
3. `git push origin main`
4. `git push origin v1.0.0`

## 4) Post-tag quick verify
1. `git tag --list | grep v1.0.0` (PowerShell: `git tag --list | Select-String v1.0.0`)
2. `ops/run-demo` veya `ops/smoke-test` ile hizli kontrol
3. `ops/build-submission` ile teslim zip + manifest uret
