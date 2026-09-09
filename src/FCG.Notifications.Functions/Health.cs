using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace FCG.Notifications.Functions;

public class Health
{
    [FunctionName(nameof(Health))]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("Health check da Function de notificações.");
        return new OkObjectResult(new
        {
            status = "ok",
            service = "fcg-notifications-function",
            timestamp = DateTime.UtcNow
        });
    }
}
