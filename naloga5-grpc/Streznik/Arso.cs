using System.Globalization;
using System.Xml.Linq;
using ZrakGrpc;

namespace Streznik;

// Branje zadnjih urnih meritev z ARSO (isto kot pri nalogi 4)
public static class Arso
{
    const string ARSO_XML = "https://www.arso.gov.si/xml/zrak/ones_zrak_urni_podatki_zadnji.xml";
    static readonly HttpClient http = new HttpClient();

    public static async Task<List<Meritev>> Preberi()
    {
        string xml = await http.GetStringAsync(ARSO_XML);
        var dokument = XDocument.Parse(xml);
        string prebrano = DateTime.Now.ToString("HH:mm:ss");

        var meritve = new List<Meritev>();
        foreach (var postaja in dokument.Descendants("postaja"))
        {
            meritve.Add(new Meritev
            {
                Postaja = postaja.Element("merilno_mesto")?.Value ?? "",
                Cas = postaja.Element("datum_do")?.Value ?? "",
                Pm10 = Stevilo(postaja.Element("pm10")),
                Pm25 = Stevilo(postaja.Element("pm2.5")),
                No2 = Stevilo(postaja.Element("no2")),
                O3 = Stevilo(postaja.Element("o3")),
                Prebrano = prebrano
            });
        }
        return meritve;
    }

    // Element manjka ali je prazen = ni meritve (-1). "<2" shranim kot 2.
    static double Stevilo(XElement? element)
    {
        if (element == null)
            return -1;

        string besedilo = element.Value.Trim().Replace("<", "");
        if (double.TryParse(besedilo, NumberStyles.Float, CultureInfo.InvariantCulture, out var vrednost))
            return vrednost;
        return -1;
    }
}
