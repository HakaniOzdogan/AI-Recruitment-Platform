# Correlation Proof

Bu dokuman, UI hata referansi ile backend log eslestirmesini kanitlamak icin kullanilir.

## Senaryo: Duplicate Application ile 409
1. `Jobs -> Job Detail -> Applications` paneline git.
2. Ayni adayi ayni ilana ikinci kez eklemeyi dene.
3. UI toast mesajinda `Ref: <correlation-id>` degerini not al.

## Log eslestirme
- Linux/Mac:
```bash
docker compose logs api | grep "<correlation-id>"
```

- PowerShell:
```powershell
docker compose logs api | Select-String "<correlation-id>"
```

Beklenen:
- API loglarinda ayni correlation id ile ilgili 409 kaydi gorunur.

## Not
- Response header `x-correlation-id` yoksa UI request id'sini fallback olarak kullanir.
- Kanit icin screenshot onerisi: `10-correlation-toast.png`
