using System.Text.Json;
using Grpc.Core;
using Grpc.Net.Client;
using ZrakGrpc;

var app = WebApplication.Create(args);
app.UseDefaultFiles();
app.UseStaticFiles();

// gRPC odjemalec (nastavek), ki kliče strežnik na portu 5001
var kanal = GrpcChannel.ForAddress("http://localhost:5001");
var client = new ZrakStoritev.ZrakStoritevClient(kanal);

var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);

// SeznamPostaj
app.MapGet("/api/postaje", async () =>
{
    var odgovor = await client.SeznamPostajAsync(new PrazenZahtevek());
    return odgovor.Postaje;
});

// MeritevPostaje: /api/meritev?postaja=MB Titova
app.MapGet("/api/meritev", async (string postaja) =>
{
    try
    {
        var odgovor = await client.MeritevPostajeAsync(new PostajaZahtevek { Postaja = postaja });
        return Results.Ok(odgovor.Meritev);
    }
    catch (RpcException napaka)
    {
        return Results.Problem("gRPC napaka: " + napaka.Status.Detail);
    }
});

// NajslabsePostaje: /api/najslabse?onesnazevalo=pm10&stevilo=5
app.MapGet("/api/najslabse", async (string onesnazevalo, int stevilo) =>
{
    var odgovor = await client.NajslabsePostajeAsync(new NajslabseZahtevek { Onesnazevalo = onesnazevalo, Stevilo = stevilo });
    return odgovor.Meritve;
});

// SpremljajPostajo (stream): vsako sporočilo iz gRPC toka takoj pošljemo
// brskalniku prek Server-Sent Events, da se prikaže v realnem času
app.MapGet("/api/spremljaj", async (string postaja, int interval, HttpContext http) =>
{
    http.Response.ContentType = "text/event-stream";

    var zahtevek = new SpremljanjeZahtevek { Postaja = postaja, IntervalSekund = interval };
    using var klic = client.SpremljajPostajo(zahtevek, cancellationToken: http.RequestAborted);

    try
    {
        await foreach (var meritev in klic.ResponseStream.ReadAllAsync(http.RequestAborted))
        {
            await http.Response.WriteAsync("data: " + JsonSerializer.Serialize(meritev, json) + "\n\n");
            await http.Response.Body.FlushAsync();
        }
    }
    catch (RpcException napaka) when (napaka.StatusCode == StatusCode.Cancelled)
    {
        // uporabnik je zaprl stran ali kliknil Ustavi
    }
    catch (OperationCanceledException)
    {
    }
});

app.Run("http://localhost:5000");
