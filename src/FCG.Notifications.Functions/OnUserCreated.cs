using FCG.Contracts.Events;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FCG.Notifications.Functions;

/// <summary>
/// Substitui o UserCreatedConsumer: dispara quando o UsersAPI publica UserCreatedEvent no RabbitMQ.
/// </summary>
public class OnUserCreated
{
    private readonly ILogger<OnUserCreated> _logger;

    public OnUserCreated(ILogger<OnUserCreated> logger) => _logger = logger;

    [Function(nameof(OnUserCreated))]
    public void Run(
        [RabbitMQTrigger("%UserCreatedQueue%", ConnectionStringSetting = "RabbitMQConnection")]
        string body)
    {
        var evt = MassTransitEnvelope.Deserialize<UserCreatedEvent>(body);

        _logger.LogInformation(
            "[NOTIFICAÇÃO] ✉ E-mail de boas-vindas ENVIADO" +
            " | Para: {Email}" +
            " | Nome: {Nome}" +
            " | UserId: {UserId}" +
            " | Cadastrado em: {DataCadastro:dd/MM/yyyy HH:mm}",
            evt.Email, evt.Nome, evt.UserId, evt.DataCadastro);
    }
}
