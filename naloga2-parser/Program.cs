using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using PuppeteerSharp;

Console.OutputEncoding = Encoding.UTF8;

const string STRAN = "https://www.arso.gov.si/zrak/kakovost%20zraka/podatki/dnevne_koncentracije.html";

// 1. Pridobivanje spletne strani
// Ob prvem zagonu Puppeteer prenese Chrome.
await new BrowserFetcher().DownloadAsync();
var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
var page = await browser.NewPageAsync();
await page.GoToAsync(STRAN);

// Datum in ura meritev, npr. "Petek, 23. oktobra 2026, med 17:00 in 18:00"
var datumElement = await page.QuerySelectorAsync("p.datum");
var datum = await datumElement.EvaluateFunctionAsync<string>("node => node.innerText");

// Prva tabela s classom "online" so povprečne urne ravni onesnaževal
var tabela = await page.QuerySelectorAsync("table.online");
var vrstice = await tabela.QuerySelectorAllAsync("tr");

// 2. Razčlenjevanje podatkov
var meritve = new List<Meritev>();

foreach (var vrstica in vrstice)
{
    var celice = await vrstica.QuerySelectorAllAsync("td");

    // vrstice z glavo imajo samo <th>, zato nimajo celic <td>
    if (celice.Length == 0)
        continue;

    var besedila = new List<string>();
    foreach (var celica in celice)
        besedila.Add(await celica.EvaluateFunctionAsync<string>("node => node.innerText"));

    // zadnja vrstica so mejne vrednosti, ne postaja
    if (besedila[0].StartsWith("Mejna"))
        continue;

    meritve.Add(new Meritev(
        besedila[0],
        Stevilo(besedila[1]),
        Stevilo(besedila[2]),
        Stevilo(besedila[3]),
        Stevilo(besedila[4]),
        Stevilo(besedila[5]),
        Stevilo(besedila[6]),
        Stevilo(besedila[7]),
        Stevilo(besedila[8])));
}

await browser.CloseAsync();

// 3. Izpis in shranjevanje v JSON
Console.WriteLine(datum);
Console.WriteLine($"Prebranih postaj: {meritve.Count}\n");
foreach (var m in meritve)
    Console.WriteLine($"{m.Postaja,-24} PM10: {m.Pm10?.ToString() ?? "-",4}  PM2.5: {m.Pm25?.ToString() ?? "-",4}  NO2: {m.No2?.ToString() ?? "-",4}");

var rezultat = new Rezultat(STRAN, datum, meritve);
var json = JsonSerializer.Serialize(rezultat, new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // da šumniki in µ ostanejo berljivi
});
File.WriteAllText("arso_zrak.json", json);
Console.WriteLine("\nShranjeno v arso_zrak.json");

// Prazna celica ali "-" pomeni, da meritve ni. "<2" pomeni pod mejo zaznave, shranim 2.
double? Stevilo(string besedilo)
{
    besedilo = besedilo.Trim().Replace("<", "");
    if (double.TryParse(besedilo, NumberStyles.Float, CultureInfo.InvariantCulture, out var vrednost))
        return vrednost;
    return null;
}

// vrednosti so v µg/m³, CO v mg/m³
record Meritev(string Postaja, double? Pm10, double? Pm25, double? So2, double? Co,
               double? O3, double? No2, double? Nox, double? Benzen);

record Rezultat(string Vir, string Datum, List<Meritev> Meritve);
