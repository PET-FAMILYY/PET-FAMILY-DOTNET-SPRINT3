# PetCare API

API REST em ASP.NET Core 10 para gestão de uma clínica veterinária / cuidado de pets, construída em **Clean Architecture**.
Reúne CRUD completo de Tutores, Pets, Consultas e Lembretes, **autenticação JWT**, persistência relacional
(**EF Core + Oracle**) e NoSQL (**MongoDB**, trilha de auditoria), **paginação, ordenação e filtros**, **HATEOAS**,
tratamento global de exceções (RFC 7807), observabilidade (health checks, logging estruturado, tracing e métricas) e
suíte de testes automatizados (unitários, de aplicação e de integração, padrão AAA).

## Sumário

1. [Visão geral](#1-visão-geral)
2. [Domínio](#2-domínio)
3. [Arquitetura](#3-arquitetura)
4. [Autenticação (JWT)](#4-autenticação-jwt)
5. [Endpoints da API](#5-endpoints-da-api)
6. [Persistência: Oracle e MongoDB](#6-persistência-oracle-e-mongodb)
7. [Monitoramento e observabilidade](#7-monitoramento-e-observabilidade)
8. [Como executar](#8-como-executar)
9. [Testes e cobertura](#9-testes-e-cobertura)
10. [Documentação da API (Swagger)](#10-documentação-da-api-swagger)
11. [Problemas conhecidos e correções aplicadas](#11-problemas-conhecidos-e-correções-aplicadas)
12. [Boas práticas de segurança](#12-boas-práticas-de-segurança-produção)
13. [Integrantes](#13-integrantes)

---

## 1. Visão geral

| Item | Tecnologia |
|---|---|
| Plataforma | .NET 10 / ASP.NET Core |
| Banco relacional | Oracle via Entity Framework Core (migrations versionadas) |
| Banco NoSQL | MongoDB (auditoria de operações) |
| Autenticação | JWT Bearer |
| Documentação | Swagger/OpenAPI (`/swagger`, exportado em `docs/swagger.json`) |
| Observabilidade | Serilog, OpenTelemetry, Prometheus (`/metrics`), Health Checks |
| Testes | xUnit, Moq, EF Core InMemory, `WebApplicationFactory` |

## 2. Domínio

| Entidade | Descrição | Principais regras |
|---|---|---|
| **Tutor** | Responsável pelo(s) pet(s) | Nome e e-mail obrigatórios; e-mail em formato válido e único |
| **Pet** | Animal de estimação | Nome e espécie obrigatórios; data de nascimento não pode ser futura; deve referenciar um Tutor existente |
| **Consulta** | Consulta veterinária de um Pet | Data da consulta obrigatória; deve referenciar um Pet existente |
| **Lembrete** | Lembrete (vacina, retorno, etc.) de um Pet | Título obrigatório; data não pode ser anterior à data atual; deve referenciar um Pet existente |
| **Usuario** | Usuário autenticável da API | Nome, e-mail e senha obrigatórios; e-mail único; senha armazenada como hash (PBKDF2) |

**Relacionamentos:**

| Entidades | Cardinalidade |
|---|---|
| Tutor → Pet | 1 : N |
| Pet → Consulta | 1 : N |
| Pet → Lembrete | 1 : N |

## 3. Arquitetura

### Diagrama

```mermaid
flowchart LR
    Client([Cliente / Swagger UI]) -->|HTTP + JWT| API

    subgraph Solution[PetCareAPI.sln]
        direction LR
        API["PetCare.API<br/>Controllers, HATEOAS, Program.cs<br/>JWT, Swagger, Health, Exception Handler"]
        APP["PetCare.Application<br/>Serviços (casos de uso), DTOs<br/>Interfaces de repositório, Paginação, Auditoria"]
        DOM["PetCare.Domain<br/>Entidades, regras de negócio<br/>Exceções de domínio"]
        INF["PetCare.Infrastructure<br/>EF Core, Repositórios<br/>MongoDB, JWT, Hash de senha"]
    end

    API --> APP
    API --> INF
    APP --> DOM
    INF --> APP
    INF --> DOM

    INF -->|EF Core| ORA[(Oracle)]
    INF -->|MongoDB.Driver| MONGO[(MongoDB)]
    API -.->|health check| EXT[[Serviço externo ViaCEP]]
    API -.->|/metrics| PROM[Prometheus]
    API -.->|logs| LOG[(Console + arquivo)]
```

**Regra de dependência:** `Domain` não depende de ninguém; `Application` depende só de `Domain`; `Infrastructure`
implementa as interfaces da `Application`; `API` compõe tudo, com injeção de dependência organizada por camada
(`AddApplication()` e `AddInfrastructure()`).

**Fluxo de uma requisição:**
`Controller → Service (Application) → Repository (interface) → EF Core / Oracle` e, em paralelo,
`Service → IAuditTrail → MongoDB`.

### Camadas

| Camada | Projeto | Responsabilidade |
|---|---|---|
| Domínio | `PetCare.Domain` | Entidades, invariantes e exceções de domínio (`DomainException`) |
| Aplicação | `PetCare.Application` | Serviços (casos de uso), DTOs (Request/Response), paginação (`PagedResult`), filtros, interfaces de repositório (genérica e específicas), contratos de auditoria e de autenticação (`IPasswordHasher`, `IJwtTokenGenerator`) |
| Infraestrutura | `PetCare.Infrastructure` | DbContext, Fluent API, migrations, repositórios Oracle (com validação de FK), repositório MongoDB de auditoria, health check do Mongo, hashing de senha (PBKDF2) e geração de token JWT |
| API | `PetCare.API` | Controllers, HATEOAS, `Program.cs`, autenticação/autorização JWT, Swagger (com suporte a Bearer token), logging, tracing/métricas, health checks, exceção global |
| Testes de Domínio | `PetCare.Domain.Tests` | Testes unitários (xUnit) das regras de negócio |
| Testes de Aplicação | `PetCare.Application.Tests` | Testes dos serviços (Moq) e dos repositórios (EF Core InMemory) |
| Testes de Integração | `PetCare.IntegrationTests` | Testes ponta a ponta com `WebApplicationFactory`, autenticação JWT real e `ICollectionFixture` compartilhando a fábrica entre classes |

## 4. Autenticação (JWT)

Todos os endpoints de Tutor, Pet, Consulta, Lembrete e Auditoria exigem autenticação via **Bearer Token (JWT)**. O fluxo é:

1. **Registrar um usuário**

   ```bash
   curl -X POST 'http://localhost:5080/api/auth/register' \
     -H 'Content-Type: application/json' \
     -d '{
       "nome": "vitor",
       "email": "vitor@gmail.com",
       "senha": "123456"
     }'
   ```

2. **Fazer login e obter o token**

   ```bash
   curl -X POST 'http://localhost:5080/api/auth/login' \
     -H 'Content-Type: application/json' \
     -d '{
       "email": "vitor@gmail.com",
       "senha": "123456"
     }'
   ```

   Resposta:

   ```json
   {
     "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
     "expiraEm": "2026-09-09T20:00:00Z",
     "nome": "vitor",
     "email": "vitor@gmail.com",
     "role": "User"
   }
   ```

3. **Usar o token nas chamadas protegidas**

   ```bash
   curl -X POST 'http://localhost:5080/api/tutor' \
     -H 'Content-Type: application/json' \
     -H 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...' \
     -d '{
       "nome": "João Pereira",
       "email": "joao@email.com",
       "telefone": "11999998888"
     }'
   ```

   > Atenção: o header precisa do prefixo `Bearer ` (com espaço) antes do token, senão a API retorna `401 Unauthorized`.

4. **Pelo Swagger:** clique no botão **Authorize** (cadeado no topo) e cole `Bearer {seu token}`. Todas as chamadas feitas pela UI passam a incluir o header automaticamente.

### Endpoints de autenticação

| Método | Rota | Descrição | Códigos de retorno |
|---|---|---|---|
| POST | `/api/auth/register` | Registra um novo usuário | 201, 409 |
| POST | `/api/auth/login` | Autentica e retorna um token JWT | 200, 401 |

### Configuração (`appsettings.json`)

```json
"Jwt": {
  "Key": "TROQUE-ESTA-CHAVE-SUPER-SECRETA-COM-NO-MINIMO-32-CARACTERES",
  "Issuer": "PetCare.API",
  "Audience": "PetCare.Clients",
  "ExpiresInMinutes": "60"
}
```

### Componentes implementados

- `Usuario` (Domain): entidade com nome, e-mail, hash de senha e role.
- `IPasswordHasher` / `PasswordHasher` (Infrastructure): hash PBKDF2 (`Rfc2898DeriveBytes`), sem dependências externas.
- `IJwtTokenGenerator` / `JwtTokenGenerator` (Infrastructure): geração de token JWT assinado com HMAC-SHA256.
- `IUsuarioRepository` / `UsuarioRepository` (Infrastructure): persistência via EF Core.
- `AuthController` (API): endpoints `register` e `login`.
- `[Authorize]` aplicado em `TutorController`, `PetController`, `ConsultaController`, `LembreteController` e `AuditoriaController`.

## 5. Endpoints da API

Todos os controllers seguem o padrão `api/[controller]` (URLs em minúsculas) e exigem `Authorization: Bearer {token}`,
exceto `auth/*` e `health/*`.

| Método | Rota | Descrição | Códigos de retorno |
|---|---|---|---|
| POST | `/api/auth/register` | Registra usuário | 201, 409 |
| POST | `/api/auth/login` | Autentica e devolve JWT | 200, 401 |
| GET | `/api/tutor` | Lista tutores (paginação, ordenação, filtros) | 200, 400 |
| GET | `/api/tutor/{id}` | Busca tutor por Id | 200, 404 |
| POST | `/api/tutor` | Cria tutor | 201, 400, 409 |
| PUT | `/api/tutor/{id}` | Atualiza tutor | 200, 400, 404, 409 |
| DELETE | `/api/tutor/{id}` | Remove tutor | 204, 404 |
| GET | `/api/pet` | Lista pets (paginação, ordenação, filtros) | 200, 400 |
| GET/POST/PUT/DELETE | `/api/pet[/{id}]` | CRUD de pets | 200/201/204, 400, 404 |
| GET | `/api/consulta` | Lista consultas (paginação, ordenação, filtros) | 200, 400 |
| GET/POST/PUT/DELETE | `/api/consulta[/{id}]` | CRUD de consultas | 200/201/204, 400, 404 |
| GET | `/api/lembrete` | Lista lembretes (paginação, ordenação, filtros) | 200, 400 |
| GET/POST/PUT/DELETE | `/api/lembrete[/{id}]` | CRUD de lembretes | 200/201/204, 400, 404 |
| GET | `/api/auditoria` | Trilha de auditoria (MongoDB) | 200, 400 |
| GET | `/health`, `/health/live`, `/health/ready` | Saúde da API e das dependências | 200, 503 |
| GET | `/metrics` | Métricas no formato Prometheus | 200 |

Swagger UI disponível em `/swagger` (ambiente de desenvolvimento), com suporte a autenticação Bearer configurado via
`AddSecurityDefinition`/`AddSecurityRequirement`.

### Paginação, ordenação e filtros (todas as listagens)

| Parâmetro | Descrição | Padrão |
|---|---|---|
| `page` | Página (mínimo 1) | 1 |
| `pageSize` | Itens por página (1 a 50) | 10 |
| `sortBy` | Campo de ordenação (ex.: `nome`); campo inválido retorna 400 | `Id` |
| `sortDir` | `asc` ou `desc` | `asc` |

| Recurso | Filtros | Campos de ordenação |
|---|---|---|
| Tutor | `nome`, `email` | Id, Nome, Email, Telefone |
| Pet | `nome`, `especie`, `tutorId` | Id, Nome, Especie, Raca, DataNascimento, TutorId |
| Consulta | `petId`, `de`, `ate` | Id, DataConsulta, Observacoes, PetId |
| Lembrete | `petId`, `titulo`, `de`, `ate` | Id, Titulo, Descricao, DataLembrete, PetId |
| Auditoria | `entidade`, `entidadeId`, `acao` | DataHora (padrão: mais recentes primeiro), Entidade, Acao, Usuario |

Exemplo:

```bash
curl -H "Authorization: Bearer $TOKEN" \
  "http://localhost:5080/api/pet?especie=cachorro&page=1&pageSize=2&sortBy=nome&sortDir=desc"
```

```json
{
  "items": [
    {
      "id": 7, "nome": "Thor", "especie": "Cachorro", "raca": "Labrador",
      "dataNascimento": "2024-03-10T00:00:00", "tutorId": 3,
      "links": [
        { "href": "/api/pet/7", "rel": "self", "method": "GET" },
        { "href": "/api/pet/7", "rel": "update", "method": "PUT" },
        { "href": "/api/pet/7", "rel": "delete", "method": "DELETE" },
        { "href": "/api/pet", "rel": "collection", "method": "GET" },
        { "href": "/api/tutor/3", "rel": "tutor", "method": "GET" },
        { "href": "/api/consulta?petId=7", "rel": "consultas", "method": "GET" },
        { "href": "/api/lembrete?petId=7", "rel": "lembretes", "method": "GET" }
      ]
    }
  ],
  "page": 1, "pageSize": 2, "totalCount": 5, "totalPages": 3,
  "links": [
    { "href": "/api/pet?especie=cachorro&page=1&pageSize=2&sortBy=nome&sortDir=desc", "rel": "self", "method": "GET" },
    { "href": "/api/pet?especie=cachorro&page=1&pageSize=2&sortBy=nome&sortDir=desc", "rel": "first", "method": "GET" },
    { "href": "/api/pet?especie=cachorro&page=2&pageSize=2&sortBy=nome&sortDir=desc", "rel": "next", "method": "GET" },
    { "href": "/api/pet?especie=cachorro&page=3&pageSize=2&sortBy=nome&sortDir=desc", "rel": "last", "method": "GET" }
  ]
}
```

### HATEOAS

Os endpoints de consulta (`GET`) devolvem `links` em cada recurso (`self`, `update`, `delete`, `collection` e
relações do domínio: pet → tutor, consultas e lembretes; tutor → pets; consulta e lembrete → pet) e em cada página
(`self`, `first`, `prev`, `next`, `last`, preservando os filtros da consulta).

### Tratamento de erros (RFC 7807 / ProblemDetails)

| Exceção | HTTP |
|---|---|
| `ArgumentException` (ex.: campo de ordenação inválido) | 400 |
| `DomainException` | 400 |
| `ResourceNotFoundException` / `KeyNotFoundException` | 404 |
| `ConflictException` | 409 |
| Falha de autenticação (token ausente/inválido/expirado) | 401 |
| Qualquer outra | 500 (sem stack trace em produção) |

A resposta inclui `traceId` para correlação com os logs.

## 6. Persistência: Oracle e MongoDB

### Relacional (Oracle + EF Core)

- `PetCareContext` com Fluent API (`Persistence/Configurations`) e migrations versionadas em `Infrastructure/Migrations`.
- Padrão **Repository**: `IRepository<T>` genérico (`GetAllAsync`, `GetPagedAsync`, `GetByIdAsync`, `AddAsync`,
  `UpdateAsync`, `DeleteAsync`, `ExistsByIdAsync`) e repositórios específicos (`ITutorRepository`, `IPetRepository`,
  `IConsultaRepository`, `ILembreteRepository`, `IUsuarioRepository`), com validação de chave estrangeira nos repositórios
  de Pet, Consulta e Lembrete.
- Paginação e ordenação são feitas no banco (`Skip`/`Take` + `OrderBy` dinâmico), com lista de campos permitidos
  para ordenação.

### NoSQL (MongoDB): trilha de auditoria

Cada criação, atualização e remoção de Tutor, Pet, Consulta e Lembrete grava um documento na coleção `auditoria`:

```json
{ "entidade": "Pet", "entidadeId": 7, "acao": "Criacao", "usuario": "vitor",
  "dataHora": "2026-10-23T17:00:00Z", "detalhes": "Pet 'Thor' cadastrado para o tutor 3." }
```

- Contrato `IAuditLogRepository` na `Application`; implementação `MongoAuditLogRepository` na `Infrastructure`
  (o documento com atributos do driver fica só na infraestrutura).
- A gravação é *best-effort*: se o MongoDB estiver fora do ar, a operação de negócio continua e o erro é logado.
- Consulta: `GET /api/auditoria?entidade=Pet&acao=Criacao&sortBy=dataHora&sortDir=desc`.

Configuração (`appsettings.json`):

```json
"Mongo": {
  "ConnectionString": "mongodb://localhost:27017",
  "Database": "petcare",
  "AuditCollection": "auditoria"
}
```

## 7. Monitoramento e observabilidade

### Health Checks

`GET /health` retorna JSON detalhado (status geral, duração total e detalhamento por check). Status HTTP 200
(`Healthy`/`Degraded`) ou 503 (`Unhealthy`). Também existem `GET /health/live` (apenas a API) e
`GET /health/ready` (dependências).

| Check | Tags | Se falhar |
|---|---|---|
| `self` | live | Unhealthy |
| `oracle_database` | ready, db | **Unhealthy** (503) |
| `mongodb` | ready, db | Degraded (200): a auditoria é best-effort |
| `external_service` (ViaCEP) | ready, external | Degraded (200): falha de terceiro não derruba a API |

O check de serviço externo faz um GET com timeout de 3 segundos. URL e timeout são configuráveis:

```json
"HealthChecks": {
  "ExternalService": {
    "Url": "https://viacep.com.br/ws/01001000/json/",
    "TimeoutSeconds": 3
  }
}
```

### Logging, tracing e métricas

- **Logging estruturado:** Serilog, saída para console e arquivo (`logs/petcare-api-.log`, rotação diária, 14 arquivos), com `CorrelationId` por requisição via `Serilog.Enrichers.CorrelationId`.
- **Tracing e métricas:** OpenTelemetry instrumentando ASP.NET Core, HttpClient e EF Core; métricas expostas em `GET /metrics` no formato Prometheus.

## 8. Como executar

### Pré-requisitos

- .NET 10 SDK
- Um banco Oracle acessível (local ou remoto)
- Docker (para o MongoDB local)
- Configuração da connection string (User Secrets, nunca commitar credenciais)

### MongoDB local

Na raiz da solution, `docker-compose.yml`:

```yaml
services:
  mongo:
    image: mongo:7
    container_name: petcare-mongo
    ports:
      - "27017:27017"
    volumes:
      - petcare-mongo-data:/data/db

volumes:
  petcare-mongo-data:
```

```bash
docker compose up -d
```

### Connection string do Oracle

```bash
cd src/PetCare.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:OracleConnection" "User Id=SEU_USUARIO;Password=SUA_SENHA;Data Source=SEU_HOST:1521/SEU_SERVICE_NAME"
cd ../..
```

### Restaurar, migrar e executar

```bash
dotnet restore
dotnet tool install --global dotnet-ef   # se ainda não tiver
dotnet ef database update --project src/PetCare.Infrastructure --startup-project src/PetCare.API
dotnet run --project src/PetCare.API
```

As migrations (`InitialCreate` e `AddUsuario`) já estão versionadas no repositório; esta versão não altera o modelo
relacional. Para criar uma nova migration no futuro:
`dotnet ef migrations add NomeDaMigration --project src/PetCare.Infrastructure --startup-project src/PetCare.API`.

A API sobe em `http://localhost:5080` (Swagger em `/swagger`, saúde em `/health`, métricas em `/metrics`).

> **Importante:** configure a seção `Jwt` (`Key`, `Issuer`, `Audience`, `ExpiresInMinutes`) no `appsettings.json` antes de subir a API; sem ela, tanto a geração quanto a validação de token falham.

## 9. Testes e cobertura

```bash
# Todos os projetos de teste
dotnet test

# Por camada
dotnet test tests/PetCare.Domain.Tests
dotnet test tests/PetCare.Application.Tests
dotnet test tests/PetCare.IntegrationTests
```

- **Domain.Tests:** regras de negócio das entidades, sem mocks, padrão AAA.
- **Application.Tests:** serviços (Tutor, Pet, Consulta, Lembrete) com repositórios e auditoria mockados (Moq); repositórios com `PetCareContext` isolado via EF Core InMemory por teste, incluindo paginação, ordenação e filtros.
- **IntegrationTests:** fluxo HTTP completo via `WebApplicationFactory<Program>`, com o Oracle substituído por InMemory, o MongoDB por um repositório em memória e os health checks externos isolados apenas no ambiente de teste (`CustomWebApplicationFactory`). A autenticação JWT é real, com token gerado com a mesma chave configurada em `appsettings.json`.

Todos os testes seguem o padrão **Arrange / Act / Assert**. O resultado da última execução está em `resultado-testes.txt`.

### Cobertura (camadas de Domínio e Aplicação)

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:"coverage/report" \
  -assemblyfilters:"+PetCare.Domain;+PetCare.Application" -reporttypes:"Html;TextSummary"
```

Resultado medido em 27/10/2026:

| Camada | Cobertura de linhas |
|---|---|
| `PetCare.Application` | 91,8% |
| `PetCare.Domain` | 82,6% |
| **Total (Domain + Application)** | **88,1%** (326 de 370 linhas) |

Cobertura de branches: 79,3%; de métodos: 86,6%. O relatório HTML é gerado em `coverage/report/index.html`.

### Compartilhamento de fixture entre testes (ICollectionFixture)

As classes de teste de integração compartilham uma única instância de `CustomWebApplicationFactory` (e do mesmo banco InMemory) através de uma coleção xUnit, em vez de recriar a fábrica a cada classe:

```csharp
[CollectionDefinition("IntegrationTests")]
public class IntegrationTestsCollection : ICollectionFixture<CustomWebApplicationFactory>
{
}
```

```csharp
[Collection("IntegrationTests")]
public class TutorControllerTests
{
    private readonly HttpClient _client;

    public TutorControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }
    // ...
}
```

> Como o banco InMemory é compartilhado entre as classes da coleção, os dados de teste usam `Guid.NewGuid()` em campos únicos (e-mail, etc.) para evitar colisão entre testes de classes diferentes.

### Autenticação nos testes de integração

`CustomWebApplicationFactory` expõe:

- `GerarTokenDeTeste(nome, email, role)`: gera um JWT válido usando a mesma `Jwt:Key`/`Issuer`/`Audience` configurados no `appsettings.json` da API.
- `CreateAuthenticatedClient()`: cria um `HttpClient` já com o header `Authorization: Bearer {token}` definido como padrão, usado em todos os testes que dependem de endpoints protegidos por `[Authorize]`.

Isso garante que os testes validem o pipeline de autenticação JWT real (assinatura, emissor, audiência, expiração), em vez de simular a autenticação com um handler fake.

## 10. Documentação da API (Swagger)

- Swagger UI em `/swagger` (ambiente Development), com comentários XML, códigos de resposta e exemplos de filtros por endpoint.
- Especificação OpenAPI exportada em [`docs/swagger.json`](docs/swagger.json). Para regenerar, com a API rodando:

```bash
curl -s http://localhost:5080/swagger/v1/swagger.json -o docs/swagger.json
```

## 11. Problemas conhecidos e correções aplicadas

Durante a implementação, os seguintes problemas foram identificados e corrigidos, documentados aqui para referência futura:

| Problema | Causa | Correção |
|---|---|---|
| `ORA-00904: "FALSE": invalid identifier` ao usar `AnyAsync()` | Bug de versões recentes do `Oracle.EntityFrameworkCore`: `.Any()`/`AnyAsync()` gera SQL com literais `True`/`False`, que o Oracle não suporta fora de blocos PL/SQL | Trocar `AnyAsync(...)` por `CountAsync(...) > 0` em métodos de verificação de existência (ex.: `ExisteEmailAsync`) |
| Erro de compilação em `AddSecurityRequirement` do Swagger | Mudança de API entre versões do `Microsoft.OpenApi`/Swashbuckle: `OpenApiSecurityRequirement` agora é `IDictionary<IOpenApiSecurityScheme, IList<string>>` e espera um delegate `Func<OpenApiDocument, OpenApiSecurityRequirement>` | Usar `options.AddSecurityRequirement(document => new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>(Array.Empty<string>()) })` |
| Testes de integração retornando `401 Unauthorized` em vez do status esperado | `[Authorize]` foi adicionado aos controllers, mas o `HttpClient` dos testes não enviava token | `CustomWebApplicationFactory.CreateAuthenticatedClient()` gera um JWT real e anexa no header `Authorization` de todo `HttpClient` criado para os testes |
| `401` persistindo mesmo após anexar o token nos testes | Ordem incorreta dos middlewares no `Program.cs`: `UseAuthorization()` estava sendo chamado **antes** de `UseAuthentication()` | Corrigir a ordem para `UseAuthentication()` → `UseAuthorization()` (autorização depende do `HttpContext.User` já populado pela autenticação) |
| Swagger UI não carregava o `swagger.json` | A UI embutida do Swashbuckle 10.x não reconhece o patch `3.0.4` do OpenAPI | Middleware que reescreve `"openapi": "3.0.4"` para `"3.0.3"` na resposta do `swagger.json` |
| Gravação de auditoria lenta se o MongoDB estiver fora do ar | O driver espera 30 s pela seleção de servidor por padrão | `ServerSelectionTimeout` de 3 s e gravação *best-effort* (erro logado, operação de negócio preservada) |

## 12. Boas práticas de segurança (produção)

- Nunca commitar a `Jwt:Key` real no repositório; usar User Secrets ou variáveis de ambiente/Key Vault em produção.
- Trocar a chave de exemplo do `appsettings.json` antes de qualquer deploy.
- Credenciais do MongoDB (se houver usuário e senha) também devem vir de User Secrets ou da variável `Mongo__ConnectionString`.
- Considerar adicionar refresh tokens e expiração mais curta do access token para uso em produção.
- Avaliar `[Authorize(Roles = "Admin")]` em endpoints administrativos, caso o domínio evolua para múltiplos níveis de acesso.
- Manter os pacotes NuGet atualizados: o build emite avisos `NU1902`/`NU1903` de vulnerabilidades em dependências transitivas (`System.Security.Cryptography.*`, `Microsoft.OpenApi`, `OpenTelemetry.Api`) que devem ser tratados com atualização de versão antes de produção.

## 13. Integrantes

| Nome | RM |
|---|---|
| Pedro Vaz | RM566551 |
| João Victor Luiz Oliveira Resende | RM565139 |
| Vitor Dias dos Santos | RM565422 |
| Felipe Modesto | RM561810 |
