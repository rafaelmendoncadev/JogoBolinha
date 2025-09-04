# 🚂 Guia de Deploy para Railway - Ball Sort Puzzle

## Por que Railway?

Railway é uma plataforma de deployment moderna e simples que oferece:
- ✅ Deploy automático via Docker
- ✅ Domínio HTTPS gratuito
- ✅ Variáveis de ambiente fáceis
- ✅ Logs em tempo real
- ✅ Sem configuração complexa de IIS
- ✅ Plano gratuito disponível

## 📦 Arquivos Criados para Railway

### 1. Dockerfile
- **Multi-stage build** otimizado para .NET 8
- **SQLite** pré-configurado no container
- **Non-root user** para segurança
- **Suporte a PORT dinâmico** da Railway

### 2. .dockerignore
- Exclui arquivos desnecessários do build
- Otimiza tempo de build e tamanho da imagem

### 3. railway.json
- Configurações específicas da Railway
- Health check em `/health/ping`
- Política de restart automático

### 4. appsettings.Railway.json
- Configurações de produção para Railway
- Path do banco de dados no container
- Logging otimizado

### 5. Program.cs Atualizado
- **Detecção automática** do ambiente Railway
- **Port binding** dinâmico (`0.0.0.0:$PORT`)
- **Forwarded headers** para HTTPS
- **Logging detalhado** para debugging

## 🚀 Deploy Step-by-Step

### Método 1: GitHub + Railway (Recomendado)

#### Passo 1: Preparar Repositório
```bash
# Commit todos os arquivos
git add .
git commit -m "feat: prepare for Railway deployment"
git push origin main
```

#### Passo 2: Conectar Railway
1. Acesse [railway.app](https://railway.app)
2. Faça login com GitHub
3. Clique em **"New Project"**
4. Selecione **"Deploy from GitHub repo"**
5. Escolha seu repositório `JogoBolinha`

#### Passo 3: Configurar Variáveis (Automático)
Railway detectará automaticamente:
- ✅ `PORT` - Definido automaticamente
- ✅ `RAILWAY_ENVIRONMENT=production`
- ✅ Dockerfile será usado automaticamente

#### Passo 4: Deploy Automático
- Railway fará build e deploy automaticamente
- Você receberá uma URL: `https://seu-projeto.up.railway.app`

### Método 2: Railway CLI

#### Instalação
```bash
# Instalar Railway CLI
npm install -g @railway/cli

# Login
railway login
```

#### Deploy
```bash
# Na raiz do projeto
cd JogoBolinha

# Inicializar projeto Railway
railway init

# Fazer deploy
railway up
```

## 🔧 Configurações da Railway

### Variáveis de Ambiente (Opcionais)
Acesse o projeto na Railway → Settings → Variables:

```env
# Opcional: Forçar ambiente
ASPNETCORE_ENVIRONMENT=Production

# Opcional: Logging detalhado
ASPNETCORE_DETAILEDERRORS=true

# Opcional: Custom connection string
ConnectionStrings__DefaultConnection=Data Source=/app/data/jogabolinha.db
```

### Configurações do Serviço
- **Build Command**: Automático (Docker)
- **Start Command**: `dotnet JogoBolinha.dll`
- **Port**: Automático (Railway define)
- **Health Check**: `/health/ping`

## 🗂️ Estrutura no Container

```
/app/
├── JogoBolinha.dll          # Aplicação principal
├── appsettings.json         # Configurações base
├── appsettings.Railway.json # Configurações Railway
├── wwwroot/                 # Assets estáticos
│   ├── css/
│   ├── js/
│   └── lib/
└── data/
    └── jogabolinha.db       # Banco SQLite (criado automaticamente)
```

## 🔍 Troubleshooting

### 1. Deploy Falhando
**Verificar:**
- ✅ Dockerfile está na raiz do projeto
- ✅ .dockerignore configurado
- ✅ Build local funciona: `docker build .`

### 2. Aplicação Não Inicia
**Verificar logs:**
```bash
railway logs
```

**Problemas comuns:**
- Port não está sendo lido corretamente
- Banco de dados não consegue ser criado
- Dependências faltando

### 3. Banco de Dados Vazio
- O banco será criado automaticamente na primeira execução
- 50 níveis serão gerados automaticamente
- Logs mostrarão o processo de criação

### 4. Assets Não Carregam
**Verificar:**
- Pasta `wwwroot` no container
- Configuração `UseStaticFiles()` no Program.cs
- HTTPS/HTTP configuration

### 5. Como Ver Logs
```bash
# Railway CLI
railway logs

# Ou no Dashboard da Railway
Project → Deployments → View Logs
```

## 📊 Monitoramento

### Health Check
- **URL**: `https://seu-app.up.railway.app/health/ping`
- **Response**: `{"status":"healthy","timestamp":"...","environment":"Production"}`

### Página de Teste
- **URL**: `https://seu-app.up.railway.app/simple`
- **Purpose**: Teste básico sem dependências

### Logs Detalhados
Railway mostra automaticamente:
- Requests e responses
- Database operations
- Errors e exceptions
- Application startup

## 🚀 Vantagens da Railway vs Azure

| Aspecto | Railway | Azure App Service |
|---------|---------|------------------|
| **Simplicidade** | ✅ Muito fácil | ❌ Complexo |
| **Docker** | ✅ Nativo | ⚠️ Suporte limitado |
| **Logs** | ✅ Tempo real | ⚠️ Configuração complexa |
| **HTTPS** | ✅ Automático | ⚠️ Precisa configurar |
| **Preço** | ✅ Plano gratuito | ❌ Mais caro |
| **Deploy** | ✅ Git push = deploy | ❌ Configuração complexa |

## 🎯 Próximos Passos

1. **Fazer commit** de todos os arquivos
2. **Push para GitHub** se ainda não fez
3. **Conectar Railway** ao repositório
4. **Aguardar deploy** (3-5 minutos)
5. **Testar aplicação** na URL fornecida
6. **Verificar logs** se necessário

## 💡 Dicas Pro

1. **Auto-deploys**: Railway faz deploy automático a cada push
2. **Branch deploys**: Pode configurar deploys por branch
3. **Custom domain**: Adicione seu próprio domínio
4. **Environment variables**: Fácil de configurar no dashboard
5. **Resource monitoring**: Railway mostra uso de CPU/RAM
6. **Database backups**: Considere Railway Database para PostgreSQL

## 🆘 Suporte

- **Railway Docs**: [docs.railway.app](https://docs.railway.app)
- **Discord**: Railway tem comunidade ativa
- **GitHub Issues**: Para problemas específicos da aplicação

---

**🎮 Agora sua aplicação Ball Sort Puzzle estará rodando na Railway com HTTPS, domínio gratuito e deploy automático!**