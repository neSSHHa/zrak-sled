using System.Globalization;

namespace ZrakSled.XmlJson;

/// <summary>Pogoji, ki jih določi uporabnik. Prazen pogoj pomeni "brez omejitve".</summary>
public class Filter
{
    public string? Mesto { get; set; }
    public string? UporabnikId { get; set; }
    public DateOnly? Od { get; set; }
    public DateOnly? Do { get; set; }
    public double? Pm10Nad { get; set; }
    public bool SamoNadMejo { get; set; }

    public IEnumerable<ZapisIzpostavljenosti> Uporabi(IEnumerable<ZapisIzpostavljenosti> zapisi)
    {
        var q = zapisi;

        if (!string.IsNullOrWhiteSpace(Mesto))
            q = q.Where(z => z.Postaja.Mesto.Contains(Mesto, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(UporabnikId))
            q = q.Where(z => z.Uporabnik.Id.Equals(UporabnikId, StringComparison.OrdinalIgnoreCase));

        if (Od is DateOnly od)
            q = q.Where(z => z.Lokacija.Datum >= od);

        if (Do is DateOnly @do)
            q = q.Where(z => z.Lokacija.Datum <= @do);

        if (Pm10Nad is double meja)
            q = q.Where(z => z.Meritev?.Pm10 is double pm10 && pm10 > meja);

        if (SamoNadMejo)
            q = q.Where(z => z.NadMejo);

        return q;
    }

    /// <summary>Kratek opis filtra, ki ga zapišemo tudi v izhodni datoteki.</summary>
    public Dictionary<string, string> Opis()
    {
        var opis = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(Mesto)) opis["mesto"] = Mesto;
        if (!string.IsNullOrWhiteSpace(UporabnikId)) opis["uporabnik"] = UporabnikId;
        if (Od is DateOnly od) opis["od"] = od.ToString("yyyy-MM-dd");
        if (Do is DateOnly @do) opis["do"] = @do.ToString("yyyy-MM-dd");
        if (Pm10Nad is double p) opis["pm10Nad"] = p.ToString(CultureInfo.InvariantCulture);
        if (SamoNadMejo) opis["samoNadMejo"] = "true";
        return opis;
    }

    public static Filter IzArgumentov(string[] args, out string mapaPodatkov, out string mapaIzhoda)
    {
        var f = new Filter();
        mapaPodatkov = "data";
        mapaIzhoda = "izhod";

        for (int i = 0; i < args.Length; i++)
        {
            string Naslednji() => i + 1 < args.Length
                ? args[++i]
                : throw new ArgumentException($"Manjka vrednost za {args[i]}");

            switch (args[i].ToLowerInvariant())
            {
                case "--mesto": f.Mesto = Naslednji(); break;
                case "--uporabnik": f.UporabnikId = Naslednji(); break;
                case "--od": f.Od = PreberiDatum(Naslednji()); break;
                case "--do": f.Do = PreberiDatum(Naslednji()); break;
                case "--pm10-nad": f.Pm10Nad = PreberiStevilo(Naslednji()); break;
                case "--nad-mejo": f.SamoNadMejo = true; break;
                case "--data": mapaPodatkov = Naslednji(); break;
                case "--izhod": mapaIzhoda = Naslednji(); break;
                default: throw new ArgumentException($"Neznan argument: {args[i]}");
            }
        }

        return f;
    }

    public static Filter Interaktivno()
    {
        Console.WriteLine("Nastavi filter (Enter = brez omejitve):");
        var f = new Filter
        {
            Mesto = Vprasaj("  Mesto postaje (npr. Maribor): "),
            UporabnikId = Vprasaj("  ID uporabnika (npr. U02): "),
        };

        var od = Vprasaj("  Od datuma (YYYY-MM-DD): ");
        if (od is not null) f.Od = PreberiDatum(od);

        var @do = Vprasaj("  Do datuma (YYYY-MM-DD): ");
        if (@do is not null) f.Do = PreberiDatum(@do);

        var pm = Vprasaj("  Samo PM10 nad vrednostjo (µg/m³): ");
        if (pm is not null) f.Pm10Nad = PreberiStevilo(pm);

        var nad = Vprasaj("  Samo zapisi nad mejo uporabnika? (d/n): ");
        f.SamoNadMejo = nad is not null && nad.StartsWith("d", StringComparison.OrdinalIgnoreCase);

        Console.WriteLine();
        return f;
    }

    private static string? Vprasaj(string vprasanje)
    {
        Console.Write(vprasanje);
        var odgovor = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(odgovor) ? null : odgovor;
    }

    private static DateOnly PreberiDatum(string tekst) =>
        DateOnly.TryParseExact(tekst, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : throw new ArgumentException($"Datum mora biti v obliki YYYY-MM-DD, dobil sem: {tekst}");

    private static double PreberiStevilo(string tekst) =>
        double.TryParse(tekst.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
            ? v
            : throw new ArgumentException($"Pričakoval sem število, dobil sem: {tekst}");
}
