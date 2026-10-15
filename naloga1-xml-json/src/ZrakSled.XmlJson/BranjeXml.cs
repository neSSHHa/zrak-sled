using System.Xml.Serialization;

namespace ZrakSled.XmlJson;

/// <summary>Vsi podatki, prebrani iz mape data/.</summary>
public record Podatki(
    List<Postaja> Postaje,
    List<Uporabnik> Uporabniki,
    List<Meritev> Meritve,
    List<Lokacija> Lokacije);

public static class BranjeXml
{
    public static Podatki Preberi(string mapa)
    {
        var postaje = Deserializiraj<PostajeDatoteka>(Path.Combine(mapa, "postaje.xml")).Postaje;
        var uporabniki = Deserializiraj<UporabnikiDatoteka>(Path.Combine(mapa, "uporabniki.xml")).Uporabniki;
        var meritve = Deserializiraj<MeritveDatoteka>(Path.Combine(mapa, "meritve.xml")).Meritve;
        var lokacije = Deserializiraj<LokacijeDatoteka>(Path.Combine(mapa, "lokacije.xml")).Lokacije;

        PreveriUnikatneId("postaje.xml", postaje.Select(p => p.Id));
        PreveriUnikatneId("uporabniki.xml", uporabniki.Select(u => u.Id));
        PreveriUnikatneId("meritve.xml", meritve.Select(m => m.Id));
        PreveriUnikatneId("lokacije.xml", lokacije.Select(l => l.Id));

        return new Podatki(postaje, uporabniki, meritve, lokacije);
    }

    private static T Deserializiraj<T>(string pot)
    {
        if (!File.Exists(pot))
            throw new FileNotFoundException($"Ne najdem datoteke {pot}");

        var serializer = new XmlSerializer(typeof(T));
        using var tok = File.OpenRead(pot);
        return (T)(serializer.Deserialize(tok)
            ?? throw new InvalidDataException($"Datoteka {pot} je prazna ali neveljavna"));
    }

    private static void PreveriUnikatneId(string datoteka, IEnumerable<string> idji)
    {
        var podvojeni = idji.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (podvojeni.Count > 0)
            throw new InvalidDataException($"V {datoteka} se ponavljajo ID-ji: {string.Join(", ", podvojeni)}");
    }
}
