using FCG.Notifications.Functions.Emails;
using FCG.Notifications.Functions.Idempotencia;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(FCG.Notifications.Functions.Startup))]

namespace FCG.Notifications.Functions;

/// <summary>
/// Bootstrap de injeção de dependências do host in-process (modelo WebJobs).
/// Registra a idempotência em MongoDB (coleção <c>processed_events</c>, dedup pelo
/// <c>MessageId</c> do envelope MassTransit) e o "envio" de e-mail simulado, mantendo
/// as funções acionadas por RabbitMQ testáveis via injeção por construtor.
/// </summary>
public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var configuration = builder.GetContext().Configuration;

        // ── Idempotência de eventos consumidos (MongoDB — coleção processed_events) ──
        // Mesma coleção/índice único (consumer, messageId) da fcg-notifications-api:
        // dedup pelo MessageId do envelope MassTransit. Índices garantidos de forma
        // lazy na primeira operação (ver MongoProcessedEventStore).
        var mongoOptions = new MongoOptions();
        configuration.GetSection(MongoOptions.SectionName).Bind(mongoOptions);
        builder.Services.AddSingleton(mongoOptions);
        builder.Services.AddSingleton<MongoContext>();
        builder.Services.AddSingleton<IProcessedEventStore, MongoProcessedEventStore>();

        // ── Notificações ("envio" de e-mail simulado via log, com paridade de formato) ──
        builder.Services.AddSingleton<IEmailNotifier, LogEmailNotifier>();
    }
}
