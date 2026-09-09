using System.Text;
using System.Text.Json;

namespace FCG.Notifications.Functions;

/// <summary>
/// MassTransit publica um envelope JSON, não o record cru.
/// A Function precisa ler a propriedade <c>message</c> para hidratar o evento.
/// </summary>
internal static class MassTransitEnvelope
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static T Deserialize<T>(byte[] body) =>
        Deserialize<T>(Encoding.UTF8.GetString(body));

    public static T Deserialize<T>(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var payload = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("message", out var message)
            ? message.GetRawText()
            : body;

        return JsonSerializer.Deserialize<T>(payload, Options)
               ?? throw new InvalidOperationException($"Não foi possível desserializar {typeof(T).Name}.");
    }
}
