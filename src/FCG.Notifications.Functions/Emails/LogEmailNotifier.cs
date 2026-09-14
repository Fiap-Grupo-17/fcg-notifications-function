using FCG.Contracts.Events;
using Microsoft.Extensions.Logging;

namespace FCG.Notifications.Functions.Emails;

/// <summary>
/// Implementação simulada de envio de e-mail: apenas loga a "entrega", sem I/O externo.
/// Formatos de log preservam paridade com a fcg-notifications-api e com as funções originais,
/// para manter observabilidade equivalente após a introdução da camada de idempotência.
/// </summary>
public class LogEmailNotifier : IEmailNotifier
{
    private readonly ILogger<LogEmailNotifier> _logger;

    public LogEmailNotifier(ILogger<LogEmailNotifier> logger) => _logger = logger;

    public Task EnviarBoasVindasAsync(UserCreatedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation(
            "[NOTIFICAÇÃO] ✉ E-mail de boas-vindas ENVIADO | Para: {Email} | Nome: {Nome} | UserId: {UserId} | Cadastrado em: {DataCadastro:dd/MM/yyyy HH:mm}",
            evt.Email, evt.Nome, evt.UserId, evt.DataCadastro);

        return Task.CompletedTask;
    }

    public Task EnviarCompraAprovadaAsync(PaymentProcessedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation(
            "[NOTIFICAÇÃO] ✉ E-mail de confirmação de compra ENVIADO | UserId: {UserId} | Jogo: {GameName} | Preço: R$ {Price:F2} | Transação: {TransactionId} | OrderId: {OrderId}",
            evt.UserId, evt.GameName, evt.Price, evt.TransactionId, evt.OrderId);

        return Task.CompletedTask;
    }

    public Task EnviarCompraRecusadaAsync(PaymentProcessedEvent evt, CancellationToken ct)
    {
        _logger.LogWarning(
            "[NOTIFICAÇÃO] ✉ E-mail de pagamento recusado ENVIADO | UserId: {UserId} | Jogo: {GameName} | Motivo: {MotivoRejeicao} | OrderId: {OrderId}",
            evt.UserId, evt.GameName, evt.MotivoRejeicao, evt.OrderId);

        return Task.CompletedTask;
    }
}
