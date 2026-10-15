using System.Globalization;
using System.Xml.Serialization;

namespace ZrakSled.XmlJson;

// Razredi, v katere XmlSerializer deserializira XML datoteke iz mape data/.
// Polja, ki so lahko prazna ali manjkajo, beremo kot string in jih pretvorimo v nullable tip,
// ker XmlSerializer prazne vrednosti (<o3></o3>) ne zna pretvoriti v double.

[XmlRoot("postaje")]
public class PostajeDatoteka
{
    [XmlElement("postaja")]
    public List<Postaja> Postaje { get; set; } = new();
}

public class Postaja
{
    [XmlAttribute("id")] public string Id { get; set; } = "";
    [XmlElement("ime")] public string Ime { get; set; } = "";
    [XmlElement("mesto")] public string Mesto { get; set; } = "";
    [XmlElement("tip")] public string Tip { get; set; } = "";
    [XmlElement("lat")] public double Lat { get; set; }
    [XmlElement("lon")] public double Lon { get; set; }
    [XmlElement("nadmorskaVisina")] public string? NadmorskaVisinaTekst { get; set; }
    [XmlElement("datumZacetkaMeritev")] public string DatumZacetkaMeritevTekst { get; set; } = "";
    [XmlElement("opomba")] public string? Opomba { get; set; }

    [XmlIgnore] public int? NadmorskaVisina => Pretvorba.VInt(NadmorskaVisinaTekst);
    [XmlIgnore] public DateOnly DatumZacetkaMeritev => Pretvorba.VDatum(DatumZacetkaMeritevTekst);
}

[XmlRoot("uporabniki")]
public class UporabnikiDatoteka
{
    [XmlElement("uporabnik")]
    public List<Uporabnik> Uporabniki { get; set; } = new();
}

public class Uporabnik
{
    public const double PrivzetaMejaPm10 = 50;

    [XmlAttribute("id")] public string Id { get; set; } = "";
    [XmlElement("ime")] public string Ime { get; set; } = "";
    [XmlElement("telefon")] public string Telefon { get; set; } = "";
    [XmlElement("telegram")] public string? Telegram { get; set; }
    [XmlElement("mesto")] public string Mesto { get; set; } = "";
    [XmlElement("datumRegistracije")] public string DatumRegistracijeTekst { get; set; } = "";
    [XmlElement("mejaPm10")] public string? MejaPm10Tekst { get; set; }
    [XmlElement("opozorila")] public bool Opozorila { get; set; }

    [XmlIgnore] public DateOnly DatumRegistracije => Pretvorba.VDatum(DatumRegistracijeTekst);
    [XmlIgnore] public double MejaPm10 => Pretvorba.VDouble(MejaPm10Tekst) ?? PrivzetaMejaPm10;
}

[XmlRoot("meritve")]
public class MeritveDatoteka
{
    [XmlElement("meritev")]
    public List<Meritev> Meritve { get; set; } = new();
}

public class Meritev
{
    [XmlAttribute("id")] public string Id { get; set; } = "";
    [XmlElement("postajaId")] public string PostajaId { get; set; } = "";
    [XmlElement("datum")] public string DatumTekst { get; set; } = "";
    [XmlElement("ura")] public int Ura { get; set; }
    [XmlElement("pm10")] public string? Pm10Tekst { get; set; }
    [XmlElement("pm25")] public string? Pm25Tekst { get; set; }
    [XmlElement("o3")] public string? O3Tekst { get; set; }
    [XmlElement("no2")] public string? No2Tekst { get; set; }

    [XmlIgnore] public DateOnly Datum => Pretvorba.VDatum(DatumTekst);
    [XmlIgnore] public double? Pm10 => Pretvorba.VDouble(Pm10Tekst);
    [XmlIgnore] public double? Pm25 => Pretvorba.VDouble(Pm25Tekst);
    [XmlIgnore] public double? O3 => Pretvorba.VDouble(O3Tekst);
    [XmlIgnore] public double? No2 => Pretvorba.VDouble(No2Tekst);
}

[XmlRoot("lokacije")]
public class LokacijeDatoteka
{
    [XmlElement("lokacija")]
    public List<Lokacija> Lokacije { get; set; } = new();
}

public class Lokacija
{
    [XmlAttribute("id")] public string Id { get; set; } = "";
    [XmlElement("uporabnikId")] public string UporabnikId { get; set; } = "";
    [XmlElement("postajaId")] public string PostajaId { get; set; } = "";
    [XmlElement("datum")] public string DatumTekst { get; set; } = "";
    [XmlElement("ura")] public int Ura { get; set; }
    [XmlElement("lat")] public double Lat { get; set; }
    [XmlElement("lon")] public double Lon { get; set; }

    [XmlIgnore] public DateOnly Datum => Pretvorba.VDatum(DatumTekst);
}

/// <summary>
/// Rezultat povezovanja: ena točka poti uporabnika skupaj z uporabnikom, najbližjo postajo
/// in meritvijo te postaje ob isti uri (če obstaja).
/// </summary>
public class ZapisIzpostavljenosti
{
    public required Lokacija Lokacija { get; init; }
    public required Uporabnik Uporabnik { get; init; }
    public required Postaja Postaja { get; init; }
    public Meritev? Meritev { get; init; }
    public double OddaljenostOdPostajeKm { get; init; }

    public bool NadMejo => Meritev?.Pm10 is double pm10 && pm10 > Uporabnik.MejaPm10;

    public string Stanje =>
        Meritev?.Pm10 is not double pm10 ? "ni podatka"
        : pm10 > Uporabnik.MejaPm10 ? "NAD MEJO"
        : "v redu";
}

public static class Pretvorba
{
    public static double? VDouble(string? tekst) =>
        double.TryParse(tekst?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;

    public static int? VInt(string? tekst) =>
        int.TryParse(tekst?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    public static DateOnly VDatum(string tekst) =>
        DateOnly.ParseExact(tekst.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
}
