# Implantação no Azure — Cooperativa

Guia completo para implantar o sistema **Cooperativa** (ASP.NET Core MVC .NET 10 + PostgreSQL) no Azure.

## Arquitetura recomendada

```
GitHub (eliveltonjulio/Cooperativa)
   └─ GitHub Actions (build → test → deploy via OIDC)
         └─ Azure App Service Linux (stack .NET 10)     ← aplicação
                  └─ Azure Database for PostgreSQL Flexible Server ← banco
```

**Por quê App Service + PostgreSQL Flexible Server:** PaaS (sem manutenção de SO), .NET 10 suportado
como runtime stack oficial, Health Check nativo, slots de staging, HTTPS gerenciado e deploy direto
do GitHub. O sistema não grava arquivos em disco, então funciona bem com `WEBSITE_RUN_FROM_PACKAGE`.

> **Custo estimado (região Brazil South):** App Service B1 ≈ US$ 13/mês + PostgreSQL Flexible B1ms ≈
> US$ 12/mês — suficiente para homologação/pequeno uso. Para produção real, suba para P1v3 e SKU
> Standard do Postgres.

## Pré-requisitos

- Azure CLI instalado (`winget install Microsoft.AzureCLI`)
- `az login` com uma assinatura que tenha permissão de criação de recursos
- .NET 10 SDK (já usado pelo projeto)

## 1. Criar os recursos no Azure

```bash
az login
az group create --name rg-cooperativa --location brazilsouth

# Plano Linux + Web App com stack .NET 10
az appservice plan create --name asp-cooperativa --resource-group rg-cooperativa \
  --is-linux --sku B1

az webapp create --name app-cooperativa --resource-group rg-cooperativa \
  --plan asp-cooperativa --runtime "DOTNET|10.0"

# PostgreSQL Flexible Server (v17)
az postgres flexible-server create \
  --resource-group rg-cooperativa --name pg-cooperativa \
  --location brazilsouth --admin-user coopadmin \
  --admin-password '<SENHA_FORTE>' \
  --sku-name Standard_B1ms --tier Burstable --storage-size 32 \
  --version 17 --database-name CooperativaS

# Regra de firewall: permite conexões de serviços Azure (inclui o App Service)
az postgres flexible-server firewall-rule create \
  --resource-group rg-cooperativa --server-name pg-cooperativa \
  --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
```

> Para produção mais segura, prefira **VNet integration + private access** em vez de `0.0.0.0`.


## 2. Configurar a aplicação (segredos fora do repositório)

A connection string **não está mais no `appsettings.json`** (não há senha versionada). O valor vem da
variável de ambiente `ConnectionStrings__DefaultConnection`, que no App Service é o App Setting:

```bash
az webapp config appsettings set -g rg-cooperativa -n app-cooperativa --settings \
  ConnectionStrings__DefaultConnection="Host=pg-cooperativa.postgres.database.azure.com;Port=5432;Database=CooperativaS;Username=coopadmin;Password=<SENHA_FORTE>;Ssl Mode=Require" \
  AdminInicial__Senha="<SENHA_FORTE_UNICA_PARA_O_ADMIN>" \
  Mfa__Habilitado=true \
  ASPNETCORE_ENVIRONMENT=Production

# Health Check no endpoint /health já existente + HTTPS obrigatório
az webapp config set -g rg-cooperativa -n app-cooperativa --health-check-path /health
az webapp update -g rg-cooperativa -n app-cooperativa --https-only true

## 3. Deploy

### Opção A — GitHub Actions (recomendada, já configurada)

O workflow está em [`.github/workflows/deploy.yml`](../.github/workflows/deploy.yml):
`push` em `main` → restore → build → **testes** → publish → deploy via OIDC.

Para habilitar o login OIDC, crie o App Registration com federated credential:

```bash
# 1. App Registration (client id)
az ad app create --display-name gh-cooperativa
# Anote o appId (CLIENT_ID) e o id do objeto (APP_OBJECT_ID)

# 2. Federated credential apontando para o repositório
az ad app federated-credential create --id <APP_OBJECT_ID> --parameters '{
  "name": "github-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:eliveltonjulio/Cooperativa:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'

# 3. Permissão de Contributor no resource group
az role assignment create --assignee <CLIENT_ID> \
  --role Contributor --scope /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/rg-cooperativa
```

Depois, cadastre os secrets no repositório GitHub
(**Settings → Secrets and variables → Actions**):

| Secret | Valor |
|---|---|
| `AZURE_CLIENT_ID` | AppId do App Registration |
| `AZURE_TENANT_ID` | `az account show --query tenantId -o tsv` |
| `AZURE_SUBSCRIPTION_ID` | `az account show --query id -o tsv` |

### Opção B — deploy manual (primeira vez / emergência)

```powershell

