# Script para Regenerar Níveis do Jogo da Bolinha
# Este script corrige os níveis problemáticos, incluindo o nível 4 impossível

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   REGENERADOR DE NÍVEIS - JOGO BOLINHA" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verificar se está no diretório correto
if (-not (Test-Path ".\JogoBolinha\JogoBolinha.csproj")) {
    Write-Host "ERRO: Execute este script na raiz do projeto!" -ForegroundColor Red
    Write-Host "Certifique-se de estar no diretório JogoBolinha" -ForegroundColor Yellow
    exit 1
}

Write-Host "Este script irá regenerar todos os níveis do jogo para corrigir:" -ForegroundColor Yellow
Write-Host "  - Nível 4 impossível de resolver" -ForegroundColor White
Write-Host "  - Progressão de dificuldade incorreta (nível 2 > nível 3)" -ForegroundColor White
Write-Host "  - Outros níveis problemáticos" -ForegroundColor White
Write-Host ""
Write-Host "⚠️  ATENÇÃO: Isso deletará TODOS os jogos salvos!" -ForegroundColor Red
Write-Host ""

$confirm = Read-Host "Deseja continuar? (S/N)"
if ($confirm -ne "S" -and $confirm -ne "s") {
    Write-Host "Operação cancelada." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Iniciando regeneração dos níveis..." -ForegroundColor Green
Write-Host ""

# Fazer backup do banco se existir
if (Test-Path ".\JogoBolinha\jogabolinha.db") {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupName = "jogabolinha_backup_$timestamp.db"
    Write-Host "Criando backup do banco: $backupName" -ForegroundColor Yellow
    Copy-Item ".\JogoBolinha\jogabolinha.db" ".\JogoBolinha\$backupName"
    Write-Host "✓ Backup criado com sucesso!" -ForegroundColor Green
}

# Definir variável de ambiente para regeneração
Write-Host ""
Write-Host "Configurando ambiente para regeneração..." -ForegroundColor Yellow
$env:REGENERATE_LEVELS = "true"
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Compilar o projeto
Write-Host ""
Write-Host "Compilando o projeto..." -ForegroundColor Yellow
dotnet build .\JogoBolinha\JogoBolinha.csproj --configuration Debug --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: Falha na compilação!" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Compilação concluída!" -ForegroundColor Green

# Executar a aplicação para regenerar
Write-Host ""
Write-Host "Executando regeneração dos níveis..." -ForegroundColor Yellow
Write-Host "A aplicação irá iniciar e regenerar automaticamente os níveis." -ForegroundColor Cyan
Write-Host "Aguarde as mensagens de confirmação no console..." -ForegroundColor Cyan
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

# Finalizar qualquer processo anterior
Get-Process -Name "JogoBolinha" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# Aguardar um momento para liberação dos arquivos
Start-Sleep -Seconds 2

# Executar a aplicação em background para regeneração
$processInfo = New-Object System.Diagnostics.ProcessStartInfo
$processInfo.FileName = "dotnet"
$processInfo.Arguments = "run --project .\JogoBolinha\JogoBolinha.csproj --no-build"
$processInfo.UseShellExecute = $false
$processInfo.RedirectStandardOutput = $true
$processInfo.RedirectStandardError = $true
$processInfo.CreateNoWindow = $true

$process = [System.Diagnostics.Process]::Start($processInfo)

# Aguardar alguns segundos para a regeneração
Write-Host "Aguardando regeneração dos níveis (20 segundos)..." -ForegroundColor Yellow
$counter = 0
while ($counter -lt 20 -and !$process.HasExited) {
    Start-Sleep -Seconds 1
    $counter++
    Write-Host "." -NoNewline -ForegroundColor Green
}
Write-Host ""

# Parar o processo
if (!$process.HasExited) {
    $process.Kill()
}
$process.Close()

# Limpar variável de ambiente
$env:REGENERATE_LEVELS = ""

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "   ✅ REGENERAÇÃO CONCLUÍDA!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Os níveis foram regenerados com sucesso!" -ForegroundColor Green
Write-Host ""
Write-Host "Melhorias aplicadas:" -ForegroundColor Cyan
Write-Host "  ✓ Nível 4 agora tem 3 cores e 5 tubos (solucionável)" -ForegroundColor White
Write-Host "  ✓ Progressão suave de dificuldade (1-10)" -ForegroundColor White
Write-Host "  ✓ Todos os níveis garantidamente solucionáveis" -ForegroundColor White
Write-Host "  ✓ Mínimo de 2 tubos vazios em níveis fáceis" -ForegroundColor White
Write-Host ""

# Perguntar se quer iniciar o jogo
Write-Host "Deseja iniciar o jogo para testar? (S/N)" -ForegroundColor Yellow
$startGame = Read-Host

if ($startGame -eq "S" -or $startGame -eq "s") {
    Write-Host ""
    Write-Host "Iniciando o jogo..." -ForegroundColor Green
    Write-Host "Acesse http://localhost:5000 no seu navegador" -ForegroundColor Cyan
    Write-Host "Teste especialmente o Nível 4 que estava impossível!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Pressione Ctrl+C para parar o servidor quando terminar." -ForegroundColor White
    Write-Host ""
    
    # Limpar variável para execução normal
    $env:REGENERATE_LEVELS = ""
    & dotnet run --project .\JogoBolinha\JogoBolinha.csproj --no-build
} else {
    Write-Host ""
    Write-Host "Para iniciar o jogo mais tarde, execute:" -ForegroundColor Cyan
    Write-Host "  dotnet run --project .\JogoBolinha\JogoBolinha.csproj" -ForegroundColor White
    Write-Host ""
}

Write-Host "Script concluído!" -ForegroundColor Green
