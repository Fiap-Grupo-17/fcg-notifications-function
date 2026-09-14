using FCG.Contracts.Events;
using FCG.Notifications.Functions.Emails;

namespace FCG.Notifications.Functions.Tests.Fakes;

/// <summary>Spy de <see cref="IEmailNotifier"/>: conta quantas vezes cada "envio" foi acionado.</summary>
public class EmailNotifierSpy : IEmailNotifier
{
    public int BoasVindas { get; private set; }
    public int CompraAprovada { get; private set; }
    public int CompraRecusada { get; private set; }
    public int TotalEnvios => BoasVindas + CompraAprovada + CompraRecusada;

    public Task EnviarBoasVindasAsync(UserCreatedEvent evt, CancellationToken ct)
    {
        BoasVindas++;
        return Task.CompletedTask;
    }

    public Task EnviarCompraAprovadaAsync(PaymentProcessedEvent evt, CancellationToken ct)
    {
        CompraAprovada++;
        return Task.CompletedTask;
    }

    public Task EnviarCompraRecusadaAsync(PaymentProcessedEvent evt, CancellationToken ct)
    {
        CompraRecusada++;
        return Task.CompletedTask;
    }
}
