# ShopFlow

API de catálogo e pedidos em **.NET 8**, feita para portfólio: o domínio é simples de propósito para a conversa ficar no desenho, nas regras e na qualidade do código.

## O problema

Um checkout de e-commerce parece um CRUD, mas quebra fácil se estoque, desconto, pagamento e integrações externas forem misturados no controller.

O ShopFlow trata um pedido como um fluxo com regras:

- não vender produto inativo ou sem estoque
- aplicar cupom sem espalhar `if` de desconto pelo código
- autenticar o cliente com JWT
- gravar o pedido e só então avisar Salesforce, fila e storage
- falhar de forma previsível (401, 404, 409), não com exceção genérica

## Por que estas escolhas

| Escolha | Motivo |
| --- | --- |
| Clean Architecture | A API troca, o domínio não. Controllers não conhecem EF, Dapper nem RabbitMQ. |
| DDD (aggregates + VOs + eventos) | Estoque, total e status moram em `Order`/`Product`, não no handler HTTP. |
| TDD | As regras acima nasceram como teste que falha, depois código, depois verde. |
| CQRS | Escrita com EF Core 8 (consistência do aggregate); leitura de dashboard/detalhe com Dapper. |
| Outbox + Worker Service | O pedido e o evento saem na mesma transação. O worker publica depois, sem Dual Write. |
| Adapters (`IEventBus`, `ISalesforceClient`, `IBlobStorage`) | RabbitMQ, SQS, Salesforce HTTP ou log local mudam no `appsettings`, não no domínio. |
| JWT + Result + ProblemDetails | Autenticação explícita e erros de negócio mapeados para HTTP. |

**Worker vs Azure Functions:** `ShopFlow.Worker` é um **.NET Worker Service** (`BackgroundService`) que varre a outbox. Não é um projeto Azure Functions (não há `TimerTrigger`, `QueueTrigger` nem `host.json`). A responsabilidade é a mesma que uma Function teria no Azure — processar outbox, Salesforce e blob — e o Terraform provisiona um Function App como *alvo de hospedagem*, não como o código atual.

Padrões que aparecem depois dessas escolhas: Factory e Strategy (cupom), Adapter (mensageria/Salesforce/storage), Decorator (cache de produto), Specification (pedidos pendentes), Observer (eventos de domínio), Mediator (MediatR), Repository, Unit of Work, Result e Options.

O mapa competência → arquivo está mais abaixo, para não começar a apresentação por uma lista.

## Arquitetura

```
src/
  ShopFlow.BuildingBlocks   # Entity, VO, Result, Specification, UoW
  ShopFlow.Domain           # Regras de negócio (sem EF, sem MediatR)
  ShopFlow.Application      # Casos de uso e contratos
  ShopFlow.Infrastructure   # EF Core, Dapper, JWT, filas, Azure, Salesforce
  ShopFlow.Api              # Composition root HTTP
  ShopFlow.Worker           # Worker Service da outbox (não é Azure Functions)
tests/
  ShopFlow.Domain.Tests
  ShopFlow.Application.Tests
  ShopFlow.Architecture.Tests
  ShopFlow.Api.Tests
infra/
  terraform/                # App Service, Function App, SQL, Key Vault, APIM
  pipelines/
```

Fluxo de um pedido:

1. `POST /api/v1/orders` → `PlaceOrderCommandHandler`
2. `Order.Place` reserva estoque e dispara `OrderPlacedDomainEvent`
3. `SaveChanges` grava o aggregate e a linha de **outbox** na mesma transação
4. `OutboxProcessor` (API em Development e/ou `ShopFlow.Worker`) publica no EventBus, sincroniza Salesforce e arquiva JSON no storage

## Como executar

Pré-requisitos: .NET 8 SDK. Docker só se for usar SQL Server de verdade (`docker compose up -d sqlserver`). Sem Docker, os testes de API sobem com EF InMemory.

```bash
dotnet test ShopFlow.sln
dotnet run --project src/ShopFlow.Api
```

### Onde abrir o Swagger

Não use uma URL inventada. Depois do `dotnet run`, o console imprime `Now listening on:`.

