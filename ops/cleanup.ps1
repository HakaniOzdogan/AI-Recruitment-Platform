param(
    [string]$ComposeFile = "docker-compose.prod.yml",
    [switch]$WithVolumes
)

$ErrorActionPreference = "Stop"

Write-Host "[1/1] Stopping containers"
docker compose -f $ComposeFile down

if ($WithVolumes) {
    Write-Host "Removing volumes as requested"
    docker compose -f $ComposeFile down -v
}
else {
    Write-Host "Tip: use -WithVolumes for full reset."
}

Write-Host "Cleanup done."
