using FCG.Notifications.Functions.Idempotencia;

namespace FCG.Notifications.Functions.Tests.Fakes;

/// <summary>
/// Fake in-memory de <see cref="IProcessedEventStore"/> — reproduz a semântica atômica do
/// índice único (consumer, messageId) do MongoDB sem infraestrutura. Conta chamadas para as
/// asserções e permite pré-semear um registro (simular "já processado").
/// </summary>
public class ProcessedEventStoreFake : IProcessedEventStore
{
    private readonly HashSet<(Guid MessageId, string Consumer)> _registrados = new();

    public int TryRegisterCalls { get; private set; }
    public int AlreadyProcessedCalls { get; private set; }

    public void Semear(Guid messageId, string consumer) => _registrados.Add((messageId, consumer));

    public Task<bool> AlreadyProcessedAsync(Guid messageId, string consumer, CancellationToken ct)
    {
        AlreadyProcessedCalls++;
        return Task.FromResult(_registrados.Contains((messageId, consumer)));
    }

    public Task<bool> TryRegisterAsync(Guid messageId, string consumer, string? businessKey, string eventType, CancellationToken ct)
    {
        TryRegisterCalls++;
        return Task.FromResult(_registrados.Add((messageId, consumer)));
    }
}
