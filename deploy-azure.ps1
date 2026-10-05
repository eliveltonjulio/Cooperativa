# ============================================================
# Plano B: Container Apps + ACR + PostgreSQL Flexible Server
# ============================================================
$ErrorActionPreference = 'Stop'
$az = 'C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd'

$Location='brazilsouth'; $Rg='rg-cooperativa'; $EnvName='env-cooperativa'
$AppName='app-cooperativa'; $PgServer='pg-cooperativa'; $PgDb='CooperativaS'
$PgUser='coopadmin'; $PgPassword='Coop!Pg#2026xK7qW9'
$AdminSenha='Coop!Admin#2026mR4tY8'; $RepoDir='c:\Projetos\Cooperativa'

function Step($m){ Write-Host ''; Write-Host ('=== '+$m+' ===') -ForegroundColor Cyan }
function Check($m){ if($LASTEXITCODE -ne 0){ throw ('FALHOU: '+$m) }; Write-Host 'OK' }

Step 'Verificando login'
& $az account show -o tsv --query name | Out-Null
Check 'az account show'
Write-Host ('Conta: ' + (& $az account show --query name -o tsv))

Step 'ACR'
$AcrName = & $az acr list --resource-group $Rg --query '[0].name' -o tsv
if (-not $AcrName) {
    $AcrName = 'acrcoop' + (Get-Date -Format 'MMddHHmm')
    & $az acr create --resource-group $Rg --name $AcrName --sku Basic --admin-enabled true --location $Location --output none
    Check 'acr create'
}
Write-Host ('ACR: ' + $AcrName)

Step 'Build da imagem (2-4 min)'
Push-Location $RepoDir
& $az acr build --registry $AcrName --image 'cooperativa:1.0' --file Dockerfile . --output none
Pop-Location
Check 'acr build'

Step 'Container Apps Environment'
$envOk = & $az containerapp env show --name $EnvName --resource-group $Rg --query id -o tsv
if (-not $envOk) {
    & $az containerapp env create --name $EnvName --resource-group $Rg --location $Location --output none
    Check 'containerapp env create'
} else { Write-Host 'ja existe' }

Step 'PostgreSQL Flexible Server (5-10 min)'
$pgOk = & $az postgres flexible-server show --name $PgServer --resource-group $Rg --query id -o tsv
if (-not $pgOk) {
    $ErrorActionPreference = 'Continue'
    & $az postgres flexible-server create --resource-group $Rg --name $PgServer --location $Location --admin-user $PgUser --admin-password $PgPassword --sku-name Standard_B1ms --tier Burstable --storage-size 32 --version 17 --yes --output none
    Check 'postgres create'
    $ErrorActionPreference = 'Stop'
} else { Write-Host 'ja existe' }

Step 'Criando banco de dados'
$dbOk = & $az postgres flexible-server db list -g $Rg -s $PgServer --query "[?name=='$PgDb']" -o tsv
if (-not $dbOk) {
    & $az postgres flexible-server db create -g $Rg -s $PgServer -n $PgDb --output none
    Check 'postgres db create'
} else { Write-Host 'ja existe' }

Step 'Firewall PostgreSQL'
$ErrorActionPreference = 'Continue'
& $az postgres flexible-server firewall-rule create --resource-group $Rg --server-name $PgServer --name AllowAll --start-ip-address 0.0.0.0 --end-ip-address 255.255.255.255 --output none
$ErrorActionPreference = 'Stop'
Write-Host 'OK (restrinja a IPs fixos em producao)'
Step 'Criando Container App'
$caOk = & $az containerapp show --name $AppName --resource-group $Rg --query id -o tsv
if ($caOk) {
    Write-Host 'ja existe - recriando para garantir imagem/env atuais'
    & $az containerapp delete --name $AppName --resource-group $Rg --yes --output none
}
$acrLogin = & $az acr show --name $AcrName --query loginServer -o tsv
$acrUser = & $az acr credential show --name $AcrName --query username -o tsv
$acrPass = & $az acr credential show --name $AcrName --query 'passwords[0].value' -o tsv
$connStr = "Host=$PgServer.postgres.database.azure.com;Port=5432;Database=$PgDb;Username=$PgUser;Password=$PgPassword;Ssl Mode=Require"
$ErrorActionPreference = 'Continue'
& $az containerapp create --name $AppName --resource-group $Rg --environment $EnvName --image "$acrLogin/cooperativa:1.0" --target-port 8080 --ingress external --transport auto --registry-server $acrLogin --registry-username $acrUser --registry-password $acrPass --min-replicas 0 --max-replicas 2 --cpu 0.5 --memory 1.0 --env-vars "ConnectionStrings__DefaultConnection=$connStr" "AdminInicial__Senha=$AdminSenha" "Mfa__Habilitado=true" "ASPNETCORE_ENVIRONMENT=Production" --output none
Check 'containerapp create'
$ErrorActionPreference = 'Stop'

Step 'Obtendo FQDN'
$fqdn = $null
foreach ($i in 1..12) {
    Start-Sleep -Seconds 5
    $fqdn = & $az containerapp show --name $AppName --resource-group $Rg --query 'properties.configuration.ingress.fqdn' -o tsv
    if ($fqdn) { break }
}
if (-not $fqdn) { throw 'FQDN nao disponivel' }
Write-Host ('FQDN: ' + $fqdn)

Step 'Verificando /health'
$ok = $false
foreach ($i in 1..24) {
    Start-Sleep -Seconds 10
    try {
        $r = Invoke-WebRequest -Uri "http://$fqdn/health" -UseBasicParsing -TimeoutSec 20
        if ($r.StatusCode -eq 200) { $ok = $true; Write-Host ('SAUDAVEL: ' + $r.Content); break }
    } catch { Write-Host ('tentativa ' + $i + ': ' + $_.Exception.Message) }
}
if (-not $ok) { Write-Warning 'Sem resposta. Logs: az containerapp logs show'; exit 1 }

Write-Host ''
Write-Host '=== IMPLANTACAO CONCLUIDA ===' -ForegroundColor Green
Write-Host ('URL:            https://' + $fqdn)
Write-Host 'Login:          admin'
Write-Host ('Senha admin:    ' + $AdminSenha)
Write-Host ('Senha Postgres: ' + $PgPassword)
Write-Host ('ACR:            ' + $AcrName + ' (~US$0.16/dia)')
Write-Host 'IMPORTANTE: salve estas senhas agora!'