- Perfil `http` (padrão do `dotnet run` neste projeto): [http://localhost:5219/swagger](http://localhost:5219/swagger)
- Perfil `https`: [https://localhost:7071/swagger](https://localhost:7071/swagger)

As portas estão em `src/ShopFlow.Api/Properties/launchSettings.json`. Se a porta estiver ocupada, o .NET escolhe outra — use sempre a URL que o console mostrou e acrescente `/swagger`.

Health check: a mesma origem + `/health`.

### Usuários seed (quando a API sobe fora de Testing)

| E-mail | Senha | Perfil |
| --- | --- | --- |
| `admin@shopflow.dev` | `Admin@123` | Admin |
| `cliente@shopflow.dev` | `Cliente@123` | Customer |

Cupons: `WELCOME10` (10%) e `VIP20` (20%).

### Caminho de sucesso

```http
POST /api/v1/auth/login
{ "email": "cliente@shopflow.dev", "password": "Cliente@123" }

GET /api/v1/products
Authorization: Bearer {token}

POST /api/v1/orders
Authorization: Bearer {token}
{
  "items": [{ "productId": "<id do GET /products>", "quantity": 1 }],
  "shippingAddress": {
    "street": "Rua Augusta", "number": "100", "city": "São Paulo",
    "state": "SP", "zipCode": "01305-000", "country": "BR"
  },
  "coupon": "WELCOME10"
}
```

Esperado: login `200`, listagem `200`, pedido `201`.

Coberto em `CheckoutFlowTests.Customer_Should_Login_List_Products_And_Place_Order`.

### Caminho de falha (resultado esperado)

Sem token, a API não lista produtos:

```http
GET /api/v1/products
```

Esperado: **401 Unauthorized**. Teste: `CheckoutFlowTests.Anonymous_User_Cannot_List_Products`.

Estoque insuficiente não cria pedido. O domínio lança `Product.InsufficientStock`; o handler devolve **Conflict**:

```csharp
// ProductTests.ReserveStock_Should_Fail_When_Insufficient
var product = Product.Create("Capacete", Sku.Create("CAP-001"), Money.Of(199), 2);
product.ReserveStock(3); // DomainException, code Product.InsufficientStock
```

```csharp
// PlaceOrderCommandHandlerTests.Should_Fail_When_Stock_Is_Insufficient
// 1 em estoque, quantity 5 → Result.Failure, Error.Code == "Conflict"
```

Na API autenticada, o mesmo caso responde **409 Conflict** (ProblemDetails com `title: Conflict`). Pedido já enviado também não cancela: `OrderTests.Cancel_Should_Fail_After_Shipping`.

### Worker Service (opcional)

Em Development a API já hospeda o `OutboxProcessor`. O projeto `ShopFlow.Worker` existe para mostrar o recorte de processo separado — o mesmo código de background, outro host:

```bash
dotnet run --project src/ShopFlow.Worker
```

Não rode API e Worker contra o mesmo banco se os dois estiverem processando a outbox ao mesmo tempo.

## Troca de provedores (sem mudar o domínio)

No `appsettings.json`:

- `Messaging:Provider` = `InMemory` | `RabbitMQ` | `Sqs`
- `Salesforce:Enabled` = `true` para o adapter HTTP real
- `AzureStorage:Enabled` = `true` para Blob Storage
- `ConnectionStrings:Redis` para cache distribuído
- Production: `AzureKeyVault:Uri` + Managed Identity (`DefaultAzureCredential`)

## Testes

```bash
dotnet test ShopFlow.sln
```

| Projeto | O que trava |
| --- | --- |
| `ShopFlow.Domain.Tests` | Invariantes: dinheiro, e-mail, estoque, cupom, status do pedido |
| `ShopFlow.Application.Tests` | Handlers com Moq (sucesso, 401, estoque insuficiente) |
| `ShopFlow.Architecture.Tests` | Domain sem EF/MediatR; Application sem Infrastructure |
| `ShopFlow.Api.Tests` | Fluxo HTTP real: 200/201 e 401 |

## O que cada competência mostra no código

| Competência | Onde aparece |
| --- | --- |
| C# / .NET 8 | Solution, `global.json`, nullable, file-scoped namespaces |
| Clean Architecture | `Api` → `Application` → `Domain` ← `Infrastructure` |
| DDD | Aggregates `Order`, `Product`, `User`; VOs `Money`, `Email`, `Sku` |
| SOLID | Handlers pequenos, abstrações, Strategy de desconto |
| Repository + Unit of Work | Repositórios + `UnitOfWorkBehavior` no MediatR |
| CQRS | Commands/Queries; EF na escrita, Dapper na leitura |
| TDD | Teste vermelho → domínio → handler → API |
| JWT | Login, policies `Admin`/`Customer`, Swagger Bearer |
| Key Vault + Managed Identity | `DefaultAzureCredential` em Production + Terraform |
| REST | `/api/v1`, ProblemDetails, rate limiting, `/health` |
| Salesforce | Porta `ISalesforceClient` + adapter HTTP ou log local |
| EventBus / RabbitMQ / SQS | `IEventBus` escolhido por configuração |
| Outbox | `OutboxMessages` no `SaveChanges` |
| Processamento assíncrono | `ShopFlow.Worker` (`BackgroundService`), não Azure Functions |
| Azure SQL / Storage | EF SQL Server + `IBlobStorage` |
| Terraform / CI | `infra/terraform`, GitHub Actions, Azure Pipelines |
| Cache | Redis se configurado; memória no desenvolvimento |

## Infraestrutura

```bash
cd infra/terraform
terraform init
terraform plan
```

O módulo cria o esqueleto Azure do currículo: SQL, Storage, Key Vault, App Service, Function App (hospedagem), API Management e identidade gerenciada.

Pipelines: `.github/workflows/ci.yml` e `infra/pipelines/azure-pipelines.yml`.

## Como apresentar

1. Comece pelo problema (checkout com estoque, cupom e integração) e pelas escolhas da tabela acima.
2. Abra `Order.cs` e mostre invariantes + eventos — ainda sem controller.
3. Mostre um teste **verde** (`ReserveStock_Should_Decrease_Quantity`) e um **vermelho esperado** (`ReserveStock_Should_Fail_When_Insufficient`).
4. Suba a API, abra o Swagger pela URL do console, faça login e um `GET /products` sem token (401).
5. Mostre o outbox em `ShopFlowDbContext.SaveChangesAsync` e deixe claro: Worker Service ≠ Azure Functions.
6. Feche com Terraform (Key Vault / Managed Identity) e o pipeline.

Este repositório é um **portfólio executável**, não um produto comercial.
