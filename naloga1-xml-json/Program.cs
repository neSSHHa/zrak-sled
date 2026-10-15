using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml.Linq;

Console.OutputEncoding = Encoding.UTF8;
var ci = CultureInfo.InvariantCulture;

// ---------- 1. Branje XML datotek (deserializacija v objekte) ----------

// Prazen element (<o3></o3>) ali manjkajoč element postane null.
double? Stevilo(XElement e, string ime) =>
    double.TryParse(e.Element(ime)?.Value, NumberStyles.Float, ci, out var v) ? v : null;

string? Besedilo(XElement e, string ime) =>
    string.IsNullOrWhiteSpace(e.Element(ime)?.Value) ? null : e.Element(ime)!.Value;

var regije = XDocument.Load("data/regije.xml").Root!.Elements("regija")
    .Select(e => new Regija(
        (string)e.Attribute("id")!,
        (string)e.Element("ime")!,
        (string)e.Element("sedez")!,
        (int)e.Element("povrsinaKm2")!))
    .ToList();

var postaje = XDocument.Load("data/postaje.xml").Root!.Elements("postaja")
    .Select(e => new Postaja(
        (string)e.Attribute("id")!,
        (string)e.Element("regijaId")!,
        (string)e.Element("ime")!,
        (string)e.Element("mesto")!,
        (string)e.Element("tip")!,
        (int?)Stevilo(e, "nadmorskaVisina"),
        Besedilo(e, "opomba")))
    .ToList();

var meritve = XDocument.Load("data/meritve.xml").Root!.Elements("meritev")
    .Select(e => new Meritev(
        (string)e.Attribute("id")!,
        (string)e.Element("postajaId")!,
        DateOnly.ParseExact((string)e.Element("datum")!, "yyyy-MM-dd", ci),
        (int)e.Element("ura")!,
        Stevilo(e, "pm10"),
        Stevilo(e, "pm25"),
        Stevilo(e, "o3"),
        Stevilo(e, "no2")))
    .ToList();

Console.WriteLine($"Prebrano: {regije.Count} regij, {postaje.Count} postaj, {meritve.Count} meritev.\n");

// ---------- 2. Povezovanje prek ID-jev ----------
// meritev.postajaId -> postaja.id, postaja.regijaId -> regija.id

var zapisi = (
    from m in meritve
    join p in postaje on m.PostajaId equals p.Id
    join r in regije on p.RegijaId equals r.Id
    orderby m.Datum, m.Ura
    select new Zapis(m.Id, m.Datum.ToString("yyyy-MM-dd"), m.Ura, p.Ime, p.Mesto, r.Ime,
                     m.Pm10, m.Pm25, m.O3, m.No2)
).ToList();

// ---------- 3. Filtriranje (izbere uporabnik) ----------

Console.WriteLine("Izberi filter:");
Console.WriteLine("  1 - meritve, kjer je PM10 večji od vrednosti");
Console.WriteLine("  2 - meritve v izbranem mestu");
Console.WriteLine("  3 - meritve po izbranem datumu");
Console.WriteLine("  0 - brez filtra (vse)");
Console.Write("Izbira: ");
var izbira = Console.ReadLine()?.Trim();

List<Zapis> filtrirano;
switch (izbira)
{
    case "1":
        Console.Write("PM10 večji od (npr. 50): ");
        var meja = double.Parse(Console.ReadLine()!, ci);
        filtrirano = zapisi.Where(z => z.Pm10 > meja).ToList();
        break;
    case "2":
        Console.Write("Mesto (npr. Maribor): ");
        var mesto = Console.ReadLine()!.Trim();
        filtrirano = zapisi.Where(z => z.Mesto.Equals(mesto, StringComparison.OrdinalIgnoreCase)).ToList();
        break;
    case "3":
        Console.Write("Po datumu (YYYY-MM-DD): ");
        var od = DateOnly.ParseExact(Console.ReadLine()!.Trim(), "yyyy-MM-dd", ci);
        filtrirano = zapisi.Where(z => DateOnly.ParseExact(z.Datum, "yyyy-MM-dd", ci) > od).ToList();
        break;
    default:
        filtrirano = zapisi;
        break;
}

// ---------- 4. Izpis v konzolo (tabela) ----------

string V(double? x) => x?.ToString(ci) ?? "-";

Console.WriteLine($"\nNajdenih zapisov: {filtrirano.Count}\n");
Console.WriteLine($"{"ID",-4} {"Datum",-10} {"Ura",3}  {"Postaja",-22} {"Regija",-22} {"PM10",5} {"PM2.5",5} {"O3",5} {"NO2",5}");
Console.WriteLine(new string('-', 90));
foreach (var z in filtrirano)
    Console.WriteLine($"{z.Id,-4} {z.Datum,-10} {z.Ura,3}  {z.Postaja,-22} {z.Regija,-22} {V(z.Pm10),5} {V(z.Pm25),5} {V(z.O3),5} {V(z.No2),5}");

// ---------- 5. Izvoz v JSON ----------

var json = JsonSerializer.Serialize(filtrirano, new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // da ostanejo č, š, ž
});
File.WriteAllText("filtrirano.json", json);

// ---------- 6. Izvoz v nov XML ----------

new XDocument(
    new XElement("meritve",
        filtrirano.Select(z => new XElement("meritev",
            new XAttribute("id", z.Id),
            new XElement("datum", z.Datum),
            new XElement("ura", z.Ura),
            new XElement("postaja", z.Postaja),
            new XElement("mesto", z.Mesto),
            new XElement("regija", z.Regija),
            new XElement("pm10", V(z.Pm10).Replace("-", "")),
            new XElement("pm25", V(z.Pm25).Replace("-", "")),
            new XElement("o3", V(z.O3).Replace("-", "")),
            new XElement("no2", V(z.No2).Replace("-", ""))))))
    .Save("filtrirano.xml");

Console.WriteLine("\nShranjeno v filtrirano.json in filtrirano.xml");

// ---------- Razredi ----------

record Regija(string Id, string Ime, string Sedez, int PovrsinaKm2);
record Postaja(string Id, string RegijaId, string Ime, string Mesto, string Tip, int? NadmorskaVisina, string? Opomba);
record Meritev(string Id, string PostajaId, DateOnly Datum, int Ura, double? Pm10, double? Pm25, double? O3, double? No2);
record Zapis(string Id, string Datum, int Ura, string Postaja, string Mesto, string Regija,
             double? Pm10, double? Pm25, double? O3, double? No2);
