# 🔥 Solução para Erro 403 (Forbidden) no Azure App Service

## 📋 Resumo das Correções Aplicadas

### ✅ 1. **web.config** - Corrigido
- Caminho dos logs ajustado para Azure (`\\?\%home%\LogFiles\stdout`)
- Removidos módulos problemáticos (WebDAV, FormsAuthentication)
- Adicionadas permissões explícitas para todos os verbos HTTP
- Habilitado modo de erro detalhado para diagnóstico
- Configuração de conteúdo estático para arquivos JSON

### ✅ 2. **View de Teste** - Criada
- Criada view `SimpleIndex.cshtml` para o `SimpleHomeController`
- Página simples sem dependências para testar se o MVC está funcionando

### ✅ 3. **Program.cs** - Ajustado
- Removido temporariamente o redirecionamento HTTPS em produção
- Adicionado logging detalhado para diagnóstico
- Melhor tratamento de erros na criação do banco de dados
- Adicionado endpoint `/test` para verificação rápida

### ✅ 4. **Script de Deploy** - Criado
- Script PowerShell `deploy-azure.ps1` para automatizar o deploy
- Inclui verificação de arquivos críticos
- Configura automaticamente as variáveis de ambiente

## 🚀 Como Fazer o Deploy Correto

### Opção 1: Usando o Script PowerShell (Recomendado)

```powershell
# Na raiz do projeto, execute:
.\deploy-azure.ps1 -ResourceGroup "seu-resource-group" -AppServiceName "seu-app-name"

# Ou para build local apenas:
.\deploy-azure.ps1 -LocalBuild
```

### Opção 2: Deploy Manual

1. **Compile o projeto:**
```powershell
dotnet publish JogoBolinha\JogoBolinha.csproj -c Release -o ./publish
```

2. **Crie o arquivo ZIP:**
```powershell
Compress-Archive -Path .\publish\* -DestinationPath deploy.zip
```

3. **Faça o upload via Kudu:**
   - Acesse: `https://seu-app.scm.azurewebsites.net`
   - Vá em Debug Console → PowerShell
   - Navegue até `D:\home\site\wwwroot`
   - Arraste o arquivo `deploy.zip`
   - Execute: `Expand-Archive -Path deploy.zip -DestinationPath . -Force`

## ⚙️ Configurações OBRIGATÓRIAS no Azure Portal

### 1. Application Settings
No Azure Portal, vá para seu App Service → Configuration → Application Settings e adicione:

| Nome | Valor | Descrição |
|------|-------|-----------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Define ambiente de produção |
| `WEBSITE_RUN_FROM_PACKAGE` | `0` | Permite escrita no sistema de arquivos |
| `SCM_DO_BUILD_DURING_DEPLOYMENT` | `false` | Desabilita build automático (já fazemos local) |
| `ASPNETCORE_DETAILEDERRORS` | `true` | Mostra erros detalhados (remover após resolver) |
| `WEBSITE_ENABLE_SYNC_UPDATE_SITE` | `true` | Sincroniza mudanças imediatamente |

### 2. General Settings
- **Stack**: .NET
- **Version**: .NET 8
- **Platform**: 64 Bit
- **Managed Pipeline**: Integrated
- **HTTP Version**: 2.0
- **Always On**: On (se disponível no seu plano)

## 🧪 URLs de Teste (Execute nesta ordem)

Após o deploy, teste estas URLs para verificar o funcionamento:

1. **Health Check Simples (JSON)**
   ```
   https://seu-app.azurewebsites.net/health/ping
   ```
   Deve retornar: `{"status":"healthy","timestamp":"...","environment":"Production"}`

2. **Página de Teste Simples**
   ```
   https://seu-app.azurewebsites.net/simple
   ```
   Deve mostrar uma página HTML com informações do sistema

3. **Endpoint de API Test**
   ```
   https://seu-app.azurewebsites.net/test
   ```
   Deve retornar JSON: `{"status":"OK","time":"...","environment":"Production"}`

4. **Página Principal**
   ```
   https://seu-app.azurewebsites.net/
   ```
   Se as anteriores funcionarem, esta deve funcionar também

