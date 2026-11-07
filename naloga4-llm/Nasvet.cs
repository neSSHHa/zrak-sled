// Razred, v katerega preberemo JSON, ki ga vrne Gemini.
// Imena lastnosti se ujemajo s ključi v JSON (priporoceno, tveganje, ...).
public class Nasvet
{
    public bool Priporoceno { get; set; }
    public string Tveganje { get; set; } = "";
    public string Razlog { get; set; } = "";
    public string? BoljsiCas { get; set; }
    public List<string> Nasveti { get; set; } = new();
}
