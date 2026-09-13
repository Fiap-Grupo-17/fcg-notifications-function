using System.Globalization;
using System.Text.Json;

namespace FCG.Notifications.Functions.Tests.Fakes;

/// <summary>
/// Fábrica de envelopes MassTransit no MESMO formato do wire real (confirmado empiricamente):
/// propriedades em camelCase e <c>decimal</c> serializado como string (ex.: "199.90").
/// </summary>
public static class Envelopes
{
    public static string UserCreated(Guid messageId, Guid? userId = null, string nome = "Raul Teste", string email = "raul@fcg.com") =>
        JsonSerializer.Serialize(new
        {
            messageId,
            messageType = new[] { "urn:message:FCG.Contracts.Events:UserCreatedEvent" },
            message = new
            {
                userId = userId ?? Guid.NewGuid(),
                nome,
                email,
                dataCadastro = "2026-09-09T12:00:00Z"
            }
        });

    public static string PaymentProcessed(Guid messageId, Guid? orderId = null, string status = "Approved", decimal price = 199.90m, string gameName = "Elden Ring") =>
        JsonSerializer.Serialize(new
        {
            messageId,
            messageType = new[] { "urn:message:FCG.Contracts.Events:PaymentProcessedEvent" },
            message = new
            {
                orderId = orderId ?? Guid.NewGuid(),
                userId = Guid.NewGuid(),
                gameId = Guid.NewGuid(),
                gameName,
                price = price.ToString(CultureInfo.InvariantCulture), // MassTransit serializa decimal como string
                status,
                transactionId = "TX-123",
                motivoRejeicao = (string?)null,
                processedAt = "2026-09-09T12:00:00Z"
            }
        });
}
