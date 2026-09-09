# PetCare API

API REST em ASP.NET Core 10 para gestão de uma clínica veterinária / cuidado de pets, construída em Clean Architecture, com CRUD completo, **autenticação JWT**, tratamento global de exceções (RFC 7807), observabilidade (health checks, logging estruturado, tracing e métricas) e suíte de testes automatizados (unitários, de aplicação e de integração).

## 1. Domínio

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

## 2. Arquitetura em Camadas

| Camada | Projeto | Responsabilidade |
|---|---|---|
| Domínio | `PetCare.Domain` | Entidades, regras de negócio e exceções de domínio (`DomainException`) |
| Aplicação | `PetCare.Application` | DTOs (Request/Response), interfaces de repositório (genérica e específicas), interfaces de autenticação (`IPasswordHasher`, `IJwtTokenGenerator`) |
| Infraestrutura | `PetCare.Infrastructure` | DbContext, Fluent API, repositórios (com validação de FK), health checks, hashing de senha (PBKDF2) e geração de token JWT |
| API | `PetCare.API` | Controllers, `Program.cs`, autenticação/autorização JWT, Swagger (com suporte a Bearer token), logging, tracing/métricas, exceção global |
| Testes de Domínio | `PetCare.Domain.Tests` | Testes unitários (xUnit) das regras de negócio |
| Testes de Aplicação | `PetCare.Application.Tests` | Testes de repositório com Moq + EF Core InMemory |
| Testes de Integração | `PetCare.IntegrationTests` | Testes ponta a ponta com `WebApplicationFactory`, autenticação JWT real e `ICollectionFixture` compartilhando a fábrica entre classes |

## 3. Autenticação (JWT)

Todos os endpoints de Tutor, Pet, Consulta e Lembrete exigem autenticação via **Bearer Token (JWT)**. O fluxo é:

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
| POST | `api/auth/register` | Registra um novo usuário | 201, 409 |
| POST | `api/auth/login` | Autentica e retorna um token JWT | 200, 401 |

### Configuração (appsettings.json)

```json
"Jwt": {
  "Key": "TROQUE-ESTA-CHAVE-SUPER-SECRETA-COM-NO-MINIMO-32-CARACTERES",
  "Issuer": "PetCare.API",
  "Audience": "PetCare.Clients",
  "ExpiresInMinutes": "60"
}
```

### Componentes implementados

- `Usuario` (Domain) — entidade com nome, e-mail, hash de senha e role.
- `IPasswordHasher` / `PasswordHasher` (Infrastructure) — hash PBKDF2 (`Rfc2898DeriveBytes`), sem dependências externas.
- `IJwtTokenGenerator` / `JwtTokenGenerator` (Infrastructure) — geração de token JWT assinado com HMAC-SHA256.
- `IUsuarioRepository` / `UsuarioRepository` (Infrastructure) — persistência via EF Core.
- `AuthController` (API) — endpoints `register` e `login`.
- `[Authorize]` aplicado em `TutorController`, `PetController`, `ConsultaController` e `LembreteController`.

## 4. Endpoints da API

Todos os controllers seguem o padrão `api/[controller]` e exigem autenticação (`[Authorize]`):

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

Swagger UI disponível em `/swagger` (ambiente de desenvolvimento), com suporte a autenticação Bearer configurado via `AddSecurityDefinition`/`AddSecurityRequirement`.

### Tratamento de erros (RFC 7807 / ProblemDetails)

| Exceção | HTTP |
|---|---|
| `ArgumentException` | 400 |
| `DomainException` | 400 |
| `ResourceNotFoundException` / `KeyNotFoundException` | 404 |
| `ConflictException` | 409 |
| Falha de autenticação (token ausente/inválido/expirado) | 401 |
| Qualquer outra | 500 (sem stack trace em produção) |

A resposta inclui `traceId` para correlação com os logs.

## 5. Monitoramento e Observabilidade

- **Health Checks** — `GET /health`: retorna JSON detalhado (status geral, duração e detalhamento por check: `self` e `oracle_database`). Status HTTP 200 (`Healthy`/`Degraded`) ou 503 (`Unhealthy`).
- **Logging estruturado** — Serilog, saída para console e arquivo (`logs/petcare-api-.log`, rotação diária), com `CorrelationId` por requisição via `Serilog.Enrichers.CorrelationId`.
- **Tracing e métricas** — OpenTelemetry instrumentando ASP.NET Core, HttpClient e EF Core; métricas expostas em `GET /metrics` no formato Prometheus.

## 6. Como executar

### Pré-requisitos

