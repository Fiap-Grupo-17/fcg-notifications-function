using System.Text.Json;
using System.Text.Json.Serialization;

namespace FCG.Notifications.Functions;

/// <summary>
/// Envelope JSON do MassTransit (content-type <c>application/vnd.masstransit+json</c>).
/// A função lê a mensagem crua da fila RabbitMQ e desembrulha aqui: o payload de negócio
/// fica em <see cref="Message"/> e o identificador de deduplicação em <see cref="MessageId"/>.
/// Só mapeamos os campos que usamos — o restante do envelope é ignorado.
/// </summary>
public sealed class MassTransitEnvelope<T>
{
    /// <summary>Id do envelope (preenchido pelo MassTransit em todo Publish/Send). Chave da idempotência.</summary>
    public Guid? MessageId { get; set; }

    /// <summary>URNs do tipo (ex.: <c>urn:message:FCG.Contracts.Events:UserCreatedEvent</c>). Útil p/ log/roteamento.</summary>
    public string[]? MessageType { get; set; }

    /// <summary>Payload de negócio.</summary>
    public T? Message { get; set; }
}

/// <summary>
/// Desserializa o envelope MassTransit com as opções necessárias observadas no wire format real:
/// propriedades em camelCase e <c>decimal</c> serializado como string (ex.: <c>"199.90"</c>).
/// </summary>
public static class MassTransitEnvelopeSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static MassTransitEnvelope<T>? Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<MassTransitEnvelope<T>>(json, Options);
        }
        catch (JsonException)
        {
            // Payload não-JSON / ilegível = mensagem irrecuperável. Retorna null para o
            // handler logar e dar ACK (return normal), em vez de propagar a exceção e cair
            // em loop de redelivery (poison message) — não há DLQ configurada.
            return null;
        }
    }
}
