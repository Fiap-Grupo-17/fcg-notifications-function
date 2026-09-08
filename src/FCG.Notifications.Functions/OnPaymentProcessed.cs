using FCG.Contracts.Events;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FCG.Notifications.Functions;

/// <summary>
/// Substitui o PaymentProcessedConsumer: dispara quando o PaymentsAPI publica PaymentProcessedEvent.
/// </summary>
public class OnPaymentProcessed
{
    private readonly ILogger<OnPaymentProcessed> _logger;

    public OnPaymentProcessed(ILogger<OnPaymentProcessed> logger) => _logger = logger;

    [Function(nameof(OnPaymentProcessed))]
    public void Run(
        [RabbitMQTrigger("%PaymentProcessedQueue%", ConnectionStringSetting = "RabbitMQConnection")]
        string body)
    {
        var evt = MassTransitEnvelope.Deserialize<PaymentProcessedEvent>(body);

        if (evt.Status == "Approved")
        {
            _logger.LogInformation(
                "[NOTIFICAÇÃO] ✉ E-mail de confirmação de compra ENVIADO" +
                " | UserId: {UserId}" +
                " | Jogo: {GameName}" +
                " | Preço: R$ {Price:F2}" +
                " | Transação: {TransactionId}" +
                " | OrderId: {OrderId}",
                evt.UserId, evt.GameName, evt.Price, evt.TransactionId, evt.OrderId);
            return;
        }

        _logger.LogWarning(
            "[NOTIFICAÇÃO] ✉ E-mail de pagamento recusado ENVIADO" +
            " | UserId: {UserId}" +
            " | Jogo: {GameName}" +
            " | Motivo: {Motivo}" +
            " | OrderId: {OrderId}",
            evt.UserId, evt.GameName, evt.MotivoRejeicao, evt.OrderId);
    }
}
