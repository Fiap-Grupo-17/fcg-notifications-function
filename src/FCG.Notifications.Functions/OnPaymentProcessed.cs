using FCG.Contracts.Events;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace FCG.Notifications.Functions;

public class OnPaymentProcessed
{
    [FunctionName(nameof(OnPaymentProcessed))]
    public void Run(
        [RabbitMQTrigger("%PaymentProcessedQueue%", ConnectionStringSetting = "RabbitMQConnection")]
        string input,
        ILogger log)
    {
        var evt = MassTransitEnvelope.Deserialize<PaymentProcessedEvent>(input);

        if (evt.Status == "Approved")
        {
            log.LogInformation(
                "[NOTIFICAÇÃO] ✉ E-mail de confirmação de compra ENVIADO" +
                " | UserId: {UserId}" +
                " | Jogo: {GameName}" +
                " | Preço: R$ {Price:F2}" +
                " | Transação: {TransactionId}" +
                " | OrderId: {OrderId}",
                evt.UserId, evt.GameName, evt.Price, evt.TransactionId, evt.OrderId);
            return;
        }

        log.LogWarning(
            "[NOTIFICAÇÃO] ✉ E-mail de pagamento recusado ENVIADO" +
            " | UserId: {UserId}" +
            " | Jogo: {GameName}" +
            " | Motivo: {Motivo}" +
            " | OrderId: {OrderId}",
            evt.UserId, evt.GameName, evt.MotivoRejeicao, evt.OrderId);
    }
}
