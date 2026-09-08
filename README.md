# PetCare API

API REST em **ASP.NET Core 10** para gestão de uma clínica veterinária / cuidado de pets, construída em **Clean Architecture**, com CRUD completo, tratamento global de exceções (RFC 7807), observabilidade (health checks, logging estruturado, tracing e métricas) e suíte de testes automatizados (unitários, de aplicação e de integração).

## 1. Domínio

| Entidade | Descrição | Principais regras |
|---|---|---|
| **Tutor** | Responsável pelo(s) pet(s) | Nome e e-mail obrigatórios; e-mail em formato válido e único |
| **Pet** | Animal de estimação | Nome e espécie obrigatórios; data de nascimento não pode ser futura; deve referenciar um Tutor existente |
| **Consulta** | Consulta veterinária de um Pet | Data da consulta obrigatória; deve referenciar um Pet existente |
| **Lembrete** | Lembrete (vacina, retorno, etc.) de um Pet | Título obrigatório; data não pode ser anterior à data atual; deve referenciar um Pet existente |

**Relacionamentos:**

| Entidades | Cardinalidade |
|---|---|
| Tutor → Pet | 1 : N |
| Pet → Consulta | 1 : N |
| Pet → Lembrete | 1 : N |

## 2. Arquitetura em Camadas

| Camada | Projeto | Responsabilidade |
|---|---|---|
| Domínio | `PetCare.Domain` | Entidades, regras de negócio e exceções de domínio (`DomainException`) |
| Aplicação | `PetCare.Application` | DTOs (Request/Response) e interfaces de repositório (genérica e específicas) |
| Infraestrutura | `PetCare.Infrastructure` | `DbContext`, Fluent API, repositórios (com validação de FK), health checks |
| API | `PetCare.API` | Controllers, `Program.cs`, Swagger, logging, tracing/métricas, exceção global |
| Testes de Domínio | `PetCare.Domain.Tests` | Testes unitários (xUnit) das regras de negócio |
| Testes de Aplicação | `PetCare.Application.Tests` | Testes de repositório com Moq + EF Core InMemory |
| Testes de Integração | `PetCare.IntegrationTests` | Testes ponta a ponta com `WebApplicationFactory` |

## 3. Endpoints da API

Todos os controllers seguem o padrão `api/[controller]`:

| Método | Rota | Descrição | Códigos de retorno |
|---|---|---|---|
| GET | `api/tutor` | Lista todos os tutores | 200 |
| GET | `api/tutor/{id}` | Busca tutor por Id | 200, 404 |
| POST | `api/tutor` | Cria tutor | 201, 400, 409 |
| PUT | `api/tutor/{id}` | Atualiza tutor | 200, 400, 404, 409 |
| DELETE | `api/tutor/{id}` | Remove tutor | 204, 404 |
| GET/POST/PUT/DELETE | `api/pet[/{id}]` | CRUD de pets | 200/201/204, 400, 404 |
| GET/POST/PUT/DELETE | `api/consulta[/{id}]` | CRUD de consultas | 200/201/204, 400, 404 |
| GET/POST/PUT/DELETE | `api/lembrete[/{id}]` | CRUD de lembretes | 200/201/204, 400, 404 |

Swagger UI disponível em `/swagger` (ambiente de desenvolvimento).

### Tratamento de erros (RFC 7807 / `ProblemDetails`)

| Exceção | HTTP |
|---|---|
| `ArgumentException` | 400 |
| `DomainException` | 400 |
| `ResourceNotFoundException` / `KeyNotFoundException` | 404 |
| `ConflictException` | 409 |
| Qualquer outra | 500 (sem stack trace em produção) |

A resposta inclui `traceId` para correlação com os logs.

## 4. Monitoramento e Observabilidade

- **Health Checks** — `GET /health`: retorna JSON detalhado (status geral, duração e detalhamento por check: `self` e `oracle_database`). Status HTTP 200 (Healthy/Degraded) ou 503 (Unhealthy).
- **Logging estruturado** — Serilog, saída para console e arquivo (`logs/petcare-api-.log`, rotação diária), com `CorrelationId` por requisição via `Serilog.Enrichers.CorrelationId`.
- **Tracing e métricas** — OpenTelemetry instrumentando ASP.NET Core, HttpClient e EF Core; métricas expostas em `GET /metrics` no formato Prometheus.

## 5. Como executar

### Pré-requisitos
- .NET 10 SDK
- Um banco Oracle acessível (local ou remoto)

### Configuração da connection string (User Secrets — nunca commitar credenciais)
```bash
cd src/PetCare.API
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:OracleConnection" "User Id=SEU_USUARIO;Password=SUA_SENHA;Data Source=SEU_HOST:1521/SEU_SERVICE_NAME"
```

### Restaurar, migrar e executar
```bash
dotnet restore
dotnet tool install --global dotnet-ef   # se ainda não tiver
dotnet ef migrations add InitialCreate --project src/PetCare.Infrastructure --startup-project src/PetCare.API
dotnet ef database update --project src/PetCare.Infrastructure --startup-project src/PetCare.API
dotnet run --project src/PetCare.API
```
A API sobe em `http://localhost:5080` (Swagger em `/swagger`, saúde em `/health`, métricas em `/metrics`).

## 6. Como executar os testes

```bash
# Todos os projetos de teste
dotnet test

# Por camada
dotnet test tests/PetCare.Domain.Tests
dotnet test tests/PetCare.Application.Tests
dotnet test tests/PetCare.IntegrationTests
```

- **Domain.Tests**: regras de negócio das entidades, sem mocks, padrão AAA.
- **Application.Tests**: repositórios com `IRepository<T>` mockado (Moq) e `PetCareContext` isolado via EF Core InMemory por teste.
- **IntegrationTests**: fluxo HTTP completo via `WebApplicationFactory<Program>`, com o Oracle substituído por InMemory apenas para o ambiente de teste (`CustomWebApplicationFactory`).

## 7. Observação sobre este ambiente de geração

Este projeto foi gerado por completo (código-fonte de todas as 7 camadas), porém o ambiente usado para gerá-lo **não tem acesso ao NuGet.org**, então não foi possível rodar `dotnet restore` / `dotnet build` / `dotnet test` aqui para validar a compilação de ponta a ponta. Rode os comandos da seção 6 no seu ambiente local (com acesso à internet) para restaurar os pacotes e confirmar o build.
