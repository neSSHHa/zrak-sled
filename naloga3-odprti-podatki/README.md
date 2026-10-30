# Naloga 3: Odprti podatki

Spletna aplikacija v C# (ASP.NET Core, .NET 8) in Chart.js. Ob vsakem nalaganju s Statističnega urada RS prenese PC-Axis (.px) datoteko z izpusti onesnaževal v zrak, jo razčleni, obdela in prikaže v dveh grafih.

V projektu **zrak-sled** uporabnik vidi, kakšen zrak je dihal (meritve z ARSO). Ta naloga pokaže, **od kod to onesnaženje prihaja**. Pri PM10 je na primer največji vir ogrevanje gospodinjstev.

## Podatki

- Vir: [SURS SiStat](https://pxweb.stat.si/SiStatData/pxweb/sl/Data/-/2719901S.px), tabela **2719901S – NAMEA emisije v zrak (SKD 2008), Slovenija, letno**
- Oblika: PC-Axis (.px)
- Dimenzije: vir emisij (dejavnosti A–U in gospodinjstva), leto (od 2008), onesnaževalo (PM10, PM2,5, NOx …)
- Enota: tone

## Kako deluje

1. **Prenos** – `GET /api/emisije?onesnazevalo=PM10` pošlje poizvedbo na PxWeb API in dobi svežo .px datoteko.
2. **Razčlenjevanje** – `PxParser.cs` iz datoteke prebere seznam vrednosti za vsako dimenzijo (`VALUES`) in vse številke iz `DATA`.
3. **Povezovanje** – številke so v `DATA` zapisane po vrsti (najprej vsa leta za prvi vir, nato za drugi …), zato z dvema zankama vsaki številki pripišemo vir in leto.
4. **Obdelava** – filtriranje po onesnaževalu, agregacija skupnih izpustov po letih (dejavnosti + gospodinjstva) in 8 največjih virov v zadnjem letu.
5. **Prikaz** – Chart.js nariše črtni graf po letih in stolpčni graf največjih virov. Ob menjavi onesnaževala se podatki znova prenesejo.

## Zagon

```bash
cd naloga3-odprti-podatki
dotnet run
```

Odpri naslov, ki ga izpiše (npr. `http://localhost:5000`).
