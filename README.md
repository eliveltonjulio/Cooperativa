# Cooperativa

Sistema de gestão de cooperativa (ponto, folha, contratos e cooperados) construído com
**ASP.NET Core MVC + .NET 10** e **PostgreSQL** (EF Core).

## Rodando localmente

Pré-requisitos: .NET 10 SDK e PostgreSQL em `localhost:5432`.

```powershell
# 1. Configurar os segredos locais (fora do Git)
cd CooperativaSolution\Cooperativa.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=CooperativaS;Username=postgres;Password=<SUA_SENHA>"
dotnet user-secrets set "AdminInicial:Senha" "<SENHA_FORTE_PARA_O_ADMIN>"
cd ..\..

# 2. Testes
dotnet test CooperativaSolution/CooperativaSolution.slnx

# 3. Executar (hot reload opcional)
.\Run-Cooperativa.ps1
```

O schema do banco é criado automaticamente na primeira execução (migração `InitialCreate`);
em bancos legados o sistema faz baseline e rotaciona a senha antiga do admin.

## Implantação no Azure

Guia completo (recursos, segredos, CI/CD, containers): **[docs/IMPLANTACAO_AZURE.md](docs/IMPLANTACAO_AZURE.md)**.

- **CI/CD:** GitHub Actions (`.github/workflows/deploy.yml`) — build + testes + deploy via OIDC
- **Container:** `Dockerfile` multi-stage (sdk:10.0 → aspnet:10.0)

## Estrutura

```
CooperativaSolution/
├── Cooperativa.Domain/          # Entidades e regras de negócio
├── Cooperativa.Application/     # Casos de uso
├── Cooperativa.Data/            # DbContext (linkado no Infrastructure)
├── Cooperativa.Infrastructure/  # EF Core + Npgsql + migrações
├── Cooperativa.Web/             # ASP.NET Core MVC
└── Cooperativa.Tests/           # Testes xUnit
```

