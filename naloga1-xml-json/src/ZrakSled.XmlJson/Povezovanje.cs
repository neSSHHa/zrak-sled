namespace ZrakSled.XmlJson;

public static class Povezovanje
{
    /// <summary>
    /// Vsako lokacijo poveže z uporabnikom (uporabnikId), postajo (postajaId)
    /// in meritvijo te postaje ob istem datumu in uri.
    /// </summary>
    public static List<ZapisIzpostavljenosti> Povezi(Podatki podatki, List<string> opozorila)
    {
        var uporabniki = podatki.Uporabniki.ToDictionary(u => u.Id);
        var postaje = podatki.Postaje.ToDictionary(p => p.Id);
        var meritve = podatki.Meritve.ToDictionary(m => (m.PostajaId, m.Datum, m.Ura));

        var rezultat = new List<ZapisIzpostavljenosti>();

        foreach (var lokacija in podatki.Lokacije)
        {
            if (!uporabniki.TryGetValue(lokacija.UporabnikId, out var uporabnik))
            {
                opozorila.Add($"Lokacija {lokacija.Id}: uporabnik {lokacija.UporabnikId} ne obstaja, preskočeno.");
                continue;
            }

            if (!postaje.TryGetValue(lokacija.PostajaId, out var postaja))
            {
                opozorila.Add($"Lokacija {lokacija.Id}: postaja {lokacija.PostajaId} ne obstaja, preskočeno.");
                continue;
            }

            meritve.TryGetValue((postaja.Id, lokacija.Datum, lokacija.Ura), out var meritev);
            if (meritev is null)
                opozorila.Add($"Lokacija {lokacija.Id}: postaja {postaja.Id} nima meritve za {lokacija.DatumTekst} ob {lokacija.Ura}:00.");

            rezultat.Add(new ZapisIzpostavljenosti
            {
                Lokacija = lokacija,
                Uporabnik = uporabnik,
                Postaja = postaja,
                Meritev = meritev,
                OddaljenostOdPostajeKm = Math.Round(RazdaljaKm(lokacija.Lat, lokacija.Lon, postaja.Lat, postaja.Lon), 2),
            });
        }

        return rezultat
            .OrderBy(z => z.Lokacija.Datum)
            .ThenBy(z => z.Lokacija.Ura)
            .ThenBy(z => z.Uporabnik.Id)
            .ToList();
    }

    // Haversinova formula: razdalja med dvema točkama na Zemlji v km.
    private static double RazdaljaKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                 + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                 * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
