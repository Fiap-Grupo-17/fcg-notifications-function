using FCG.Contracts.Events;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace FCG.Notifications.Functions;

public class OnUserCreated
{
    [FunctionName(nameof(OnUserCreated))]
    public void Run(
        [RabbitMQTrigger("%UserCreatedQueue%", ConnectionStringSetting = "RabbitMQConnection")]
        string input,
        ILogger log)
    {
        var evt = MassTransitEnvelope.Deserialize<UserCreatedEvent>(input);

        log.LogInformation(
            "[NOTIFICAÇÃO] ✉ E-mail de boas-vindas ENVIADO" +
            " | Para: {Email}" +
            " | Nome: {Nome}" +
            " | UserId: {UserId}" +
            " | Cadastrado em: {DataCadastro:dd/MM/yyyy HH:mm}",
            evt.Email, evt.Nome, evt.UserId, evt.DataCadastro);
    }
}
