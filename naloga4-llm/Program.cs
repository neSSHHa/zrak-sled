using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

// ARSO: zadnje urne meritve onesnaževal po vseh merilnih postajah (XML)
const string ARSO_XML = "https://www.arso.gov.si/xml/zrak/ones_zrak_urni_podatki_zadnji.xml";
const string GEMINI_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

// oblika JSON, ki jo zahtevamo od modela
const string OBLIKA_JSON = """
    {
      "priporoceno": true ali false,
      "tveganje": "nizko" ali "srednje" ali "visoko",
      "razlog": "kratka razlaga v 1-2 stavkih",
      "boljsiCas": "kdaj bi bilo bolje, ali null če je že zdaj v redu",
      "nasveti": ["nasvet 1", "nasvet 2", "nasvet 3"]
    }
    """;

var app = WebApplication.Create(args);
app.UseDefaultFiles();
app.UseStaticFiles();

var http = new HttpClient();

// Seznam postaj za dropdown na strani
app.MapGet("/api/postaje", async () =>
{
    var meritve = await PreberiArso();
    return meritve.Select(m => m.Postaja).Distinct();
});

// Nasvet za aktivnost: /api/nasvet?postaja=MB Titova&aktivnost=tek&ura=17:30
app.MapGet("/api/nasvet", async (string postaja, string aktivnost, string ura) =>
{
    // 1. Sveže meritve z ARSO za izbrano postajo
    var meritve = await PreberiArso();
    var meritev = meritve.FirstOrDefault(m => m.Postaja == postaja);
    if (meritev == null)
        return Results.NotFound("Postaja ne obstaja.");

    // 2. Prompt sestavimo iz vnosa uporabnika in meritev
    string prompt = SestaviPrompt(meritev, aktivnost, ura);

    // 3. Pošljemo prompt modelu
    string odgovor;
    try
    {
        odgovor = await VprasajGemini(prompt);
    }
    catch (Exception napaka)
    {
        return Results.Problem("Napaka pri klicu Gemini: " + napaka.Message);
    }

    // 4. JSON odgovor preberemo v razred Nasvet
    Nasvet? nasvet;
    try
    {
        var moznosti = new JsonSerializerOptions(JsonSerializerDefaults.Web); // camelCase, ne loči velikih črk
        nasvet = JsonSerializer.Deserialize<Nasvet>(odgovor, moznosti);
    }
    catch (JsonException)
    {
        return Results.Problem("Model ni vrnil pravilnega JSON: " + odgovor);
    }

    // prompt vrnemo zraven, da se na strani vidi, kaj smo poslali
    return Results.Ok(new { meritev, prompt, nasvet });
});

app.Run();


// Prenese XML z ARSO in iz vsake <postaja> prebere ime in vrednosti
async Task<List<Meritev>> PreberiArso()
{
    string xml = await http.GetStringAsync(ARSO_XML);
    var dokument = XDocument.Parse(xml);

    var meritve = new List<Meritev>();
    foreach (var postaja in dokument.Descendants("postaja"))
    {
        meritve.Add(new Meritev(
            postaja.Element("merilno_mesto")?.Value ?? "",
            postaja.Element("datum_do")?.Value ?? "",
            Stevilo(postaja.Element("pm10")),
            Stevilo(postaja.Element("pm2.5")),
            Stevilo(postaja.Element("no2")),
            Stevilo(postaja.Element("o3"))));
    }
    return meritve;
}

// Element manjka ali je prazen = ni meritve. "<2" shranim kot 2.
double? Stevilo(XElement? element)
{
    if (element == null)
        return null;

    string besedilo = element.Value.Trim().Replace("<", "");
    if (double.TryParse(besedilo, NumberStyles.Float, CultureInfo.InvariantCulture, out var vrednost))
        return vrednost;
    return null;
}

string Izpis(double? vrednost)
{
    return vrednost == null ? "ni podatka" : vrednost + " µg/m³";
}

// Dinamičen prompt: postaja, aktivnost in ura pridejo od uporabnika, vrednosti z ARSO
string SestaviPrompt(Meritev m, string aktivnost, string ura)
{
    return $"""
        Uporabnik je v bližini merilne postaje {m.Postaja} in ob {ura} načrtuje aktivnost na prostem: {aktivnost}.
        Zadnje urne meritve kakovosti zraka z ARSO (do {m.Cas}):
        - PM10: {Izpis(m.Pm10)}
        - PM2,5: {Izpis(m.Pm25)}
        - NO2: {Izpis(m.No2)}
        - O3: {Izpis(m.O3)}
        Za primerjavo: dnevna mejna vrednost za PM10 je 50 µg/m³, urna mejna vrednost za NO2 je 200 µg/m³.

        Oceni, ali je ta aktivnost ob tej uri priporočljiva glede na kakovost zraka.
        Odgovori v slovenščini in vrni samo JSON v tej obliki:
        {OBLIKA_JSON}
        """;
}

// Pošlje prompt na Gemini API in vrne besedilo odgovora (to je naš JSON)
async Task<string> VprasajGemini(string prompt)
{
    string? kljuc = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
    if (string.IsNullOrEmpty(kljuc))
        throw new Exception("Manjka okoljska spremenljivka GEMINI_API_KEY.");

    var telo = new
    {
        contents = new[] { new { parts = new[] { new { text = prompt } } } },
        // s tem Gemini vrne čist JSON namesto navadnega besedila
        generationConfig = new { responseMimeType = "application/json" }
    };

    var zahteva = new HttpRequestMessage(HttpMethod.Post, GEMINI_URL);
    zahteva.Headers.Add("x-goog-api-key", kljuc);
    zahteva.Content = new StringContent(JsonSerializer.Serialize(telo), Encoding.UTF8, "application/json");

    var odgovor = await http.SendAsync(zahteva);
    odgovor.EnsureSuccessStatusCode();

    // Odgovor Gemini: candidates[0].content.parts[0].text
    using var json = JsonDocument.Parse(await odgovor.Content.ReadAsStringAsync());
    return json.RootElement
        .GetProperty("candidates")[0]
        .GetProperty("content")
        .GetProperty("parts")[0]
        .GetProperty("text")
        .GetString() ?? "";
}

// Ena postaja z ARSO, vrednosti v µg/m³
record Meritev(string Postaja, string Cas, double? Pm10, double? Pm25, double? No2, double? O3);
