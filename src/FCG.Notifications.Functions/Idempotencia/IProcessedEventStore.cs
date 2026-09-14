namespace FCG.Notifications.Functions.Idempotencia;

/// <summary>
/// Store de idempotência / log de eventos consumidos. Implementada sobre MongoDB
/// (coleção <c>processed_events</c> com índice único <c>(consumer, messageId)</c>).
/// Base da deduplicação de eventos consumidos pela função.
/// </summary>
public interface IProcessedEventStore
{
    /// <summary>
    /// Tenta registrar o processamento de uma mensagem de forma atômica.
    /// </summary>
    /// <returns>
    /// <c>true</c> se registrou agora (primeira vez — deve processar);
    /// <c>false</c> se já existia (duplicado — deve ignorar).
    /// </returns>
    Task<bool> TryRegisterAsync(
        Guid messageId,
        string consumer,
        string? businessKey,
        string eventType,
        CancellationToken ct);

    /// <summary>
    /// Verifica, sem registrar, se a mensagem já foi processada por este consumer.
    /// Usada como checagem prévia ao envio (decisão D1: registrar só após o "envio"
    /// do e-mail, para não perder notificações em caso de falha entre registrar e enviar).
    /// </summary>
    Task<bool> AlreadyProcessedAsync(
        Guid messageId,
        string consumer,
        CancellationToken ct);
}
