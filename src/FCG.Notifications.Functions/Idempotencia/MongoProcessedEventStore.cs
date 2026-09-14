using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace FCG.Notifications.Functions.Idempotencia;

/// <summary>
/// Idempotência baseada em MongoDB. A atomicidade vem do índice único
/// <c>(consumer, messageId)</c>: uma inserção duplicada lança E11000, capturada
/// e tratada como "já processado".
/// </summary>
/// <remarks>
/// Os índices são garantidos de forma <b>lazy e idempotente</b> na primeira operação
/// (decisão D-1): no host in-process não há <c>IHostedService</c> de bootstrap idiomático,
/// então a barreira de índice fica aqui — versionada em código, criada uma única vez por
/// instância e com retry natural caso o MongoDB ainda não esteja acessível no cold start.
/// </remarks>
public class MongoProcessedEventStore : IProcessedEventStore
{
    private readonly MongoContext _ctx;
    private readonly ILogger<MongoProcessedEventStore> _logger;

    private readonly SemaphoreSlim _indexLock = new(1, 1);
    private volatile bool _indexesEnsured;

    public MongoProcessedEventStore(MongoContext ctx, ILogger<MongoProcessedEventStore> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public async Task<bool> TryRegisterAsync(
        Guid messageId,
        string consumer,
        string? businessKey,
        string eventType,
        CancellationToken ct)
    {
        await EnsureIndexesAsync(ct);

        var doc = new ProcessedEventDocument
        {
            MessageId = messageId,
            Consumer = consumer,
            EventType = eventType,
            BusinessKey = businessKey,
            ProcessedAt = DateTime.UtcNow
        };

        try
        {
            await _ctx.ProcessedEvents.InsertOneAsync(doc, options: null, ct);
            return true; // primeira vez
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return false; // já processado (duplicado)
        }
    }

    public async Task<bool> AlreadyProcessedAsync(
        Guid messageId,
        string consumer,
        CancellationToken ct)
    {
        await EnsureIndexesAsync(ct);

        var filtro = Builders<ProcessedEventDocument>.Filter.And(
            Builders<ProcessedEventDocument>.Filter.Eq(d => d.Consumer, consumer),
            Builders<ProcessedEventDocument>.Filter.Eq(d => d.MessageId, messageId));

        return await _ctx.ProcessedEvents.Find(filtro).AnyAsync(ct);
    }

    /// <summary>
    /// Cria os índices MongoDB uma única vez por instância (idempotente). Não seta a flag
    /// em caso de falha, permitindo nova tentativa na próxima mensagem (ex.: Mongo ainda subindo).
    /// </summary>
    private async Task EnsureIndexesAsync(CancellationToken ct)
    {
        if (_indexesEnsured) return;

        await _indexLock.WaitAsync(ct);
        try
        {
            if (_indexesEnsured) return;

            var pe = _ctx.ProcessedEvents;

            // processed_events: índice único (consumer, messageId) → base atômica da idempotência.
            var uniqueKey = Builders<ProcessedEventDocument>.IndexKeys
                .Ascending(x => x.Consumer)
                .Ascending(x => x.MessageId);
            await pe.Indexes.CreateOneAsync(
                new CreateIndexModel<ProcessedEventDocument>(
                    uniqueKey,
                    new CreateIndexOptions { Unique = true, Name = "ux_consumer_messageId" }),
                cancellationToken: ct);

            // TTL opcional em processedAt (0 = sem expiração, mantém histórico).
            if (_ctx.Options.ProcessedEventsTtlDays > 0)
            {
                await pe.Indexes.CreateOneAsync(
                    new CreateIndexModel<ProcessedEventDocument>(
                        Builders<ProcessedEventDocument>.IndexKeys.Ascending(x => x.ProcessedAt),
                        new CreateIndexOptions
                        {
                            Name = "ttl_processedAt",
                            ExpireAfter = TimeSpan.FromDays(_ctx.Options.ProcessedEventsTtlDays)
                        }),
                    cancellationToken: ct);
            }

            _indexesEnsured = true;
            _logger.LogInformation("✅ Índices MongoDB criados/verificados (processed_events).");
        }
        catch (Exception ex)
        {
            // Não derruba a função por falha de índice; loga e permite retry na próxima mensagem.
            _logger.LogError(ex, "❌ Falha ao criar índices MongoDB. MongoDB está acessível?");
        }
        finally
        {
            _indexLock.Release();
        }
    }
}
