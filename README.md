# FCG Notifications Function

Azure Function (.NET 8 in-process) que **substitui o container 24/7** da `fcg-notifications-api`.

É o entregável da Fase 3: *Migrar para Serverless*. O gatilho de negócio é a **fila do RabbitMQ**, não HTTP e não o Kong.

## Por que não fica na mesma Azure do Kong

O PDF permite **Kong ou Azure APIM ou AWS API Gateway**. O grupo escolheu **Kong local**. Só a Function é obrigatória em nuvem. Contas diferentes (sua de estudante vs. a do Woto) são válidas; depois replicam na conta que forem gravar o vídeo.

```
Cliente HTTP ──► Kong local :8000 ──► UsersAPI / CatalogAPI
                                            │
                                            ▼
                                      RabbitMQ
                                            │
                                            ▼
                               Azure Function (sua conta)
                               OnUserCreated
                               OnPaymentProcessed
```

## O que esta Function faz

| Function | Fila (MassTransit) | Ação |
|---|---|---|
| `OnUserCreated` | `notifications-user-created` | loga e-mail de boas-vindas |
| `OnPaymentProcessed` | `notifications-payment-processed` | loga confirmação ou recusa de compra |
| `Health` | HTTP `GET /api/health` | só para ping de deploy |

A lógica é a mesma dos consumers da Fase 2. O MassTransit publica um **envelope**; o código lê a propriedade `message`.

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
- Docker Desktop (RabbitMQ + APIs da Fase 2)
- Repositório `fcg-contracts` no mesmo diretório pai (ProjectReference)

## 1. Conta Azure de estudante (sua, não a do Woto)

1. Use o e-mail **institucional da FIAP** (`@fiap.com.br` / `@aluno.fiap.com.br`). Gmail **não** passa na verificação de estudante.
2. Abra [Azure for Students](https://azure.microsoft.com/free/students/) e verifique com SheerID.
3. Se a FIAP não passar no SheerID, use o [trial gratuito](https://azure.microsoft.com/free/) (cartão só para validar; o Consumption da Function cabe nos créditos).
4. Confirme:

```bash
az login
az account show
```

## 2. Rodar local (faça isto antes de publicar)

```bash
# Na pasta deste repositório
copy src\FCG.Notifications.Functions\local.settings.json.example src\FCG.Notifications.Functions\local.settings.json

# Suba a plataforma SEM o container antigo de notificações
cd ..\fcg-orchestration
docker compose up -d postgres rabbitmq users-api catalog-api payments-api

# Volte e inicie a Function
cd ..\fcg-notifications-function\src\FCG.Notifications.Functions
func start
```

Antes do `func start`, crie as filas (o trigger da Azure **não** cria sozinho):

```powershell
cd C:\Users\conta\OneDrive\Documentos\GitHub\fcg-notifications-function
powershell -File infra\setup-rabbitmq.ps1
```

Confira em http://localhost:15672 (`guest` / `guest`) → **Queues**: `notifications-user-created` e `notifications-payment-processed`.

**Teste:** `POST /api/usuarios/registrar` no UsersAPI (`http://localhost:8081/swagger` ou via Kong `:8000`). No terminal da Function deve aparecer `[NOTIFICAÇÃO] ✉ E-mail de boas-vindas`.

Pare o container `fcg-notifications-api` se ainda estiver no ar — os dois não podem competir pela mesma fila.

Storage local: se o host pedir `AzureWebJobsStorage`, instale [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) (`npm i -g azurite` e `azurite`) ou use o emulador do Visual Studio.

## 3. RabbitMQ visível na nuvem (só na hora do deploy)

`localhost:5672` não existe dentro da Azure. Na demo da Function publicada, Users/Payments e a Function precisam do **mesmo broker público**:

- mais simples: [CloudAMQP](https://www.cloudamqp.com) plano Cute Cat (free), ou
- RabbitMQ num container na Azure.

Aí:

- `RabbitMQConnection` da Function = `amqps://...` do CloudAMQP
- `RabbitMQ__Host` / user / senha das APIs no compose = o mesmo broker

## 4. Infraestrutura (Bicep) + publish

```bash
az group create --name rg-fcg-notifications --location brazilsouth

az deployment group create \
  --resource-group rg-fcg-notifications \
  --template-file infra/main.bicep \
  --parameters functionAppName=func-fcg-notifications-g17 \
               rabbitMqConnection="amqps://USER:PASS@HOST/VHOST"

cd src/FCG.Notifications.Functions
func azure functionapp publish func-fcg-notifications-g17
```

No portal: Function App → **Functions** (`OnUserCreated`, `OnPaymentProcessed`) → **Application Insights** / Log stream. Esse log é o trecho do vídeo da sua task.

Health após o publish: `https://<hostname>/api/health`

## 5. Depois que funcionar

- Remover `notifications-api` do `docker-compose.yml` e o manifesto `k8s/07-notifications-api.yaml` em `fcg-orchestration`.
- README da orquestração: Notifications = esta Function.
- README de `fcg-notifications-api`: migrado para cá.

## Grupo 17 — Pós-Tech FIAP

- Letícia Lopes Ribeiro Vasconcelos
- Marcelo Henrique Cornelis Rei
- Vinícius Calixto Real
