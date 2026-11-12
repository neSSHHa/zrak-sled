using Grpc.Core;
using ZrakGrpc;

namespace Streznik;

public class ZrakService : ZrakStoritev.ZrakStoritevBase
{
    // 1. Preprost RPC: seznam postaj
    public override async Task<PostajeOdgovor> SeznamPostaj(PrazenZahtevek request, ServerCallContext context)
    {
        var meritve = await Arso.Preberi();
        var odgovor = new PostajeOdgovor();
        odgovor.Postaje.AddRange(meritve.Select(m => m.Postaja).Distinct());
        return odgovor;
    }

    // 2. Preprost RPC: meritev ene postaje
    public override async Task<MeritevOdgovor> MeritevPostaje(PostajaZahtevek request, ServerCallContext context)
    {
        var meritve = await Arso.Preberi();
        var meritev = meritve.FirstOrDefault(m => m.Postaja == request.Postaja);
        if (meritev == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Postaja ne obstaja."));

        return new MeritevOdgovor { Meritev = meritev };
    }

    // 3. Preprost RPC: N postaj z najvišjo vrednostjo izbranega onesnaževala
    public override async Task<MeritveOdgovor> NajslabsePostaje(NajslabseZahtevek request, ServerCallContext context)
    {
        var meritve = await Arso.Preberi();
        int stevilo = request.Stevilo > 0 ? request.Stevilo : 5;

        var najslabse = meritve
            .Where(m => Vrednost(m, request.Onesnazevalo) >= 0) // brez postaj, ki tega ne merijo
            .OrderByDescending(m => Vrednost(m, request.Onesnazevalo))
            .Take(stevilo);

        var odgovor = new MeritveOdgovor();
        odgovor.Meritve.AddRange(najslabse);
        return odgovor;
    }

    // 4. Server-side streaming: na vsakih N sekund pošlje svežo meritev postaje,
    //    dokler odjemalec ne prekine povezave
    public override async Task SpremljajPostajo(SpremljanjeZahtevek request, IServerStreamWriter<Meritev> responseStream, ServerCallContext context)
    {
        int interval = request.IntervalSekund >= 2 ? request.IntervalSekund : 5;

        try
        {
            while (!context.CancellationToken.IsCancellationRequested)
            {
                var meritve = await Arso.Preberi();
                var meritev = meritve.FirstOrDefault(m => m.Postaja == request.Postaja);
                if (meritev != null)
                    await responseStream.WriteAsync(meritev);

                await Task.Delay(interval * 1000, context.CancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // odjemalec je ustavil spremljanje
        }
    }

    static double Vrednost(Meritev m, string onesnazevalo)
    {
        switch (onesnazevalo)
        {
            case "pm10": return m.Pm10;
            case "pm25": return m.Pm25;
            case "no2": return m.No2;
            case "o3": return m.O3;
            default: return m.Pm10;
        }
    }
}
