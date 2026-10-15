# Naloga 1: XML in JSON

Konzolna aplikacija v C# (.NET 8) za projekt **zrak-sled**. Prebere meritve kakovosti zraka iz XML datotek, jih poveže s postajami in regijami, filtrira po izbiri uporabnika ter rezultat shrani v `filtrirano.json` in `filtrirano.xml`.

## Datoteke

```
naloga1-xml-json/
├── data/
│   ├── regije.xml      12 statističnih regij Slovenije
│   ├── postaje.xml     12 ARSO merilnih postaj
│   └── meritve.xml     15 urnih meritev zraka
├── Program.cs          celotna aplikacija
├── Naloga1.csproj
├── filtrirano.json     primer izhoda
└── filtrirano.xml      primer izhoda
```

## Povezave med datotekami

```
meritve.xml                postaje.xml               regije.xml
<postajaId>P02 ───────►  <postaja id="P02">
                           <regijaId>R02 ───────►  <regija id="R02">
```

- vsaka entiteta ima unikaten atribut `id` (R01, P01, M01 …)
- datum: `<datum>2026-10-12</datum>` (YYYY-MM-DD)
- številska polja: `ura`, `pm10`, `pm25`, `o3`, `no2`, `povrsinaKm2`, `nadmorskaVisina`
- besedilna polja: `ime`, `mesto`, `tip`, `sedez`, `opomba`
- prazne vrednosti: `<o3></o3>`, `<no2></no2>`, `<opomba></opomba>`
- manjkajoče polje: postaja P12 nima `nadmorskaVisina` in `opomba`

## Kaj program naredi

1. Prebere vse tri XML datoteke in jih pretvori v objekte (`Regija`, `Postaja`, `Meritev`).
2. Poveže meritve s postajami (`postajaId`) in postaje z regijami (`regijaId`).
3. Uporabnik izbere filter:
   - `1` meritve, kjer je PM10 večji od vrednosti
   - `2` meritve v izbranem mestu
   - `3` meritve po izbranem datumu
   - `0` vse meritve
4. Rezultat izpiše v konzolo kot tabelo.
5. Shrani ga v `filtrirano.json`.
6. Shrani ga v `filtrirano.xml`.

## Zagon

V mapi `naloga1-xml-json`:

```bash
dotnet run
```

Primer (tako je nastal priloženi izhod):

```
Prebrano: 12 regij, 12 postaj, 15 meritev.

Izberi filter:
  1 - meritve, kjer je PM10 večji od vrednosti
  2 - meritve v izbranem mestu
  3 - meritve po izbranem datumu
  0 - brez filtra (vse)
Izbira: 1
PM10 večji od (npr. 50): 50

Najdenih zapisov: 7

ID   Datum      Ura  Postaja                Regija                  PM10 PM2.5    O3   NO2
------------------------------------------------------------------------------------------
M03  2026-10-12   7  Maribor center         Podravska                 61    44     8    58
M02  2026-10-12  17  Ljubljana Bežigrad     Osrednjeslovenska         56    41     -    52
M07  2026-10-13   8  Murska Sobota Rakičan  Pomurska                  54    39    20     -
M05  2026-10-13   9  Celje bolnica          Savinjska                 72    55    10    39
M10  2026-10-14   8  Trbovlje               Zasavska                  81    63     5    33
M14  2026-10-14  17  Celje bolnica          Savinjska                 66    50     -    44
M12  2026-10-14  18  Zagorje                Zasavska                  58    46     9    21

Shranjeno v filtrirano.json in filtrirano.xml
```

Drugi primeri: izbira `2` in `Maribor` vrne 3 meritve, izbira `3` in `2026-10-13` vrne meritve od 14. 10. naprej.

Vrednosti so v µg/m³, `-` pomeni, da meritve ni. Podatki so izmišljeni, vendar oblikovani po ARSO meritvah.
