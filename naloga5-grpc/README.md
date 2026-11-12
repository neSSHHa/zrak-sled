# Naloga 5: gRPC

Prenos podatkov o kakovosti zraka med gRPC strežnikom in spletno aplikacijo v C# (ASP.NET Core, .NET 8).

- **Streznik** – gRPC strežnik (`Grpc.AspNetCore`), ki ob vsakem klicu prebere zadnje urne meritve z ARSO (XML).
- **Odjemalec** – spletna aplikacija, ki prek generiranega nastavka (`ZrakStoritev.ZrakStoritevClient`) kliče strežnik in podatke prikaže na strani.
- **Protos/zrak.proto** – skupna definicija storitve in sporočil. Iz nje `Grpc.Tools` ob gradnji generira kodo za oba projekta.

## Definicija (zrak.proto)

Storitev `ZrakStoritev` s 4 RPC klici:

| RPC | Tip | Zahtevek → odgovor |
|---|---|---|
| `SeznamPostaj` | preprost | `PrazenZahtevek` → `PostajeOdgovor` |
| `MeritevPostaje` | preprost | `PostajaZahtevek` → `MeritevOdgovor` |
| `NajslabsePostaje` | preprost | `NajslabseZahtevek` → `MeritveOdgovor` |
| `SpremljajPostajo` | server-side streaming | `SpremljanjeZahtevek` → `stream Meritev` |

Podatkovni tip je sporočilo `Meritev` (postaja, čas, PM10, PM2,5, NO2, O3, čas branja). Vrednost `-1` pomeni, da postaja tega ne meri.

## Kako deluje

1. Ob odprtju strani odjemalec pokliče `SeznamPostaj` in napolni dropdown.
2. Uporabnik izbere postajo → `MeritevPostaje` vrne zadnjo meritev.
3. Uporabnik izbere onesnaževalo in število → `NajslabsePostaje` vrne postaje z najvišjimi vrednostmi.
4. **Spremljanje v živo**: `SpremljajPostajo` je tok – strežnik na vsakih N sekund znova prebere ARSO in pošlje meritev. Odjemalec vsako sporočilo iz toka takoj posreduje brskalniku (Server-Sent Events), tako da se tabela posodablja v realnem času. Ko uporabnik klikne *Ustavi*, se klic prekine in strežnik neha pošiljati.

Brskalnik ne more neposredno klicati gRPC (potrebuje HTTP/2 trailerje), zato je gRPC odjemalec ASP.NET backend spletne aplikacije.

## Zagon

V dveh terminalih:

```powershell
cd naloga5-grpc/Streznik
dotnet run
```

```powershell
cd naloga5-grpc/Odjemalec
dotnet run
```

Strežnik posluša na `http://localhost:5001` (HTTP/2), aplikacija na `http://localhost:5000`.
