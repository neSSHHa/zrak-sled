# Naloga 1: XML in JSON (zrak-sled)

Konzolna aplikacija v C# (.NET 8), ki prebere XML podatke projekta **zrak-sled**, jih poveže prek ID-jev, filtrira po pogojih uporabnika in rezultat izvozi v `filtrirano.json` in `filtrirano.xml`.

Podatki so vzeti iz teme projekta: ARSO merilna mesta, urne meritve kakovosti zraka in točke poti uporabnikov, ki lokacijo delijo prek Telegrama. Program za vsako točko poti ugotovi, kakšen zrak je uporabnik dihal (meritev najbližje postaje ob isti uri), in označi, ali je bil PM10 nad njegovo mejo za opozorila.

## Struktura

```
naloga1-xml-json/
├── data/
│   ├── postaje.xml       12 ARSO merilnih mest
│   ├── uporabniki.xml    10 uporabnikov (Telegram)
│   ├── meritve.xml       15 urnih meritev (PM10, PM2,5, NO2, O3)
│   └── lokacije.xml      15 točk poti uporabnikov
├── src/ZrakSled.XmlJson/
│   ├── Program.cs        potek programa
│   ├── Modeli.cs         razredi za deserializacijo (XmlSerializer)
│   ├── BranjeXml.cs      branje XML datotek, preverjanje unikatnih ID-jev
│   ├── Povezovanje.cs    združevanje prek ID-jev
│   ├── Filter.cs         filtri (argumenti ali interaktivno)
│   └── Izvoz.cs          izpis tabele, izvoz v JSON in XML
└── izhod/
    ├── filtrirano.json   primer izhoda
    └── filtrirano.xml    primer izhoda
```

## Podatki in povezave

```
uporabniki.xml        lokacije.xml                  postaje.xml
 U01 ─────────────┐    L01                    ┌───── P01
                  └──► uporabnikId = U01      │
                       postajaId   = P01 ◄────┘
                       datum, ura ─────────┐
                                           ▼
                                      meritve.xml
                                       M01  postajaId = P01, isti datum in ura
```

| Datoteka | ID | Sklici | Datum | Števila | Besedilo |
|---|---|---|---|---|---|
| `postaje.xml` | `id="P01"` | – | `datumZacetkaMeritev` | `lat`, `lon`, `nadmorskaVisina` | `ime`, `mesto`, `tip`, `opomba` |
| `uporabniki.xml` | `id="U01"` | – | `datumRegistracije` | `mejaPm10` | `ime`, `telefon`, `telegram`, `mesto` |
| `meritve.xml` | `id="M01"` | `postajaId` | `datum` | `ura`, `pm10`, `pm25`, `o3`, `no2` | – |
| `lokacije.xml` | `id="L01"` | `uporabnikId`, `postajaId` | `datum` | `ura`, `lat`, `lon` | – |

Vsi datumi so v obliki `YYYY-MM-DD`.

**Prazne in manjkajoče vrednosti** (namenoma):

- prazni elementi: `<opomba></opomba>` pri več postajah, `<o3></o3>` pri M02, M09, M14, `<no2></no2>` pri M07, `<telegram></telegram>` pri U06
- izpuščena polja: U03 nima `<telegram>`, U08 nima `<mejaPm10>` (program uporabi privzeto mejo 50 µg/m³), P12 nima `<nadmorskaVisina>` in `<opomba>`
- manjkajoča meritev: za lokacijo L15 postaja P12 ob 13:00 nima meritve, zato je zapis brez vrednosti zraka (`"meritev": null` v JSON, `<meritev />` v XML)

## Kaj program naredi