- .NET 10 SDK
- Um banco Oracle acessível (local ou remoto)
- Configuração da connection string (User Secrets — nunca commitar credenciais)

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
dotnet ef migrations add AddUsuario --project src/PetCare.Infrastructure --startup-project src/PetCare.API
dotnet ef database update --project src/PetCare.Infrastructure --startup-project src/PetCare.API
dotnet run --project src/PetCare.API
```

A API sobe em `http://localhost:5080` (Swagger em `/swagger`, saúde em `/health`, métricas em `/metrics`).

> **Importante:** configure a seção `Jwt` (`Key`, `Issuer`, `Audience`, `ExpiresInMinutes`) no `appsettings.json` antes de subir a API — sem ela, tanto a geração quanto a validação de token falham.

## 7. Como executar os testes

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
- **IntegrationTests**: fluxo HTTP completo via `WebApplicationFactory<Program>`, com o Oracle substituído por InMemory apenas para o ambiente de teste (`CustomWebApplicationFactory`), e autenticação JWT real usando um token gerado com a mesma chave configurada em `appsettings.json`.

### Compartilhamento de fixture entre testes (ICollectionFixture)

As classes de teste de integração (`TutorControllerTests`, `PetControllerTests`, `ConsultaControllerTests`, `LembreteControllerTests`) compartilham uma única instância de `CustomWebApplicationFactory` (e do mesmo banco InMemory) através de uma coleção xUnit, em vez de recriar a fábrica a cada classe:

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

- `GerarTokenDeTeste(nome, email, role)` — gera um JWT válido usando a mesma `Jwt:Key`/`Issuer`/`Audience` configurados no `appsettings.json` da API.
- `CreateAuthenticatedClient()` — cria um `HttpClient` já com o header `Authorization: Bearer {token}` definido como padrão, usado em todos os testes que dependem de endpoints protegidos por `[Authorize]`.

Isso garante que os testes validem o pipeline de autenticação JWT real (assinatura, emissor, audiência, expiração), em vez de simular a autenticação com um handler fake.

## 8. Problemas conhecidos e correções aplicadas

Durante a implementação, os seguintes problemas foram identificados e corrigidos — documentados aqui para referência futura:

| Problema | Causa | Correção |
|---|---|---|
| `ORA-00904: "FALSE": invalid identifier` ao usar `AnyAsync()` | Bug de versões recentes do `Oracle.EntityFrameworkCore`: `.Any()`/`AnyAsync()` gera SQL com literais `True`/`False`, que o Oracle não suporta fora de blocos PL/SQL | Trocar `AnyAsync(...)` por `CountAsync(...) > 0` em métodos de verificação de existência (ex.: `ExisteEmailAsync`) |
| Erro de compilação em `AddSecurityRequirement` do Swagger | Mudança de API entre versões do `Microsoft.OpenApi`/Swashbuckle: `OpenApiSecurityRequirement` agora é `IDictionary<IOpenApiSecurityScheme, IList<string>>` e espera um delegate `Func<OpenApiDocument, OpenApiSecurityRequirement>` | Usar `options.AddSecurityRequirement(document => new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>(Array.Empty<string>()) })` |
| Testes de integração retornando `401 Unauthorized` em vez do status esperado | `[Authorize]` foi adicionado aos controllers, mas o `HttpClient` dos testes não enviava token | `CustomWebApplicationFactory.CreateAuthenticatedClient()` gera um JWT real e anexa no header `Authorization` de todo `HttpClient` criado para os testes |
| `401` persistindo mesmo após anexar o token nos testes | Ordem incorreta dos middlewares no `Program.cs`: `UseAuthorization()` estava sendo chamado **antes** de `UseAuthentication()` | Corrigir a ordem para `UseAuthentication()` → `UseAuthorization()` (autorização depende do `HttpContext.User` já populado pela autenticação) |

## 9. Boas práticas de segurança (produção)

- Nunca commitar a `Jwt:Key` real no repositório — usar User Secrets ou variáveis de ambiente/Key Vault em produção.
- Trocar a chave de exemplo do `appsettings.json` antes de qualquer deploy.
- Considerar adicionar refresh tokens e expiração mais curta do access token para uso em produção.
- Avaliar `[Authorize(Roles = "Admin")]` em endpoints administrativos, caso o domínio evolua para múltiplos níveis de acesso.
- ## Integrantes

| Nome | RM |
|---|---|
| Pedro Vaz | RM566551 |
| João Victor Luiz Oliveira Resende | RM565139 |
| Vitor Dias dos Santos | RM565422|
| Felipe Modesto | RM561810 |
---