## 🔍 Como Diagnosticar se Ainda Houver Problemas

### 1. Verificar Logs em Tempo Real
```powershell
# Via Azure CLI
az webapp log tail --resource-group seu-rg --name seu-app

# Ou no Portal Azure
App Service → Monitoring → Log stream
```

### 2. Verificar se os Arquivos Foram Publicados
- Acesse Kudu: `https://seu-app.scm.azurewebsites.net`
- Vá em Debug Console → CMD
- Navegue até `D:\home\site\wwwroot`
- Verifique se existem:
  - `JogoBolinha.dll`
  - `web.config`
  - `appsettings.Production.json`
  - Pasta `Views`
  - Pasta `wwwroot`

### 3. Verificar Logs de Stdout
- Em Kudu, navegue até `D:\home\LogFiles`
- Procure por arquivos `stdout_*.log`
- Estes contêm os logs detalhados da aplicação

### 4. Testar Localmente com Configuração de Produção
```powershell
# Define ambiente como Production
$env:ASPNETCORE_ENVIRONMENT="Production"

# Executa a aplicação
dotnet run --project JogoBolinha\JogoBolinha.csproj --configuration Release

# Teste em: http://localhost:5000
```

## ❌ Problemas Comuns e Soluções

### Erro 403 Persiste
1. **Verifique o web.config**: Certifique-se que foi publicado corretamente
2. **Limpe o cache**: No Azure Portal → App Service → Restart
3. **Verifique permissões**: Application Settings devem estar corretas
4. **Teste sem HTTPS**: Acesse com `http://` ao invés de `https://`

### Erro 500 (Internal Server Error)
1. Verifique os logs de stdout para exceções
2. Confirme que o banco SQLite pode ser criado
3. Verifique `appsettings.Production.json`

### Assets (CSS/JS) não carregam
1. Confirme que a pasta `wwwroot` foi publicada
2. Verifique se `UseStaticFiles()` está no Program.cs
3. Limpe o cache do navegador

### Banco de dados não funciona
1. O caminho deve ser `D:\home\site\wwwroot\jogabolinha.db`
2. O App Service precisa permissão de escrita
3. `WEBSITE_RUN_FROM_PACKAGE` deve ser `0`

## 📊 Checklist de Verificação

- [ ] web.config publicado e configurado corretamente
- [ ] Application Settings configuradas no Azure Portal
- [ ] Deploy feito com todos os arquivos necessários
- [ ] `/health/ping` retorna JSON válido
- [ ] `/simple` mostra página HTML
- [ ] Logs não mostram erros críticos
- [ ] Banco de dados pode ser criado/acessado
- [ ] Static files (CSS/JS) carregam corretamente

## 🆘 Se Nada Funcionar

1. **Crie um novo App Service** do zero
2. **Use o template padrão** do .NET 8
3. **Faça deploy incremental**: primeiro só o básico, depois adicione features
4. **Considere usar GitHub Actions** para CI/CD automático
5. **Abra um ticket** com o suporte da Microsoft Azure

## 📞 Comandos Úteis

```powershell
# Reiniciar App Service
az webapp restart --resource-group seu-rg --name seu-app

# Ver status
az webapp show --resource-group seu-rg --name seu-app

# Listar configurações
az webapp config appsettings list --resource-group seu-rg --name seu-app

# Ver logs em tempo real
az webapp log tail --resource-group seu-rg --name seu-app

# Fazer deploy do ZIP
az webapp deployment source config-zip --resource-group seu-rg --name seu-app --src deploy.zip
```

## ✅ Próximos Passos

1. **Execute o script de deploy**: `.\deploy-azure.ps1`
2. **Configure as Application Settings** no Azure Portal
3. **Teste as URLs** na ordem indicada
4. **Verifique os logs** se houver problemas
5. **Remova `ASPNETCORE_DETAILEDERRORS`** após resolver

---

💡 **Dica:** Sempre teste primeiro o `/health/ping` - se ele funcionar, o problema está na aplicação, não na infraestrutura.