## 4. Checklist pós-deploy

- [ ] Acessar `https://app-cooperativa.azurewebsites.net/health` → deve responder `web-program-marker-2026`
- [ ] Trocar a senha do usuário `admin` logo após o 1º login
- [ ] MFA habilitado (`Mfa__Habilitado=true`) — exigido para administradores
- [ ] Logs: `az webapp log tail -g rg-cooperativa -n app-cooperativa`
- [ ] Ligar **Application Insights** para monitoramento
- [ ] Ativar **PITR/backup** no Flexible Server:
      `az postgres flexible-server backup create -g rg-cooperativa -s pg-cooperativa --backup-type OnDemand`
- [ ] Se escalar para **>1 instância ou usar staging slots**: persistir as chaves de Data Protection
      do cookie auth em Azure Storage (`PersistKeysToAzureBlobStorage`), senão os logins não sobrevivem
      à troca de instância
- [ ] Revisão de SKU (App Service P1v3 + Postgres Standard) para produção

## 5. Segredos em desenvolvimento local

No desenvolvimento, os segredos ficam em **user-secrets** (fora do Git):

```powershell
cd CooperativaSolution\Cooperativa.Web

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=CooperativaS;Username=postgres;Password=<SENHA_LOCAL>"
dotnet user-secrets set "AdminInicial:Senha" "<SENHA_FORTE_LOCAL>"

dotnet user-secrets list   # lista os segredos
```

A senha do banco local antiga (`036945`) está no histórico do Git — **rotacione-a**.

## 6. Solução de problemas

| Sintoma | Causa provável | Solução |
|---|---|---|
| `A connection string 'DefaultConnection' não foi encontrada` | App Setting ausente | Configurar `ConnectionStrings__DefaultConnection` |
| Páginas 500 com erro de relação inexistente | Banco vazio e migração não aplicada | Verificar log: `AplicarMigracoes` deve aplicar `InitialCreate` |
| `password authentication failed` | Senha errada ou firewall | Conferir senha e regra de firewall do Postgres |
| Login em loop após scale-out | Chaves de Data Protection locais | Persistir chaves em Azure Storage (ver checklist) |
| Deploy não publica | Secrets OIDC ausentes | Conferir `AZURE_CLIENT_ID/TENANT_ID/SUBSCRIPTION_ID` |

dotnet publish .\CooperativaSolution\Cooperativa.Web\Cooperativa.Web.csproj -c Release -o publish
Compress-Archive -Path publish\* -DestinationPath publish.zip -Force
az webapp deploy -g rg-cooperativa -n app-cooperativa --src-path publish.zip --type zip
```

### Opção C — containers (Container Apps / ACI)

O repositório possui `Dockerfile` multi-stage (sdk:10.0 → aspnet:10.0, usuário não-root):

```bash
docker build -t cooperativa:1.0 .
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=...;Database=CooperativaS;..." \
  cooperativa:1.0
```

> Valide o build com `docker build .` na sua máquina — este ambiente não possui Docker instalado.

```

> **`AdminInicial__Senha` define a senha do usuário `admin`.** Se não for definida, o sistema gera
> uma senha forte automaticamente e escreve no log na primeira inicialização (troque em seguida).

### Banco de dados: schema é criado automaticamente

A partir desta versão o sistema possui a migração **`InitialCreate`** (EF Core) que cria as 17 tabelas
do zero. Na inicialização:

- **Banco novo (Azure):** `AplicarMigracoes()` detecta schema inexistente → aplica `InitialCreate` →
  roda o script de dados iniciais → cria o admin.
- **Banco legado (tabelas já existem sem histórico):** o sistema faz **baseline** — registra a migração
  como aplicada sem recriar nada — e **rotaciona automaticamente a senha `123456`** do admin.

Ou seja: **basta apontar a connection string para um banco vazio** e o sistema se inicializa sozinho.

Para migrar dados existentes do banco local para o Azure (opcional):

```bash
# Dump completo (schema + dados) do banco local
"C:\Program Files\PostgreSQL\18\bin\pg_dump.exe" -h localhost -U postgres -d CooperativaS -f dump.sql

# Restaurar no Azure (psql local ou Query Editor do portal)
"C:\Program Files\PostgreSQL\18\bin\psql.exe" \
  -h pg-cooperativa.postgres.database.azure.com -U coopadmin -d CooperativaS -f dump.sql
```
