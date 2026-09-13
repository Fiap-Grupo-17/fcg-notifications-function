using FCG.Contracts.Events;

namespace FCG.Notifications.Functions.Emails;

/// <summary>
/// Abstração do "envio" de e-mails (simulado — ver <see cref="LogEmailNotifier"/>).
/// Isolada por interface para manter as Functions testáveis (mock em vez de I/O real).
/// </summary>
public interface IEmailNotifier
{
    /// <summary>E-mail de boas-vindas, disparado a partir de <see cref="UserCreatedEvent"/>.</summary>
    Task EnviarBoasVindasAsync(UserCreatedEvent evt, CancellationToken ct);

    /// <summary>E-mail de confirmação de compra, disparado quando o pagamento é aprovado.</summary>
    Task EnviarCompraAprovadaAsync(PaymentProcessedEvent evt, CancellationToken ct);

    /// <summary>E-mail informando recusa do pagamento.</summary>
    Task EnviarCompraRecusadaAsync(PaymentProcessedEvent evt, CancellationToken ct);
}
