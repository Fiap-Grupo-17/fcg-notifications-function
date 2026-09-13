using FCG.Contracts.Events;
using FCG.Notifications.Functions.Emails;
using FCG.Notifications.Functions.Idempotencia;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace FCG.Notifications.Functions;

/// <summary>
/// Acionada por <c>UserCreatedEvent</c> (fila-ponte <c>notifications-user-created</c>,
/// ligada à exchange MassTransit <c>FCG.Contracts.Events:UserCreatedEvent</c>).
/// "Envia" o e-mail de boas-vindas (simulado, via log) com deduplicação por MessageId em MongoDB.
/// </summary>
public class OnUserCreated
{
    private readonly IEmailNotifier _emailNotifier;
    private readonly IProcessedEventStore _processedEventStore;

    public OnUserCreated(IEmailNotifier emailNotifier, IProcessedEventStore processedEventStore)
    {
        _emailNotifier = emailNotifier;
        _processedEventStore = processedEventStore;
    }

    [FunctionName(nameof(OnUserCreated))]
    public async Task Run(
        [RabbitMQTrigger("%UserCreatedQueue%", ConnectionStringSetting = "RabbitMQConnection")]
        string input,
        ILogger log,
        CancellationToken cancellationToken)
    {
        var envelope = MassTransitEnvelopeSerializer.Deserialize<UserCreatedEvent>(input);
        var evt = envelope?.Message;
        var messageId = envelope?.MessageId;

        if (evt is null || messageId is null || messageId == Guid.Empty)
        {
            log.LogWarning("UserCreatedEvent: envelope inválido ou sem MessageId — ignorando");
            return;
        }

        const string consumer = nameof(UserCreatedEvent);
        var eventType = typeof(UserCreatedEvent).FullName!;

        // Checagem ANTES de enviar: evita reenviar e-mail numa reentrega normal (retry/redelivery)
        // do RabbitMQ para uma mensagem já concluída com sucesso.
        if (await _processedEventStore.AlreadyProcessedAsync(messageId.Value, consumer, cancellationToken))
        {
            log.LogInformation(
                "UserCreatedEvent duplicado ignorado (já processado) | MessageId={MessageId}", messageId);
            return;
        }

        await _emailNotifier.EnviarBoasVindasAsync(evt, cancellationToken);

        // Registro APÓS o envio (decisão D1): se a função falhar entre enviar e registrar, o
        // RabbitMQ reentrega e o e-mail é reenviado (aceitável para notificação simulada) — o
        // inverso (registrar antes de enviar) arriscaria "perder" o e-mail em caso de falha após
        // o registro. A janela de duplicidade sob concorrência real é rara e aceita neste cenário.
        var primeiraVez = await _processedEventStore.TryRegisterAsync(
            messageId.Value, consumer, businessKey: evt.UserId.ToString(), eventType, cancellationToken);

        if (!primeiraVez)
        {
            log.LogInformation(
                "UserCreatedEvent: registro concorrente após envio (janela rara aceita) | MessageId={MessageId}",
                messageId);
        }
    }
}
