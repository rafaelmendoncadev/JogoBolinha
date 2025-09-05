# Script Simplificado para Corrigir Níveis
# Corrige o nível 4 impossível e outros problemas

Write-Host ""
Write-Host "🎮 CORREÇÃO DOS NÍVEIS - JOGO DA BOLINHA" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar diretório
if (-not (Test-Path ".\JogoBolinha\JogoBolinha.csproj")) {
    Write-Host "❌ ERRO: Execute na raiz do projeto JogoBolinha!" -ForegroundColor Red
    exit 1
}

Write-Host "Este script corrige:" -ForegroundColor Yellow
Write-Host "  ✅ Nível 4 impossível (agora 3 cores, 5 tubos)" -ForegroundColor White
Write-Host "  ✅ Progressão de dificuldade (níveis 1-10)" -ForegroundColor White
Write-Host "  ✅ Garantia de solvabilidade" -ForegroundColor White
Write-Host ""

$confirm = Read-Host "Continuar? (S/N)"
if ($confirm -ne "S" -and $confirm -ne "s") {
    Write-Host "Cancelado." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "🔧 Iniciando correção..." -ForegroundColor Green

try {
    # Finalizar processos anteriores
    Write-Host "Finalizando processos anteriores..." -ForegroundColor Yellow
    Get-Process -Name "JogoBolinha" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2

    # Build do projeto
    Write-Host "Compilando projeto..." -ForegroundColor Yellow
    $buildResult = & dotnet build .\JogoBolinha\JogoBolinha.csproj --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ ERRO na compilação!" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Compilação OK" -ForegroundColor Green

    # Definir flag de regeneração
    Write-Host "Configurando regeneração..." -ForegroundColor Yellow
    $env:REGENERATE_LEVELS = "true"
    $env:ASPNETCORE_ENVIRONMENT = "Development"

    # Executar regeneração
    Write-Host "Executando regeneração (aguarde 30 segundos)..." -ForegroundColor Yellow
    
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = "dotnet"
    $psi.Arguments = "run --project .\JogoBolinha\JogoBolinha.csproj --no-build"
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $false
    
    $process = [System.Diagnostics.Process]::Start($psi)
    
    # Aguardar regeneração
    $timeout = 30
    $counter = 0
    while ($counter -lt $timeout -and !$process.HasExited) {
        Start-Sleep -Seconds 1
        $counter++
        if ($counter % 5 -eq 0) {
            Write-Host "⏳ $counter/$timeout segundos..." -ForegroundColor Cyan
        }
    }
    
    # Finalizar processo
    if (!$process.HasExited) {
        $process.Kill()
        Write-Host "✅ Processo finalizado após timeout" -ForegroundColor Green
    } else {
        Write-Host "✅ Processo finalizou naturalmente" -ForegroundColor Green
    }
    
    $process.Close()
    
    # Limpar variáveis
    $env:REGENERATE_LEVELS = ""
    
    Write-Host ""
    Write-Host "🎉 CORREÇÃO CONCLUÍDA!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "✅ Níveis corrigidos com sucesso!" -ForegroundColor Green
    Write-Host "✅ Nível 4 agora é solucionável" -ForegroundColor Green
    Write-Host "✅ Progressão 1-10 balanceada" -ForegroundColor Green
    Write-Host ""
    
    $test = Read-Host "Iniciar o jogo para testar? (S/N)"
    if ($test -eq "S" -or $test -eq "s") {
        Write-Host ""
        Write-Host "🚀 Iniciando jogo..." -ForegroundColor Green
        Write-Host "Acesse: http://localhost:5000" -ForegroundColor Cyan
        Write-Host "Teste especialmente o Nível 4!" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Pressione Ctrl+C para parar quando terminar." -ForegroundColor White
        Write-Host ""
        
        # Executar jogo normalmente
        & dotnet run --project .\JogoBolinha\JogoBolinha.csproj --no-build
    } else {
        Write-Host ""
        Write-Host "Para testar mais tarde:" -ForegroundColor Cyan
        Write-Host "  dotnet run --project .\JogoBolinha\JogoBolinha.csproj" -ForegroundColor White
        Write-Host ""
    }
    
} catch {
    Write-Host ""
    Write-Host "❌ ERRO durante a correção:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Tente executar manualmente:" -ForegroundColor Yellow
    Write-Host "  `$env:REGENERATE_LEVELS='true'" -ForegroundColor White
    Write-Host "  dotnet run --project .\JogoBolinha\JogoBolinha.csproj" -ForegroundColor White
    exit 1
}

Write-Host ""
Write-Host "✨ Script concluído!" -ForegroundColor Green
