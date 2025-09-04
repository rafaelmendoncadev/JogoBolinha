# Guia de Deploy para Azure App Service

## Problema Comum: "You do not have permission to view this directory or page"

Este erro geralmente indica problemas de configuração no deployment. Siga este guia para corrigir:

## ✅ Arquivos Criados/Configurados

### 1. web.config (ATUALIZADO para erro 403)
- **Localização**: `/JogoBolinha/web.config`
- **Propósito**: Configura IIS para ASP.NET Core (versão simplificada)
- **Importante**: 
  - Configuração básica sem restrições excessivas
  - `stdoutLogEnabled="true"` para debugging
  - `httpErrors errorMode="Detailed"` para diagnóstico
  - Remoção de módulos restritivos (WebDAV)

### 2. appsettings.Production.json
- **Localização**: `/JogoBolinha/appsettings.Production.json`
- **Propósito**: Configurações específicas para produção
- **Important**: Path correto do SQLite na Azure (`D:\\home\\site\\wwwroot\\`)

### 3. .deployment
- **Localização**: `/.deployment`
- **Propósito**: Informa ao Azure qual projeto publicar

### 4. Program.cs Atualizado
- **Adicionado**: Inicialização automática do banco
- **Adicionado**: Geração de níveis iniciais
- **Adicionado**: Tratamento de erros na startup

## 🚀 Passos para Deploy Correto

### Opção 1: Via Azure Portal (Recomendado)

1. **Acesse o Azure Portal**
   - Vá para o seu App Service
   - Navegue até **Deployment Center**

2. **Configurar Source Control**
   ```
   Source: GitHub/Local Git
   Build Provider: App Service Build Service
   ```

3. **Configurar Application Settings**
   ```
   ASPNETCORE_ENVIRONMENT = Production
   WEBSITE_RUN_FROM_PACKAGE = 0
   SCM_DO_BUILD_DURING_DEPLOYMENT = true
   ```

4. **Fazer Deploy**
   - Push do código para o repositório configurado
   - Azure irá automaticamente fazer build e deploy

### Opção 2: Via Visual Studio

1. **Publish Profile**
   - Clique direito no projeto → Publish
   - Escolha "Azure App Service"
   - Selecione seu App Service

2. **Configurações de Publish**
   ```
   Configuration: Release
   Target Framework: net8.0
   Deployment Mode: Framework Dependent
   ```

### Opção 3: Via Azure CLI

```bash
# 1. Fazer build local
dotnet build --configuration Release

# 2. Publicar
dotnet publish --configuration Release --output ./publish

# 3. Fazer zip dos arquivos
# (Windows)
Compress-Archive -Path .\publish\* -DestinationPath deploy.zip

# 4. Deploy via Azure CLI
az webapp deployment source config-zip --resource-group seu-resource-group --name seu-app-name --src deploy.zip
```

## 🔧 Configurações Importantes no Azure

### App Settings (no Portal Azure)
```
ASPNETCORE_ENVIRONMENT = Production
WEBSITE_RUN_FROM_PACKAGE = 0
SCM_DO_BUILD_DURING_DEPLOYMENT = true
WEBSITE_ENABLE_SYNC_UPDATE_SITE = true
```

### Configuração de Logs
```
ASPNETCORE_LOGGING__LOGLEVEL__DEFAULT = Information
ASPNETCORE_LOGGING__LOGLEVEL__MICROSOFT = Warning
```

## 🗂️ Estrutura de Arquivos Após Deploy
```
D:\home\site\wwwroot\
├── JogoBolinha.dll
├── JogoBolinha.deps.json
├── JogoBolinha.runtimeconfig.json
├── web.config
├── appsettings.json
├── appsettings.Production.json
├── wwwroot/
│   ├── css/
│   ├── js/
│   └── lib/
├── Views/
└── jogabolinha.db (será criado automaticamente)
```

## 🔍 Troubleshooting

### 1. Erro 403 Forbidden / "You do not have permission to view this directory or page"
- ✅ **Verificar web.config**: Deve ter configuração básica sem restrições excessivas
- ✅ **App Settings**: `ASPNETCORE_ENVIRONMENT = Production`
- ✅ **Testar endpoints simples**: `/health/ping` ou `/simple` primeiro
- ✅ **Verificar logs**: Use Application Insights ou Log Stream
- ✅ **Tentar sem HTTPS**: Temporariamente remover redirecionamento HTTPS
- ✅ **Verificar módulos IIS**: WebDAV pode causar conflitos

### 2. Erro de Banco de Dados
- ✅ Verificar path no `appsettings.Production.json`
- ✅ Confirmar que o banco será criado automaticamente
- ✅ Verificar logs da aplicação

### 3. Assets Não Carregam (CSS/JS)
- ✅ Verificar se pasta `wwwroot` foi publicada
- ✅ Confirmar configuração de `UseStaticFiles()` no Program.cs
- ✅ Verificar configurações HTTPS

### 4. Como Verificar Logs
```bash
# Via Azure CLI
az webapp log tail --resource-group seu-resource-group --name seu-app-name

# Ou no Portal Azure
App Service → Monitoring → Log stream
```

## ⚠️ Pontos Importantes

1. **SQLite Path**: O caminho do banco em produção é diferente (`D:\\home\\site\\wwwroot\\`)

2. **HTTPS**: O web.config força redirecionamento HTTPS (exceto localhost)

3. **Inicialização**: O banco e níveis são criados automaticamente na primeira execução

4. **Logs**: Em caso de erro, sempre verificar os logs do App Service

## 🎯 Próximos Passos

1. **Fazer novo deploy** com as configurações atualizadas
2. **Verificar App Settings** no Azure Portal
3. **Testar a aplicação** após deploy
4. **Verificar logs** se ainda houver problemas

## 📞 Comandos Úteis

```bash
# Verificar status do App Service
az webapp show --resource-group seu-resource-group --name seu-app-name

# Reiniciar App Service
az webapp restart --resource-group seu-resource-group --name seu-app-name

# Ver configurações
az webapp config appsettings list --resource-group seu-resource-group --name seu-app-name
```