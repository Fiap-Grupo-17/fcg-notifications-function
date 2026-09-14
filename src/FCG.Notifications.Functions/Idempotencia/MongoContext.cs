using MongoDB.Driver;

namespace FCG.Notifications.Functions.Idempotencia;

/// <summary>
/// Encapsula a conexão MongoDB e expõe a coleção <c>processed_events</c>, usada
/// exclusivamente para idempotência de eventos consumidos pela função. Registrado
/// como singleton (reuso do <c>MongoClient</c> entre invocações/cold starts).
/// </summary>
public class MongoContext
{
    public const string ProcessedEventsCollection = "processed_events";

    public MongoOptions Options { get; }
    public IMongoDatabase Database { get; }

    public MongoContext(MongoOptions options)
    {
        Options = options;

        var client = new MongoClient(options.ConnectionString);
        Database = client.GetDatabase(options.Database);
    }

    public IMongoCollection<ProcessedEventDocument> ProcessedEvents =>
        Database.GetCollection<ProcessedEventDocument>(ProcessedEventsCollection);
}
