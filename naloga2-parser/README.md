# Naloga 2: Parser

Razčlenjevalnik v C# (.NET 8) s knjižnico [Puppeteer Sharp](https://www.puppeteersharp.com/). Prebere zadnje urne meritve kakovosti zraka po vseh ARSO merilnih postajah in jih shrani v `arso_zrak.json`. Te podatke potrebujem v projektu **zrak-sled**, da jih povežem z lokacijami uporabnika.

Stran: [Zadnje urne in dnevne ravni onesnaževal (ARSO)](https://www.arso.gov.si/zrak/kakovost%20zraka/podatki/dnevne_koncentracije.html)

## Kako deluje

1. Puppeteer odpre stran v Chromu brez okna.
2. Iz `p.datum` prebere datum in uro meritev.
3. V prvi tabeli `table.online` gre čez vse vrstice `tr` in iz celic `td` prebere ime postaje in vrednosti (PM10, PM2,5, SO2, CO, O3, NO2, NOx, benzen).
4. Vrstice z glavo (samo `th`) in vrstico "Mejna vrednost" preskoči.
5. Prazna celica ali `-` postane `null`, `<2` se shrani kot `2`.
6. Rezultat izpiše in shrani v `arso_zrak.json`.

Vrednosti so v µg/m³, CO v mg/m³.

## Zagon

```bash
cd naloga2-parser
dotnet run
```

Ob prvem zagonu Puppeteer prenese Chrome, zato traja malo dlje.

## Primer izhoda

```json
{
  "Vir": "https://www.arso.gov.si/zrak/kakovost%20zraka/podatki/dnevne_koncentracije.html",
  "Datum": "Petek, 23. oktobra 2026, med 17:00 in 18:00",
  "Meritve": [
    {
      "Postaja": "LJ Bežigrad",
      "Pm10": 16,
      "Pm25": 9,
      "So2": null,
      "Co": null,
      "O3": 11,
      "No2": 34,
      "Nox": 47,
      "Benzen": 0.5
    },
    ...
  ]
}
```

Celoten primer (24 postaj) je v [`arso_zrak.json`](arso_zrak.json).
