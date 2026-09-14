# ShopFlow

Plataforma de catálogo e pedidos em **.NET 8** para demonstrar, de ponta a ponta, as competências de um engenheiro sênior: Clean Architecture, DDD, TDD, segurança, mensageria, Azure e entrega contínua.

O domínio é propositalmente simples (produto, pedido, cliente) para a conversa ficar no **como** o software foi construído, não no problema de negócio.

## O que você consegue mostrar nesta solução

| Competência | Onde aparece |
| --- | --- |
| C# / .NET 8 | Toda a solution, `global.json`, nullable, file-scoped namespaces |
| Clean Architecture | `Api` → `Application` → `Domain` ← `Infrastructure` |
| DDD | Aggregates (`Order`, `Product`, `User`), VOs (`Money`, `Email`, `Sku`), eventos de domínio, bounded contexts por namespace |
| SOLID | Handlers pequenos, dependência de abstrações, estratégias de desconto |
| Repository + Unit of Work | `IOrderRepository`, `IProductRepository`, `IUserRepository`, `IUnitOfWork` + pipeline MediatR |
| CQRS | Commands/Queries MediatR; escrita com EF Core 8 e leitura com Dapper |
| TDD / testes | xUnit, Moq, FluentAssertions, testes de arquitetura (NetArchTest) e teste de API |
| JWT | Login, policies `Admin`/`Customer`, Swagger Bearer |
| Azure Key Vault + Managed Identity | `DefaultAzureCredential` em Production + Terraform (`azurerm_user_assigned_identity`) |
| REST | Controllers versionados (`/api/v1`), ProblemDetails, rate limiting, health checks |
| Integração Salesforce | Porta `ISalesforceClient` + adapter HTTP (Polly/resilience) ou adapter local de log |
| EventBus / RabbitMQ / SQS | `IEventBus` com adapters InMemory, RabbitMQ e AWS SQS (Strategy) |
| Outbox | `OutboxMessages` persistidas no `SaveChanges` e processadas pelo Worker |
| Azure Functions (papel) | `ShopFlow.Worker` processa outbox, Salesforce e Blob — o mesmo recorte de uma Function |
| Azure SQL / Storage | EF Core SQL Server + `IBlobStorage` (Azure Blob ou disco local) |
| Terraform | App Service, Function App, SQL, Storage, Key Vault, APIM, Managed Identity |
| CI/CD | GitHub Actions e Azure Pipelines |
| Cache | Redis quando configurado; memória no desenvolvimento (Decorator na query de produto) |
| Padrões | Factory, Strategy, Adapter, Decorator, Specification, Observer (eventos), Mediator, Result, Options |

## Arquitetura

```
src/
  ShopFlow.BuildingBlocks   # Entity, VO, Result, Specification, UoW
  ShopFlow.Domain           # Regras de negócio puras (sem EF, sem MediatR)
  ShopFlow.Application      # Casos de uso, contratos, validações
  ShopFlow.Infrastructure   # EF Core, Dapper, JWT, mensageria, Azure, Salesforce
  ShopFlow.Api              # Composition root HTTP
  ShopFlow.Worker           # Processamento assíncrono (outbox / integrações)
tests/
  ShopFlow.Domain.Tests
  ShopFlow.Application.Tests
  ShopFlow.Architecture.Tests
  ShopFlow.Api.Tests
infra/
  terraform/
  pipelines/
```

Fluxo de um pedido:

1. `POST /api/v1/orders` → `PlaceOrderCommandHandler`
2. Aggregate `Order.Place` reserva estoque e dispara `OrderPlacedDomainEvent`
3. `SaveChanges` grava o aggregate e a mensagem de **outbox** na mesma transação
4. `OutboxProcessor` publica no EventBus, sincroniza Salesforce e arquiva JSON no Storage

## Como executar

Pré-requisitos: .NET 8 SDK e, para persistência real, Docker.

```bash
docker compose up -d sqlserver
dotnet test ShopFlow.sln
dotnet run --project src/ShopFlow.Api
```

Swagger: `https://localhost:7xxx/swagger`

### Usuários seed

| E-mail | Senha | Perfil |
| --- | --- | --- |
| `admin@shopflow.dev` | `Admin@123` | Admin |
| `cliente@shopflow.dev` | `Cliente@123` | Customer |

Cupons: `WELCOME10` (10%) e `VIP20` (20%).

### Exemplo rápido

```http
POST /api/v1/auth/login
{ "email": "cliente@shopflow.dev", "password": "Cliente@123" }

GET /api/v1/products
Authorization: Bearer {token}

POST /api/v1/orders
{
  "items": [{ "productId": "...", "quantity": 1 }],
  "shippingAddress": {
    "street": "Rua Augusta", "number": "100", "city": "São Paulo",
    "state": "SP", "zipCode": "01305-000", "country": "BR"
  },
  "coupon": "WELCOME10"
}
```

Worker (opcional se a API já estiver com outbox habilitada em Development):

```bash
dotnet run --project src/ShopFlow.Worker
```

## Troca de provedores (sem mudar o domínio)

No `appsettings.json`:

- `Messaging:Provider` = `InMemory` | `RabbitMQ` | `Sqs`
- `Salesforce:Enabled` = `true` para o adapter HTTP real
- `AzureStorage:Enabled` = `true` para Blob Storage
- `ConnectionStrings:Redis` para cache distribuído
- Production: `AzureKeyVault:Uri` + Managed Identity (`DefaultAzureCredential`)

## Testes

Os testes de domínio nasceram das regras (TDD): estoque, desconto, transição de status, e-mail e dinheiro.

```bash
dotnet test ShopFlow.sln
```

Testes de arquitetura travam dependências indevidas (Domain não conhece EF/MediatR; Application não conhece Infrastructure).

## Infraestrutura

```bash
cd infra/terraform
terraform init
terraform plan
```

O módulo cria o esqueleto Azure usado no currículo: SQL, Storage, Key Vault, App Service, Function App, API Management e identidade gerenciada.

Pipelines em `.github/workflows/ci.yml` e `infra/pipelines/azure-pipelines.yml`.

## Como apresentar

1. Comece pelo `README` e pelo desenho das camadas.
2. Abra `Order.cs` e mostre invariantes + eventos.
3. Mostre o handler com mocks em `PlaceOrderCommandHandlerTests`.
4. Mostre o outbox em `ShopFlowDbContext.SaveChangesAsync` e o Worker.
5. Feche com Terraform + Key Vault/Managed Identity e o pipeline.

Este repositório é um **portfólio executável**, não um produto comercial.
