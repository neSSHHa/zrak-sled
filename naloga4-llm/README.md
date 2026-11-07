# Naloga 4: Uporaba LLM

Spletna aplikacija v C# (ASP.NET Core, .NET 8), ki z modelom Google Gemini svetuje, ali je izbrana aktivnost na prostem primerna glede na trenutno kakovost zraka.

V projektu **zrak-sled** Gemini že razlaga statistiko uporabnika. Ta naloga doda še nasvet pred aktivnostjo: npr. ali naj grem zdaj teči ali raje zjutraj.

## Kako deluje

1. **Vnos** – uporabnik izbere merilno postajo (seznam se naloži z ARSO), aktivnost (dropdown) in uro (privzeto trenutni čas iz brskalnika).
2. **Meritve** – backend prenese XML z zadnjimi urnimi meritvami z ARSO in vzame vrednosti PM10, PM2,5, NO2 in O3 za izbrano postajo.
3. **Dinamičen prompt** – `SestaviPrompt()` sestavi vprašanje iz vnosa in meritev, npr.:
   > Uporabnik je v bližini merilne postaje MB Titova in ob 17:30 načrtuje aktivnost na prostem: tek. Zadnje urne meritve: PM10: 48 µg/m³ ... Oceni, ali je ta aktivnost priporočljiva ... vrni samo JSON v tej obliki: ...
4. **Klic Gemini** – prompt se pošlje na Gemini API z `responseMimeType: "application/json"`, zato model vrne čist JSON.
5. **JSON → razred** – odgovor se deserializira v razred `Nasvet` (`Nasvet.cs`). Če JSON ni pravilen, API vrne napako.
6. **Prikaz** – stran izpiše posamezna polja (priporočeno, tveganje, razlog, boljši čas, nasveti) in poslan prompt.

## Oblika JSON

```json
{
  "priporoceno": false,
  "tveganje": "srednje",
  "razlog": "PM10 je blizu dnevne mejne vrednosti.",
  "boljsiCas": "zjutraj pred 9. uro",
  "nasveti": ["Izberi pot stran od prometnih cest", "Zmanjšaj intenzivnost"]
}
```

```csharp
public class Nasvet
{
    public bool Priporoceno { get; set; }
    public string Tveganje { get; set; }
    public string Razlog { get; set; }
    public string? BoljsiCas { get; set; }
    public List<string> Nasveti { get; set; }
}
```

## API

- `GET /api/postaje` – seznam ARSO postaj
- `GET /api/nasvet?postaja=MB Titova&aktivnost=tek&ura=17:30` – meritve, poslan prompt in nasvet

## Zagon

API ključ dobiš v [Google AI Studio](https://aistudio.google.com/apikey).

```powershell
cd naloga4-llm
$env:GEMINI_API_KEY="tvoj_kljuc"
dotnet run
```

Odpri naslov, ki ga izpiše (npr. `http://localhost:5000`).
