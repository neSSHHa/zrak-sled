using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace ZrakSled.XmlJson;

public static class Izvoz
{
    // ---------- konzola ----------

    public static void IzpisiTabelo(IReadOnlyList<ZapisIzpostavljenosti> zapisi)
    {
        string[] glava = { "Lok.", "Datum", "Ura", "Uporabnik", "Postaja", "Mesto", "km", "PM10", "PM2.5", "NO2", "O3", "Meja", "Stanje" };

        var vrstice = zapisi.Select(z => new[]
        {
            z.Lokacija.Id,
            z.Lokacija.DatumTekst,
            $"{z.Lokacija.Ura:00}:00",
            $"{z.Uporabnik.Id} {z.Uporabnik.Ime}",
            z.Postaja.Ime,
            z.Postaja.Mesto,
            z.OddaljenostOdPostajeKm.ToString("0.0", CultureInfo.InvariantCulture),
            Vrednost(z.Meritev?.Pm10),
            Vrednost(z.Meritev?.Pm25),
            Vrednost(z.Meritev?.No2),
            Vrednost(z.Meritev?.O3),
            Vrednost(z.Uporabnik.MejaPm10),
            z.Stanje,
        }).ToList();

        var sirine = glava.Select((g, i) => Math.Max(g.Length, vrstice.Select(v => v[i].Length).DefaultIfEmpty(0).Max())).ToArray();

        string Vrstica(string[] celice) =>
            "| " + string.Join(" | ", celice.Select((c, i) => c.PadRight(sirine[i]))) + " |";
        string crta = "+" + string.Join("+", sirine.Select(s => new string('-', s + 2))) + "+";

        Console.WriteLine(crta);
        Console.WriteLine(Vrstica(glava));
        Console.WriteLine(crta);
        foreach (var v in vrstice) Console.WriteLine(Vrstica(v));
        Console.WriteLine(crta);
    }

    private static string Vrednost(double? v) =>
        v is double d ? d.ToString("0.#", CultureInfo.InvariantCulture) : "-";

    // ---------- JSON ----------

    public static void ShraniJson(string pot, IReadOnlyList<ZapisIzpostavljenosti> zapisi, Filter filter)
    {
        var dokument = new
        {
            opis = "Izpostavljenost uporabnikov onesnaženemu zraku (zrak-sled)",
            enota = "µg/m³",
            filter = filter.Opis(),
            steviloZapisov = zapisi.Count,
            zapisi = zapisi.Select(z => new
            {
                lokacijaId = z.Lokacija.Id,
                datum = z.Lokacija.DatumTekst,
                ura = z.Lokacija.Ura,
                lat = z.Lokacija.Lat,
                lon = z.Lokacija.Lon,
                uporabnik = new
                {
                    id = z.Uporabnik.Id,
                    ime = z.Uporabnik.Ime,
                    telegram = string.IsNullOrWhiteSpace(z.Uporabnik.Telegram) ? null : z.Uporabnik.Telegram,
                    mejaPm10 = z.Uporabnik.MejaPm10,
                },
                postaja = new
                {
                    id = z.Postaja.Id,
                    ime = z.Postaja.Ime,
                    mesto = z.Postaja.Mesto,
                    oddaljenostKm = z.OddaljenostOdPostajeKm,
                },
                meritev = z.Meritev is null ? null : new
                {
                    id = z.Meritev.Id,
                    pm10 = z.Meritev.Pm10,
                    pm25 = z.Meritev.Pm25,
                    no2 = z.Meritev.No2,
                    o3 = z.Meritev.O3,
                },
                nadMejo = z.NadMejo,
                stanje = z.Stanje,
            }),
        };

        var moznosti = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // da ostanejo č, š, ž in µ berljivi
        };

        File.WriteAllText(pot, JsonSerializer.Serialize(dokument, moznosti), new UTF8Encoding(false));
    }

    // ---------- XML ----------

    public static void ShraniXml(string pot, IReadOnlyList<ZapisIzpostavljenosti> zapisi, Filter filter)
    {
        var koren = new XElement("izpostavljenost",
            new XAttribute("steviloZapisov", zapisi.Count),
            new XAttribute("enota", "µg/m³"),
            new XElement("filter", filter.Opis().Select(p => new XElement(p.Key, p.Value))),
            zapisi.Select(z => new XElement("zapis",
                new XAttribute("lokacijaId", z.Lokacija.Id),
                new XElement("datum", z.Lokacija.DatumTekst),
                new XElement("ura", z.Lokacija.Ura),
                new XElement("lat", Stevilo(z.Lokacija.Lat)),
                new XElement("lon", Stevilo(z.Lokacija.Lon)),
                new XElement("uporabnik",
                    new XAttribute("id", z.Uporabnik.Id),
                    new XElement("ime", z.Uporabnik.Ime),
                    new XElement("mejaPm10", Stevilo(z.Uporabnik.MejaPm10))),
                new XElement("postaja",
                    new XAttribute("id", z.Postaja.Id),
                    new XElement("ime", z.Postaja.Ime),
                    new XElement("mesto", z.Postaja.Mesto),
                    new XElement("oddaljenostKm", Stevilo(z.OddaljenostOdPostajeKm))),
                z.Meritev is null
                    ? new XElement("meritev") // prazen element: za to uro ni meritve
                    : new XElement("meritev",
                        new XAttribute("id", z.Meritev.Id),
                        new XElement("pm10", Stevilo(z.Meritev.Pm10)),
                        new XElement("pm25", Stevilo(z.Meritev.Pm25)),
                        new XElement("no2", Stevilo(z.Meritev.No2)),
                        new XElement("o3", Stevilo(z.Meritev.O3))),
                new XElement("nadMejo", z.NadMejo ? "true" : "false"),
                new XElement("stanje", z.Stanje))));

        var dokument = new XDocument(new XDeclaration("1.0", "UTF-8", null), koren);

        var nastavitve = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
        using var pisalec = XmlWriter.Create(pot, nastavitve);
        dokument.Save(pisalec);
    }

    // Manjkajoča vrednost postane prazen element (npr. <o3></o3>), tako kot v vhodnih datotekah.
    private static string Stevilo(double? v) =>
        v is double d ? d.ToString(CultureInfo.InvariantCulture) : "";
}
