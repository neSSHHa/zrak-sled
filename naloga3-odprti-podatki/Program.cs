using System.Text;
using System.Text.Json;

// SURS .px datoteke so v kodni tabeli windows-1250, ki je .NET privzeto ne pozna
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// SURS, tabela 2719901S: NAMEA emisije v zrak (SKD 2008), Slovenija, letno
const string PX_API = "https://pxweb.stat.si/SiStatData/api/v1/sl/Data/2719901S.px";

// viri emisij: skupaj, dejavnosti A–U in gospodinjstva
string[] viri = ["TOT", "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U",
    "Gospodinjstva - SKUPAJ", "Gospodinjstva - promet", "Gospodinjstva - ogrevanje/hlajenje", "Gospodinjstva - drugo"];

var app = WebApplication.Create(args);
app.UseDefaultFiles();   
app.UseStaticFiles();

app.MapGet("/api/emisije", async (string? onesnazevalo) =>
{
    onesnazevalo ??= "PM10";

    // 1. Ob vsakem klicu prenesemo svežo .px datoteko s SURS
    var poizvedba = new
    {
        query = new object[]
        {
            new { code = "VIR EMISIJ", selection = new { filter = "item", values = viri } },
            new { code = "LETO",       selection = new { filter = "all",  values = new[] { "*" } } },
            new { code = "MERITVE",    selection = new { filter = "item", values = new[] { onesnazevalo } } }
        },
        response = new { format = "px" }
    };
    using var http = new HttpClient();
    var odgovor = await http.PostAsync(PX_API, new StringContent(JsonSerializer.Serialize(poizvedba), Encoding.UTF8, "application/json"));
    odgovor.EnsureSuccessStatusCode();
    var px = Encoding.GetEncoding(1250).GetString(await odgovor.Content.ReadAsByteArrayAsync());

    // 2. Razčlenjevanje
    var (vrednosti, podatki) = PxParser.Preberi(px);

    // 3. Vsaki številki pripišemo vir in leto.
    //    Številke so po vrsti: vir se spreminja počasneje, leto hitreje
    //    (onesnaževalo je samo eno, zato zanke zanj ne rabimo).
    var emisije = new List<Emisija>();
    int i = 0;
    foreach (var vir in vrednosti["VIR EMISIJ"])
        foreach (var leto in vrednosti["LETO"])
            emisije.Add(new Emisija(vir, leto, podatki[i++] ?? 0));

    // 4. Obdelava
    string[] skupaj = ["SKD dejavnost - SKUPAJ", "Gospodinjstva - SKUPAJ"];

    // agregacija: skupni izpusti po letih = vse dejavnosti + vsa gospodinjstva
    var poLetih = emisije
        .Where(e => skupaj.Contains(e.Vir))
        .GroupBy(e => e.Leto)
        .Select(g => new { leto = g.Key, tone = g.Sum(e => e.Tone) });

    // filtriranje: 8 največjih virov v zadnjem letu
    var zadnjeLeto = vrednosti["LETO"].Last();
    var najvecjiViri = emisije
        .Where(e => e.Leto == zadnjeLeto && !skupaj.Contains(e.Vir))
        .OrderByDescending(e => e.Tone)
        .Take(8)
        .Select(e => new { vir = e.Vir, tone = e.Tone });

    return Results.Ok(new { onesnazevalo, zadnjeLeto, poLetih, najvecjiViri });
});

app.Run();

// Ena vrednost iz tabele: koliko ton je vir izpustil v enem letu
record Emisija(string Vir, string Leto, double Tone);
