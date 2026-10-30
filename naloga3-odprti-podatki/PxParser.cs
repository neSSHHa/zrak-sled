using System.Globalization;
using System.Text.RegularExpressions;

// Knjižnico pa nisem uporabljao ker ni tak potrebna za 25 lniije kode.. 

// Preprost razčlenjevalnik za PC-Axis (.px) datoteke.
// Iz datoteke potrebujemo samo dve stvari:
//   VALUES("dimenzija")="a","b","c";   -> seznam vrednosti za vsako dimenzijo
//   DATA= 1.5 2.3 ".." 4.1 ...;        -> vse številke po vrsti
static class PxParser
{
    public static (Dictionary<string, List<string>> Vrednosti, List<double?> Podatki) Preberi(string px)
    {
        var vrednosti = new Dictionary<string, List<string>>();
        foreach (Match m in Regex.Matches(px, @"VALUES\(""([^""]+)""\)=((?:\s*""[^""]*"",?)+);"))
            vrednosti[m.Groups[1].Value] = Regex.Matches(m.Groups[2].Value, @"""([^""]*)""")
                .Select(x => x.Groups[1].Value).ToList();

        // manjkajoči podatki so zapisani kot ".." ali "...", te shranimo kot null
        var podatki = px[(px.IndexOf("DATA=") + 5)..]
            .Split(new[] { ' ', '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => double.TryParse(s, CultureInfo.InvariantCulture, out var x) ? x : (double?)null)
            .ToList();

        return (vrednosti, podatki);
    }
}
