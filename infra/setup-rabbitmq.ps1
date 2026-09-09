# Cria as filas da Function e faz bind nos exchanges do MassTransit.
# Requer RabbitMQ Management em http://localhost:15672 (guest/guest).

$ErrorActionPreference = "Stop"
$definitionsPath = Join-Path $PSScriptRoot "rabbitmq-definitions.json"
$body = Get-Content -Raw -Path $definitionsPath
$token = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("guest:guest"))

Invoke-RestMethod `
  -Uri "http://localhost:15672/api/definitions" `
  -Method Post `
  -Headers @{ Authorization = "Basic $token" } `
  -ContentType "application/json" `
  -Body $body

Write-Host "Filas notifications-user-created e notifications-payment-processed criadas."