1. **Branje XML**: vse štiri datoteke deserializira z `XmlSerializer` v razrede `Postaja`, `Uporabnik`, `Meritev`, `Lokacija`. Prazne vrednosti postanejo `null`.
2. **Povezovanje**: vsako lokacijo poveže z uporabnikom (`uporabnikId`), postajo (`postajaId`) in meritvijo te postaje ob istem datumu in uri. Izračuna še oddaljenost od postaje (Haversinova formula). Neveljavni sklici se izpišejo kot opozorilo.
3. **Filtriranje**: po mestu postaje, uporabniku, datumu od/do, vrednosti PM10 in preseganju meje uporabnika. Pogoje se da kombinirati.
4. **Izpis** v konzolo kot tabela.
5. **Izvoz** v `filtrirano.json` (objekt z opisom filtra in seznamom zapisov).
6. **Izvoz** v nov `filtrirano.xml` z istimi zapisi.

## Zagon

Potreben je [.NET 8 SDK](https://dotnet.microsoft.com/download). Ukaze poženi v mapi `naloga1-xml-json`.

Interaktivno (program vpraša za filter, Enter pomeni brez omejitve):

```bash
dotnet run --project src/ZrakSled.XmlJson
```

Z argumenti:

```bash
# zapisi od 13. 10. 2026 naprej (tako je nastal primer v mapi izhod/)
dotnet run --project src/ZrakSled.XmlJson -- --od 2026-10-13

# samo postaje v Mariboru
dotnet run --project src/ZrakSled.XmlJson -- --mesto Maribor

# vse, kjer je PM10 presegel mejo uporabnika
dotnet run --project src/ZrakSled.XmlJson -- --nad-mejo

# uporabnik U02 med 12. in 13. 10., PM10 nad 45
dotnet run --project src/ZrakSled.XmlJson -- --uporabnik U02 --od 2026-10-12 --do 2026-10-13 --pm10-nad 45
```

| Argument | Pomen |
|---|---|
| `--mesto <ime>` | mesto postaje (ni občutljivo na velike črke, zadošča del imena) |
| `--uporabnik <id>` | ID uporabnika, npr. `U02` |
| `--od <YYYY-MM-DD>` | datum od (vključno) |
| `--do <YYYY-MM-DD>` | datum do (vključno) |
| `--pm10-nad <število>` | samo zapisi, kjer je PM10 večji od vrednosti |
| `--nad-mejo` | samo zapisi, kjer je PM10 nad mejo uporabnika |
| `--data <mapa>` | mapa z XML datotekami (privzeto `data`) |
| `--izhod <mapa>` | mapa za izhod (privzeto `izhod`) |

## Primer izhoda

```
$ dotnet run --project src/ZrakSled.XmlJson -- --od 2026-10-13
Prebrano: 12 postaj, 10 uporabnikov, 15 meritev, 15 lokacij.
Opozorilo: Lokacija L15: postaja P12 nima meritve za 2026-10-14 ob 13:00.

Filter: od=2026-10-13
Najdenih zapisov: 11 od 15

+------+------------+-------+-------------------+-----------------------+-----------------+-----+------+-------+-----+----+------+------------+
| Lok. | Datum      | Ura   | Uporabnik         | Postaja               | Mesto           | km  | PM10 | PM2.5 | NO2 | O3 | Meja | Stanje     |
+------+------------+-------+-------------------+-----------------------+-----------------+-----+------+-------+-----+----+------+------------+
| L07  | 2026-10-13 | 08:00 | U05 Eva Vidmar    | Murska Sobota Rakičan | Murska Sobota   | 2.3 | 54   | 39    | -   | 20 | 45   | NAD MEJO   |
| L05  | 2026-10-13 | 09:00 | U03 Maja Horvat   | Celje bolnica         | Celje           | 0.9 | 72   | 55    | 39  | 10 | 50   | NAD MEJO   |
| L06  | 2026-10-13 | 12:00 | U04 Jan Zupan     | Koper                 | Koper           | 0.1 | 18   | 9     | 14  | 71 | 50   | v redu     |
| L08  | 2026-10-13 | 15:00 | U06 Nik Golob     | Nova Gorica           | Nova Gorica     | 0.1 | 22   | 14    | 19  | 64 | 50   | v redu     |
| L09  | 2026-10-14 | 07:00 | U07 Sara Kos      | Kranj                 | Kranj           | 0.6 | 33   | 24    | 36  | -  | 50   | v redu     |
| L13  | 2026-10-14 | 08:00 | U02 Luka Kranjc   | Maribor center        | Maribor         | 0.2 | 49   | 37    | 62  | 11 | 40   | NAD MEJO   |
| L10  | 2026-10-14 | 08:00 | U08 Matej Mlakar  | Trbovlje              | Trbovlje        | 0.6 | 81   | 63    | 33  | 5  | 50   | NAD MEJO   |
| L11  | 2026-10-14 | 10:00 | U09 Tina Potočnik | Novo mesto            | Novo mesto      | 0.2 | 44   | 31    | 25  | 27 | 50   | v redu     |
| L15  | 2026-10-14 | 13:00 | U01 Ana Novak     | Iskrba                | Kočevska Reka   | 0.5 | -    | -     | -   | -  | 50   | ni podatka |
| L14  | 2026-10-14 | 17:00 | U03 Maja Horvat   | Celje bolnica         | Celje           | 0.4 | 66   | 50    | 44  | -  | 50   | NAD MEJO   |
| L12  | 2026-10-14 | 18:00 | U10 Rok Kovačič   | Zagorje               | Zagorje ob Savi | 0.1 | 58   | 46    | 21  | 9  | 50   | NAD MEJO   |
+------+------------+-------+-------------------+-----------------------+-----------------+-----+------+-------+-----+----+------+------------+

Shranjeno: izhod/filtrirano.json
Shranjeno: izhod/filtrirano.xml
```

Odlomek iz `izhod/filtrirano.json`:

```json
{
  "opis": "Izpostavljenost uporabnikov onesnaženemu zraku (zrak-sled)",
  "enota": "µg/m³",
  "filter": { "od": "2026-10-13" },
  "steviloZapisov": 11,
  "zapisi": [
    {
      "lokacijaId": "L07",
      "datum": "2026-10-13",
      "ura": 8,
      "lat": 46.6625,
      "lon": 16.1664,
      "uporabnik": { "id": "U05", "ime": "Eva Vidmar", "telegram": "@evav", "mejaPm10": 45 },
      "postaja": { "id": "P05", "ime": "Murska Sobota Rakičan", "mesto": "Murska Sobota", "oddaljenostKm": 2.28 },
      "meritev": { "id": "M07", "pm10": 54, "pm25": 39, "no2": null, "o3": 20 },
      "nadMejo": true,
      "stanje": "NAD MEJO"
    }
  ]
}
```

Odlomek iz `izhod/filtrirano.xml`:

```xml
<izpostavljenost steviloZapisov="11" enota="µg/m³">
  <filter>
    <od>2026-10-13</od>
  </filter>
  <zapis lokacijaId="L07">
    <datum>2026-10-13</datum>
    <ura>8</ura>
    <lat>46.6625</lat>
    <lon>16.1664</lon>
    <uporabnik id="U05">
      <ime>Eva Vidmar</ime>
      <mejaPm10>45</mejaPm10>
    </uporabnik>
    <postaja id="P05">
      <ime>Murska Sobota Rakičan</ime>
      <mesto>Murska Sobota</mesto>
      <oddaljenostKm>2.28</oddaljenostKm>
    </postaja>
    <meritev id="M07">
      <pm10>54</pm10>
      <pm25>39</pm25>
      <no2></no2>
      <o3>20</o3>
    </meritev>
    <nadMejo>true</nadMejo>
    <stanje>NAD MEJO</stanje>
  </zapis>
  ...
</izpostavljenost>
```

## Opombe

- Podatki so izmišljeni, vendar oblikovani po ARSO podatkih (imena postaj, onesnaževala, enote µg/m³). Telefonske številke in imena niso resnični.
- Meja PM10 50 µg/m³ je dnevna mejna vrednost za PM10; uporabnik si lahko v projektu nastavi strožjo.
