using System.Text;
using ZrakSled.XmlJson;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

try
{
    // 1. Filter: iz argumentov ukazne vrstice ali interaktivno, če argumentov ni.
    string mapaPodatkov, mapaIzhoda;
    Filter filter;
    if (args.Length > 0)
    {
        filter = Filter.IzArgumentov(args, out mapaPodatkov, out mapaIzhoda);
    }
    else
    {
        mapaPodatkov = "data";
        mapaIzhoda = "izhod";
        filter = Filter.Interaktivno();
    }

    // 2. Branje in deserializacija XML datotek.
    var podatki = BranjeXml.Preberi(mapaPodatkov);
    Console.WriteLine($"Prebrano: {podatki.Postaje.Count} postaj, {podatki.Uporabniki.Count} uporabnikov, " +
                      $"{podatki.Meritve.Count} meritev, {podatki.Lokacije.Count} lokacij.");

    // 3. Povezovanje prek ID-jev.
    var opozorila = new List<string>();
    var zapisi = Povezovanje.Povezi(podatki, opozorila);
    foreach (var o in opozorila) Console.WriteLine($"Opozorilo: {o}");

    // 4. Filtriranje.
    var filtrirano = filter.Uporabi(zapisi).ToList();
    var opis = filter.Opis();
    Console.WriteLine();
    Console.WriteLine("Filter: " + (opis.Count == 0 ? "brez (vsi zapisi)" : string.Join(", ", opis.Select(p => $"{p.Key}={p.Value}"))));
    Console.WriteLine($"Najdenih zapisov: {filtrirano.Count} od {zapisi.Count}");
    Console.WriteLine();

    if (filtrirano.Count > 0)
        Izvoz.IzpisiTabelo(filtrirano);
    else
        Console.WriteLine("Noben zapis ne ustreza filtru.");

    // 5. in 6. Izvoz v JSON in XML.
    Directory.CreateDirectory(mapaIzhoda);
    var potJson = Path.Combine(mapaIzhoda, "filtrirano.json");
    var potXml = Path.Combine(mapaIzhoda, "filtrirano.xml");
    Izvoz.ShraniJson(potJson, filtrirano, filter);
    Izvoz.ShraniXml(potXml, filtrirano, filter);

    Console.WriteLine();
    Console.WriteLine($"Shranjeno: {potJson}");
    Console.WriteLine($"Shranjeno: {potXml}");
    return 0;
}
catch (Exception ex) when (ex is ArgumentException or FileNotFoundException or InvalidDataException or InvalidOperationException)
{
    Console.Error.WriteLine($"Napaka: {ex.Message}");
    if (ex.InnerException is not null) Console.Error.WriteLine($"  {ex.InnerException.Message}");
    return 1;
}
