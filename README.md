# Orders API

WebAPI em ASP.NET Core (.NET 10) para gerenciar pedidos de uma loja: iniciar pedidos, adicionar e remover produtos, fechar pedidos e listá-los com paginação e filtro por status.
Organizada em camadas no estilo DDD, com persistência em EF Core InMemory e testes com xUnit.

## Como rodar

Pré-requisito: [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/Orders.Api
```

| Perfil (`launchSettings.json`) | URL |
|---|---|
| `http` (padrão) | `http://localhost:5146` |
| `https` | `https://localhost:7056` (e `http://localhost:5146`) |

Para usar o perfil `https`: `dotnet run --project src/Orders.Api --launch-profile https`.

- Abrir `/` redireciona para o Swagger UI em `/swagger`.
- Documento OpenAPI: `/openapi/v1.json`.
- O arquivo `src/Orders.Api/Orders.Api.http` tem o fluxo completo para o HTTP client do Rider/JetBrains. Execute primeiro a requisição **"Start a new order"**: ela guarda o id do pedido criado em `{{orderId}}`, usado pelas requisições seguintes.
- O catálogo de produtos é criado (seed) na inicialização da API.

## Como testar

```bash
dotnet test
```

Todos os testes ficam em `tests/Orders.Tests`, separados por camada:

| Pasta | O que cobre |
|---|---|
| `Domain/` | Testes unitários das entidades e regras de negócio (`Order`, `OrderItem`, `Product`, exceções, versão do pedido). |
| `Application/` | Casos de uso (`OrderService`, `ProductService`), paginação e mapeamento para DTOs, com repositórios mockados via NSubstitute. |
| `Infrastructure/` | Repositórios e `OrdersDbContext` contra o EF Core InMemory, incluindo conflitos de concorrência. |
| `Api/` | Testes de integração das rotas com `WebApplicationFactory`: status HTTP, corpo das respostas e formato dos erros. |

Relatório de cobertura (HTML em `coverage/html/index.html`):

```bash
dotnet test tests/Orders.Tests --collect:"XPlat Code Coverage" --results-directory coverage/raw
dotnet tool exec -y dotnet-reportgenerator-globaltool -- -reports:"coverage/raw/*/coverage.cobertura.xml" -targetdir:"coverage/html" -reporttypes:Html
```

## Rotas

| Método | Rota | Descrição | Sucesso | Erros possíveis |
|---|---|---|---|---|
| `GET` | `/api/products` | Lista o catálogo de produtos. | `200` | — |
| `POST` | `/api/orders` | Inicia um pedido novo, vazio e aberto. | `201` | — |
| `GET` | `/api/orders` | Lista pedidos paginados, mais recentes primeiro. Query opcional: `status` (`Open`/`Closed`), `page`, `pageSize`. | `200` | `400` |
| `GET` | `/api/orders/{id}` | Retorna um pedido com seus produtos. | `200` | `400`, `404` |
| `POST` | `/api/orders/{id}/items` | Adiciona unidades de um produto ao pedido. Corpo: `{ "productId", "quantity" }`. | `200` | `400`, `404`, `409`, `422` |
| `DELETE` | `/api/orders/{id}/items/{productId}` | Remove unidades de um produto. Query opcional: `quantity` (sem ela, remove o item inteiro). | `200` | `400`, `404`, `409`, `422` |
| `POST` | `/api/orders/{id}/close` | Fecha o pedido. | `200` | `400`, `404`, `409`, `422` |

Todas as rotas que alteram um pedido devolvem o pedido atualizado. Enums trafegam como texto (`"Open"`, `"Closed"`).

### Exemplo de fluxo

**1. Iniciar um pedido**

```http
POST /api/orders
```

`201 Created` — `Location: http://localhost:5146/api/orders/01a11303-3c56-7862-be6c-6d912110fa17`

```json
{
  "id": "01a11303-3c56-7862-be6c-6d912110fa17",
  "status": "Open",
  "createdAt": "2026-10-06T20:59:01.5908595Z",
  "closedAt": null,
  "total": 0,
  "items": []
}
```

**2. Adicionar 3 mouses**

```http
POST /api/orders/01a11303-3c56-7862-be6c-6d912110fa17/items
Content-Type: application/json

{
  "productId": "00000000-0000-0000-0000-000000000002",
  "quantity": 3
}
```

`200 OK`

```json
{
  "id": "01a11303-3c56-7862-be6c-6d912110fa17",
  "status": "Open",
  "createdAt": "2026-10-06T20:59:01.5908595Z",
  "closedAt": null,
  "total": 360.00,
  "items": [
    {
      "productId": "00000000-0000-0000-0000-000000000002",
      "productName": "Mouse",
      "unitPrice": 120.00,
      "quantity": 3,
      "subtotal": 360.00
    }
  ]
}
```

**3. Remover 1 unidade** (sem `quantity`, o item inteiro seria removido)

```http
DELETE /api/orders/01a11303-3c56-7862-be6c-6d912110fa17/items/00000000-0000-0000-0000-000000000002?quantity=1
```

`200 OK` — o item passa a ter `quantity: 2` e o pedido `total: 240.00`.

**4. Fechar o pedido**

```http
POST /api/orders/01a11303-3c56-7862-be6c-6d912110fa17/close
```

`200 OK` — `status: "Closed"` e `closedAt` preenchido.

**5. Listar pedidos fechados**

```http
GET /api/orders?status=Closed&page=1&pageSize=10
```

`200 OK`

```json
{
  "items": [
    {
      "id": "01a11303-3c56-7862-be6c-6d912110fa17",
      "status": "Closed",
      "createdAt": "2026-10-06T20:59:01.5908595Z",
      "closedAt": "2026-10-06T21:00:12.3456789Z",
      "total": 240.00,
      "items": [
        {
          "productId": "00000000-0000-0000-0000-000000000002",
          "productName": "Mouse",
          "unitPrice": 120.00,
          "quantity": 2,
          "subtotal": 240.00
        }
      ]
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1
}
```

## Catálogo de produtos

Criado na inicialização, com ids fixos (`ProductConfiguration.cs`):

| Id | Nome | Preço |
|---|---|---|
| `00000000-0000-0000-0000-000000000001` | Notebook | 4500.00 |
| `00000000-0000-0000-0000-000000000002` | Mouse | 120.00 |
| `00000000-0000-0000-0000-000000000003` | Keyboard | 250.00 |
| `00000000-0000-0000-0000-000000000004` | Monitor 27" | 1800.00 |
| `00000000-0000-0000-0000-000000000005` | Headset | 350.00 |
| `00000000-0000-0000-0000-000000000006` | USB-C Cable | 45.00 |

## Erros

Todos os erros seguem ProblemDetails (RFC 9457). Erros conhecidos trazem também o campo `code`, um código estável que o cliente pode usar sem depender do texto da mensagem:

```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "Business rule violated",
  "status": 422,
  "detail": "The order is closed and cannot be modified.",
  "code": "OrderClosed"
}
```

| Status | Significado | Códigos (`code`) |
|---|---|---|
| `400` | Parâmetro inválido | `InvalidPage`, `InvalidPageSize` |
| `400` | Requisição malformada (enum inválido, id que não é Guid, JSON inválido, campo obrigatório ausente) — `ValidationProblemDetails` com `errors` por campo | — |
| `404` | Recurso não encontrado | `OrderNotFound`, `ProductNotFound` |
| `404` | Rota inexistente | — |
| `409` | O pedido foi alterado por outra requisição ao mesmo tempo; recarregar e tentar de novo | `ConcurrencyConflict` |
| `413` | Corpo da requisição grande demais | — |
| `422` | Regra de negócio violada | `OrderClosed`, `OrderWithoutItems`, `ProductNotInOrder`, `InvalidQuantity`, `QuantityExceedsItem`, `QuantityExceedsLimit` |
| `500` | Erro inesperado; detalhes vão apenas para o log, nunca para o cliente | — |

- O mapeamento exceção → status fica em um único lugar: `src/Orders.Api/ErrorHandling/ApiExceptionHandler.cs`.
- Mensagens e códigos ficam nos `.resx` de cada camada: `Orders.Domain/Resources/DomainErrors.resx`, `Orders.Application/Resources/ApplicationErrors.resx` e `Orders.Api/Resources/ApiErrors.resx` (títulos).
- O `code` é a própria chave da mensagem no `.resx`.

## Regras de negócio

- Pedido fechado não pode ser alterado (`OrderClosed`).
- Um pedido só pode ser fechado se tiver ao menos um produto (`OrderWithoutItems`).
- Adicionar um produto que já está no pedido soma a quantidade ao item existente.
- A remoção pode ser parcial:
  - sem `quantity` (ou com `quantity` igual à do item), o item inteiro é removido;
  - remover mais unidades do que o item tem gera erro (`QuantityExceedsItem`);
  - remover um produto que não está no pedido gera erro (`ProductNotInOrder`).
- Quantidades devem ser maiores que zero (`InvalidQuantity`).
- Máximo de 1000 unidades por item (`QuantityExceedsLimit`).
- Paginação: `page` de 1 até `Pagination.MaxPage` (42949672, para o deslocamento caber em `int`); `pageSize` de 1 a 50. Padrão: `page=1`, `pageSize=10`.
- A listagem vem ordenada do pedido mais recente para o mais antigo.
- O item do pedido guarda um snapshot do nome e do preço do produto no momento em que foi adicionado.

## Arquitetura

```
Orders.Api ──► Orders.Application ──► Orders.Domain
     │                                     ▲
     └───────► Orders.Infrastructure ──────┘

Orders.Domain não depende de nenhum outro projeto.
```

| Projeto | Responsabilidade |
|---|---|
| `Orders.Domain` | Entidades (`Order`, `OrderItem`, `Product`), regras de negócio, exceções de domínio e interfaces de repositório e unit of work. |
| `Orders.Application` | Casos de uso (`OrderService`, `ProductService`), DTOs, mapeamento manual, validação de paginação e erros de aplicação (não encontrado, parâmetro inválido). |
| `Orders.Infrastructure` | `OrdersDbContext` (EF Core InMemory), mapeamentos Fluent API, repositórios, seed do catálogo e controle de concorrência. |
| `Orders.Api` | Controllers, contratos de entrada, tratamento de erros, OpenAPI e Swagger. |
| `tests/Orders.Tests` | Testes de todas as camadas. |

Cada camada registra seus próprios serviços: `AddApplication()` e `AddInfrastructure()`, chamados no `Program.cs`.

## Decisões de design (DDD)

| Decisão | Por quê |
|---|---|
| `Order` é a raiz de agregado; toda alteração passa pelos seus métodos (`AddItem`, `RemoveItem`, `Close`). Os itens ficam em uma lista privada, expostos como somente leitura. | Garante que as regras do pedido não possam ser contornadas de fora. |
| `OrderItem` é uma coleção owned (`OwnsMany`). | O item só existe dentro do pedido: é carregado e salvo sempre junto com ele. |
| O item guarda snapshot de nome e preço do produto. | Mudanças futuras no catálogo não alteram pedidos já feitos. |
| Regras de negócio dentro das entidades. | Os casos de uso só coordenam (carregar, alterar, salvar); a regra fica em um único lugar e é testável sem infraestrutura. |
| Ids gerados pelo domínio (`Guid.CreateVersion7()`), com `ValueGeneratedNever` no EF. | A entidade tem identidade desde a criação, sem depender do banco. Guid v7 é ordenável por tempo. |
| Mapeamento com Fluent API (`IEntityTypeConfiguration`). | O domínio não tem atributos nem dependências de persistência. |
| Mensagens de erro em `.resx`, com códigos estáveis. | Nenhum texto hardcoded; o cliente usa o `code`, que não muda se a mensagem mudar. |
| Interfaces de repositório e unit of work no Domain, implementadas na Infrastructure. | As camadas internas não dependem do EF Core; a persistência pode ser trocada. |
| Concorrência otimista com `Order.Version` (concurrency token), convertida em `409`. | Duas requisições alterando o mesmo pedido ao mesmo tempo não sobrescrevem uma à outra em silêncio. |
| Mapeamento manual para DTOs; sem AutoMapper nem MediatR. | Simplicidade e código explícito para o tamanho do projeto; além disso, as duas bibliotecas passaram a ter licença comercial. |
| Controllers finos e um único `IExceptionHandler`. | Controllers só delegam aos casos de uso, sem try/catch; o formato dos erros é definido em um só lugar. |

## Limitações conhecidas

- **Dados em memória**: o EF Core InMemory perde tudo ao reiniciar a API (o catálogo é recriado pelo seed).
- **Sem transações no InMemory**: o provider pode salvar parte das alterações antes de detectar um conflito. Por isso o `OrdersDbContext` verifica as versões antes de salvar e serializa verificação + gravação (ver comentário em `src/Orders.Infrastructure/Persistence/OrdersDbContext.cs`). Com um banco relacional, uma transação resolveria isso.
- **Sem autenticação**: não fazia parte do escopo.
- **Swagger habilitado em todos os ambientes**, de propósito, para facilitar a avaliação.

## Estrutura de pastas

```
src/
├── Orders.Domain/
│   ├── Common/            Entity, exceções, IUnitOfWork
│   ├── Orders/            Order, OrderItem, OrderStatus, IOrderRepository
│   ├── Products/          Product, IProductRepository
│   └── Resources/         DomainErrors.resx
├── Orders.Application/
│   ├── Common/            Paginação, PagedResult, exceções de aplicação
│   ├── Orders/            OrderService, DTOs, mapeamentos
│   ├── Products/          ProductService, DTOs, mapeamentos
│   └── Resources/         ApplicationErrors.resx
├── Orders.Infrastructure/
│   └── Persistence/
│       ├── Configurations/  Mapeamentos Fluent API e seed do catálogo
│       └── Repositories/
└── Orders.Api/
    ├── Contracts/         Modelos de entrada
    ├── Controllers/
    ├── ErrorHandling/     ApiExceptionHandler
    ├── Resources/         ApiErrors.resx
    └── Orders.Api.http
tests/
└── Orders.Tests/
    ├── Domain/
    ├── Application/
    ├── Infrastructure/
    └── Api/
```
