using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace FCG.Notifications.Functions;

/// <summary>
/// Health check opcional para validar o deploy. O gatilho de negócio é o RabbitMQ, não HTTP.
/// </summary>
public class Health
{
    private readonly ILogger<Health> _logger;

    public Health(ILogger<Health> logger) => _logger = logger;

    [Function(nameof(Health))]
    public HttpResponseData Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")]
        HttpRequestData req)
    {
        _logger.LogInformation("Health check da Function de notificações.");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        response.WriteString(JsonSerializer.Serialize(new
        {
            status = "ok",
            service = "fcg-notifications-function",
            timestamp = DateTime.UtcNow
        }));
        return response;
    }
}
