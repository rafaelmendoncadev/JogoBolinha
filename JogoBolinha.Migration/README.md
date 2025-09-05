# Script de Migração de Níveis - Fase 3

Este script implementa a **Fase 3** da refatoração do sistema de níveis conforme especificado no PRD.

## Funcionalidades

### 🔄 **Conversão de Formatos**
- **JSON → Compacto**: Converte níveis do formato JSON legado para o formato compacto
- **Detecção Automática**: Identifica automaticamente o formato de cada nível
- **Validação**: Verifica a solucionabilidade de todos os níveis

### 🛡️ **Segurança e Backup**
- **Backup Automático**: Cria backup do banco antes da migração
- **Validação Completa**: Testa cada nível antes de aplicar mudanças
- **Logging Detalhado**: Registra todas as operações

### 🔧 **Regeneração Inteligente**
- **Níveis Inválidos**: Regenera automaticamente níveis que não são solucionáveis
- **Parâmetros Atualizados**: Aplica as novas especificações de dificuldade
- **Seeds de Geração**: Adiciona seeds para reproduzibilidade

## Como Executar

### Pré-requisitos
- .NET 8.0 SDK
- Banco SQLite com níveis existentes

### Comando
```bash
cd JogoBolinha.Migration
dotnet run
```

## O Que o Script Faz

### 1. **Backup** 📦
Cria um backup timestamped do banco atual:
```
jogabolinha_backup_YYYYMMDD_HHMMSS.db
```

### 2. **Análise** 🔍
Para cada nível existente:
- Detecta o formato (Compacto vs JSON)
- Valida a solucionabilidade
- Categoriza a ação necessária

### 3. **Processamento** ⚙️
- **Formato Compacto + Válido**: ✅ Mantém como está
- **Formato JSON + Válido**: 🔄 Converte para compacto
- **Qualquer + Inválido**: 🔄 Regenera completamente

### 4. **Estatísticas** 📊
Relatório final mostra:
- Níveis já no formato compacto
- Níveis convertidos
- Níveis regenerados
- Taxa de sucesso geral

## Exemplo de Execução

```
=== SCRIPT DE MIGRAÇÃO DE NÍVEIS - FASE 3 ===
Refatoração do Sistema de Geração de Níveis

✅ Backup criado: jogabolinha_backup_20250904_225409.db
📊 Encontrados 10 níveis para migração

--- Processando Nível 1 ---
Nível 1: Já está no formato compacto ✓
Nível 1: Válido ✓

--- Processando Nível 2 ---
Nível 2: Formato JSON detectado - Convertendo...
Nível 2: Convertido com sucesso ✓

--- Processando Nível 3 ---
Nível 3: Inválido - Regenerando...
Nível 3: Regenerado com sucesso ✓

=== RELATÓRIO FINAL DE MIGRAÇÃO ===
📊 Níveis já no formato compacto: 5
🔄 Níveis convertidos de JSON: 3
🔄 Níveis regenerados: 2
✅ Níveis válidos: 10
❌ Níveis com erro: 0
📈 Total processado: 10
🎯 Taxa de sucesso: 100,00%

=== MIGRAÇÃO CONCLUÍDA COM SUCESSO ===
```

## Formatos Suportados

### ❌ **Formato JSON (Legacy)**
```json
{
  "Tubes": [
    {
      "Id": 0,
      "Balls": [
        {"Color": "#FF6B6B", "Position": 0},
        {"Color": "#4ECDC4", "Position": 1}
      ]
    },
    {"Id": 1, "Balls": []},
    {"Id": 2, "Balls": []}
  ]
}
```

### ✅ **Formato Compacto (Novo)**
```
T1=0,1;T2=;T3=
```

**Benefícios do formato compacto:**
- 🚀 **80% menor** em tamanho
- ⚡ **Parsing mais rápido**
- 🔧 **Mais fácil de debugar**

## Configuração

Arquivo `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=../JogoBolinha/jogabolinha.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## Tratamento de Erros

O script é robusto e trata os seguintes cenários:
- ❌ **JSON malformado**: Regenera o nível
- ❌ **Nível inválido**: Regenera com novos parâmetros
- ❌ **Erro de conversão**: Fallback para regeneração
- ❌ **Erro de banco**: Rollback automático

## Logs

O script gera logs detalhados:
- 📋 **Console**: Progresso visual
- 📝 **Logger**: Logs estruturados para debugging
- 📊 **Estatísticas**: Métricas de sucesso

## Rollback

Se necessário fazer rollback:
```bash
# Parar a aplicação
# Restaurar o backup
cp jogabolinha_backup_YYYYMMDD_HHMMSS.db ../JogoBolinha/jogabolinha.db
```

## Verificação Pós-Migração

Após a migração, execute os testes para validar:
```bash
cd ../JogoBolinha.Tests
dotnet test
```

## Próximos Passos

Após a migração bem-sucedida:
1. ✅ **Fase 3 Completa**: Migração executada
2. 🎯 **Fase 4**: Testes e validação final
3. 🚀 **Deploy**: Aplicar em produção