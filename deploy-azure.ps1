# Deploy Script para Azure App Service
# Este script prepara e faz o deploy do Jogo da Bolinha para Azure

param(
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroup = "",
    
    [Parameter(Mandatory=$false)]
    [string]$AppServiceName = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$LocalBuild = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Deploy do Jogo da Bolinha para Azure " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar se está no diretório correto
if (-not (Test-Path ".\JogoBolinha\JogoBolinha.csproj")) {
    Write-Host "ERRO: Execute este script a partir da raiz do projeto!" -ForegroundColor Red
    Write-Host "Certifique-se de estar no diretório que contém a pasta JogoBolinha" -ForegroundColor Yellow
    exit 1
}

# Função para verificar se Azure CLI está instalado
function Test-AzureCLI {
    try {
        $null = az --version 2>&1
        return $true
    } catch {
        return $false
    }
}

# 1. PREPARAÇÃO DO BUILD
Write-Host "1. Preparando o Build..." -ForegroundColor Green

if (-not $SkipBuild) {
    Write-Host "   Limpando builds anteriores..." -ForegroundColor Yellow
    Remove-Item -Path ".\JogoBolinha\bin\Release" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path ".\JogoBolinha\obj\Release" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path ".\publish" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path ".\deploy.zip" -Force -ErrorAction SilentlyContinue
    
    Write-Host "   Restaurando pacotes..." -ForegroundColor Yellow
    dotnet restore .\JogoBolinha\JogoBolinha.csproj
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERRO: Falha ao restaurar pacotes!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   Compilando em modo Release..." -ForegroundColor Yellow
    dotnet build .\JogoBolinha\JogoBolinha.csproj --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERRO: Falha na compilação!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   Publicando aplicação..." -ForegroundColor Yellow
    dotnet publish .\JogoBolinha\JogoBolinha.csproj --configuration Release --output .\publish --no-build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERRO: Falha ao publicar!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "   ✓ Build concluído com sucesso!" -ForegroundColor Green
} else {
    Write-Host "   Pulando build (usando publicação existente)..." -ForegroundColor Yellow
}

# 2. VERIFICAÇÃO DOS ARQUIVOS
Write-Host ""
Write-Host "2. Verificando arquivos críticos..." -ForegroundColor Green

$criticalFiles = @(
    ".\publish\JogoBolinha.dll",
    ".\publish\web.config",
    ".\publish\appsettings.json",
    ".\publish\appsettings.Production.json"
)

$allFilesPresent = $true
foreach ($file in $criticalFiles) {
    if (Test-Path $file) {
        Write-Host "   ✓ $(Split-Path $file -Leaf)" -ForegroundColor Green
    } else {
        Write-Host "   ✗ $(Split-Path $file -Leaf) NÃO ENCONTRADO!" -ForegroundColor Red
        $allFilesPresent = $false
    }
}

if (-not $allFilesPresent) {
    Write-Host "ERRO: Arquivos críticos estão faltando!" -ForegroundColor Red
    exit 1
}

# 3. CRIAR ARQUIVO ZIP
Write-Host ""
Write-Host "3. Criando arquivo ZIP para deploy..." -ForegroundColor Green

# Remover zip antigo se existir
Remove-Item -Path ".\deploy.zip" -Force -ErrorAction SilentlyContinue

# Criar novo zip
Compress-Archive -Path .\publish\* -DestinationPath .\deploy.zip -Force
Write-Host "   ✓ Arquivo deploy.zip criado" -ForegroundColor Green

# Verificar tamanho do arquivo
$zipSize = (Get-Item ".\deploy.zip").Length / 1MB
Write-Host "   Tamanho do arquivo: $([math]::Round($zipSize, 2)) MB" -ForegroundColor Yellow

# 4. DEPLOY PARA AZURE (se não for build local)
if (-not $LocalBuild) {
    Write-Host ""
    Write-Host "4. Deploy para Azure App Service..." -ForegroundColor Green
    
    # Verificar se Azure CLI está instalado
    if (-not (Test-AzureCLI)) {
        Write-Host "   Azure CLI não encontrado!" -ForegroundColor Yellow
        Write-Host "   Para fazer deploy automático, instale o Azure CLI:" -ForegroundColor Yellow
        Write-Host "   https://aka.ms/installazurecli" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "   Arquivo deploy.zip foi criado. Você pode fazer upload manual:" -ForegroundColor Green
        Write-Host "   1. Acesse o Azure Portal" -ForegroundColor White
        Write-Host "   2. Vá para seu App Service" -ForegroundColor White
        Write-Host "   3. Em 'Development Tools' → 'Advanced Tools' → 'Go'" -ForegroundColor White
        Write-Host "   4. Em 'Debug console' → 'CMD' ou 'PowerShell'" -ForegroundColor White
        Write-Host "   5. Navegue até 'site\wwwroot'" -ForegroundColor White
        Write-Host "   6. Arraste o arquivo deploy.zip para fazer upload" -ForegroundColor White
        Write-Host "   7. Extraia o conteúdo do ZIP" -ForegroundColor White
    } else {
        # Se Resource Group e App Name não foram fornecidos, solicitar
        if ([string]::IsNullOrEmpty($ResourceGroup)) {
            $ResourceGroup = Read-Host "Digite o nome do Resource Group"
        }
        if ([string]::IsNullOrEmpty($AppServiceName)) {
            $AppServiceName = Read-Host "Digite o nome do App Service"
        }
        
        Write-Host "   Fazendo login no Azure..." -ForegroundColor Yellow
        az login --only-show-errors
        
        Write-Host "   Enviando aplicação para Azure..." -ForegroundColor Yellow
        az webapp deployment source config-zip `
            --resource-group $ResourceGroup `
            --name $AppServiceName `
            --src .\deploy.zip `
            --only-show-errors
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "   ✓ Deploy concluído com sucesso!" -ForegroundColor Green
            
            Write-Host ""
            Write-Host "5. Configurando App Settings..." -ForegroundColor Green
            
            # Configurar variáveis de ambiente
            az webapp config appsettings set `
                --resource-group $ResourceGroup `
                --name $AppServiceName `
                --settings `
                    ASPNETCORE_ENVIRONMENT=Production `
                    WEBSITE_RUN_FROM_PACKAGE=0 `
                    SCM_DO_BUILD_DURING_DEPLOYMENT=false `
                    ASPNETCORE_DETAILEDERRORS=true `
                --only-show-errors
            
            Write-Host "   ✓ Configurações aplicadas" -ForegroundColor Green
            
            # Reiniciar o App Service
            Write-Host ""
            Write-Host "6. Reiniciando App Service..." -ForegroundColor Green
            az webapp restart --resource-group $ResourceGroup --name $AppServiceName --only-show-errors
            
            Write-Host "   ✓ App Service reiniciado" -ForegroundColor Green
            
            # Mostrar URL
            $appUrl = az webapp show --resource-group $ResourceGroup --name $AppServiceName --query "defaultHostName" -o tsv
            Write-Host ""
            Write-Host "========================================" -ForegroundColor Cyan
            Write-Host "  Deploy Concluído com Sucesso!  " -ForegroundColor Green
            Write-Host "========================================" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "URLs para teste:" -ForegroundColor Yellow
            Write-Host "  Principal:    https://$appUrl" -ForegroundColor White
            Write-Host "  Teste Simples: https://$appUrl/simple" -ForegroundColor White
            Write-Host "  Health Check:  https://$appUrl/health/ping" -ForegroundColor White
            Write-Host "  API Test:      https://$appUrl/test" -ForegroundColor White
            Write-Host ""
            Write-Host "Se ainda houver erro 403, verifique:" -ForegroundColor Yellow
            Write-Host "  1. Log Stream no Azure Portal" -ForegroundColor White
            Write-Host "  2. Application Insights (se configurado)" -ForegroundColor White
            Write-Host "  3. Kudu Console: https://$($AppServiceName).scm.azurewebsites.net" -ForegroundColor White
            
        } else {
            Write-Host "   ✗ Erro no deploy!" -ForegroundColor Red
            Write-Host "   Verifique as credenciais e tente novamente" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Build Local Concluído!  " -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Arquivo deploy.zip criado com sucesso!" -ForegroundColor Green
    Write-Host "Use este arquivo para fazer deploy manual no Azure Portal" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Script concluído!" -ForegroundColor Green
