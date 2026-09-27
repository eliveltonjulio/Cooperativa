param(
    [switch]$NoRestore,
    [switch]$SkipDbCheck,
    [switch]$Watch,
    [switch]$Help
)

$ErrorActionPreference = 'Stop'

if ($Help) {
    Write-Host 'Uso:'
    Write-Host '  .\Run-Cooperativa.ps1'
    Write-Host '  .\Run-Cooperativa.ps1 -NoRestore'
    Write-Host '  .\Run-Cooperativa.ps1 -SkipDbCheck'
    Write-Host '  .\Run-Cooperativa.ps1 -Watch'
    exit 0
}

$solutionDir = Join-Path $PSScriptRoot 'CooperativaSolution'
$webProject = Join-Path $solutionDir 'Cooperativa.Web\Cooperativa.Web.csproj'
$solutionFile = Join-Path $solutionDir 'CooperativaSolution.slnx'

if (-not (Test-Path $solutionDir)) {
    throw "Pasta da solucao nao encontrada: $solutionDir"
}

if (-not (Test-Path $webProject)) {
    throw "Projeto web nao encontrado: $webProject"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet SDK nao encontrado no PATH. Instale o .NET 10 SDK e tente novamente.'
}

if (-not $SkipDbCheck) {
    try {
        $dbPort = Test-NetConnection -ComputerName 'localhost' -Port 5432 -WarningAction SilentlyContinue
        if (-not $dbPort.TcpTestSucceeded) {
            Write-Warning 'PostgreSQL nao respondeu em localhost:5432. A aplicacao pode falhar ao iniciar.'
        }
    }
    catch {
        Write-Warning 'Nao foi possivel validar a porta do PostgreSQL. Seguindo inicializacao.'
    }
}

Push-Location $solutionDir
try {
    if (-not $NoRestore) {
        Write-Host 'Restaurando pacotes...'
        dotnet restore $solutionFile
    }

    if ($Watch) {
        Write-Host 'Iniciando Cooperativa.Web com hot reload (dotnet watch)...'
        dotnet watch --project $webProject run
    }
    else {
        Write-Host 'Iniciando Cooperativa.Web...'
        dotnet run --project $webProject
    }
}
finally {
    Pop-Location
}
