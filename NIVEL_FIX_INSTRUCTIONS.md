# 🎮 Correção dos Níveis do Jogo da Bolinha

## 🐛 Problemas Identificados e Corrigidos

### 1. **Nível 4 Impossível**
- **Problema**: O nível 4 tinha apenas 3 tubos com cores misturadas de forma impossível de resolver
- **Solução**: Nova configuração com 5 tubos (3 cores, 2 vazios) garantindo solvabilidade

### 2. **Progressão de Dificuldade Incorreta**
- **Problema**: Nível 2 mais difícil que nível 3 devido à randomização
- **Solução**: Configuração determinística para níveis 1-20 com progressão suave

### 3. **Falta de Tubos Vazios**
- **Problema**: Níveis sem espaço suficiente para manobras
- **Solução**: Garantia de pelo menos 2 tubos vazios em níveis fáceis, 2-3 em médios

## ✅ Melhorias Implementadas

### LevelGeneratorServiceV2
- **Configuração Determinística**: Níveis 1-20 têm configurações fixas e testadas
- **Validação de Solvabilidade**: Algoritmo verifica se o nível é possível de resolver
- **Progressão Gradual**:
  - Níveis 1-3: 2 cores apenas (tutorial)
  - Níveis 4-5: Introdução da 3ª cor
  - Níveis 6-10: 3-4 cores com bastante espaço
  - Níveis 11-20: Aumento gradual de complexidade

### Progressão dos Primeiros 10 Níveis
```
Nível 1: 2 cores, 4 tubos (2 vazios), 2 bolas/cor - MUITO FÁCIL
Nível 2: 2 cores, 4 tubos (2 vazios), 3 bolas/cor - MUITO FÁCIL
Nível 3: 2 cores, 5 tubos (3 vazios), 3 bolas/cor - MUITO FÁCIL
Nível 4: 3 cores, 5 tubos (2 vazios), 3 bolas/cor - FÁCIL (CORRIGIDO!)
Nível 5: 3 cores, 6 tubos (3 vazios), 3 bolas/cor - FÁCIL
Nível 6: 3 cores, 5 tubos (2 vazios), 4 bolas/cor - FÁCIL
Nível 7: 3 cores, 6 tubos (3 vazios), 4 bolas/cor - FÁCIL
Nível 8: 4 cores, 6 tubos (2 vazios), 3 bolas/cor - MÉDIO-FÁCIL
Nível 9: 4 cores, 7 tubos (3 vazios), 3 bolas/cor - MÉDIO-FÁCIL
Nível 10: 4 cores, 6 tubos (2 vazios), 4 bolas/cor - MÉDIO
```

## 🚀 Como Aplicar as Correções

### Opção 1: Regeneração Automática na Inicialização (Recomendado)

1. **Defina a variável de ambiente antes de iniciar:**

```powershell
# PowerShell
$env:REGENERATE_LEVELS="true"
dotnet run --project JogoBolinha\JogoBolinha.csproj
```

```bash
# Bash/Linux/Mac
export REGENERATE_LEVELS=true
dotnet run --project JogoBolinha/JogoBolinha.csproj
```

2. **A aplicação irá automaticamente:**
   - Deletar todos os níveis antigos
   - Deletar todos os jogos salvos (necessário!)
   - Gerar 50 novos níveis com a lógica V2
   - Mostrar logs detalhados do processo

3. **Após a regeneração, remova a variável:**
```powershell
# PowerShell
$env:REGENERATE_LEVELS=""
```

### Opção 2: Via Painel Administrativo (Interface Web)

1. **Faça login como administrador**
   - Use uma conta com "admin" no nome de usuário

2. **Acesse o painel de gerenciamento:**
   ```
   http://localhost:5000/Admin/LevelManagement
   ```

3. **Escolha uma ação:**
   - **"Regenerar Níveis Problemáticos"** - Corrige apenas níveis com problemas detectados
   - **"Regenerar TODOS os Níveis"** - Recria todos os 50 níveis
   - **"Regenerar"** (individual) - Regenera um nível específico

4. **Teste os níveis:**
   - Clique em "Testar Níveis 1-10" para abrir os primeiros níveis

### Opção 3: Correção Manual de Níveis Específicos

Se quiser corrigir apenas níveis específicos sem deletar todos:

1. **Adicione no appsettings.json:**
```json
{
  "RegenerateLevels": false,
  "ProblematicLevelNumbers": [2, 4, 7] // Níveis a regenerar
}
```

2. **Execute a aplicação normalmente**

## 🧪 Testando as Correções

### Verificação Rápida

1. **Teste o Nível 4 especificamente:**
   - Deve ter 3 cores e 5 tubos agora
   - Deve ser solucionável com movimentos lógicos

2. **Teste a progressão 1-5:**
   - Nível 1-3: Muito fácil (2 cores)
   - Nível 4-5: Introdução gradual da 3ª cor

3. **Verifique no console os logs:**
```
[LEVEL GEN V2] Nível 1: 2 cores, 4 tubos (2 vazios), 2 bolas/cor
[LEVEL GEN V2] Nível 2: 2 cores, 4 tubos (2 vazios), 3 bolas/cor
[LEVEL GEN V2] Nível 3: 2 cores, 5 tubos (3 vazios), 3 bolas/cor
[LEVEL GEN V2] Nível 4: 3 cores, 5 tubos (2 vazios), 3 bolas/cor
...
```

### Validação Completa

Execute este comando PowerShell para validar todos os níveis:

```powershell
# Verificar solvabilidade de todos os níveis
for ($i = 1; $i -le 50; $i++) {
    Write-Host "Testando Nível $i..." -ForegroundColor Yellow
    # Abrir o nível no navegador
    Start-Process "http://localhost:5000/Game/PlayLevel?levelNumber=$i"
    Start-Sleep -Seconds 2
}
```

## ⚠️ Avisos Importantes

1. **Backup**: A regeneração deleta TODOS os jogos salvos. Faça backup se necessário!

2. **Azure/Produção**: Para regenerar em produção:
   ```bash
   # No Azure Portal, adicione a configuração:
   REGENERATE_LEVELS = true
   
   # Reinicie o App Service
   az webapp restart --resource-group seu-rg --name seu-app
   
   # Após regeneração, remova a configuração
   ```

3. **Verificação de Problemas**:
   - Se um nível continuar impossível, reporte com o número específico
   - Use o painel admin para regenerar individualmente

## 📊 Critérios de Validação

Um nível é considerado **válido** se:
- ✅ Tubos ≥ Cores + 2 (regra fundamental)
- ✅ Níveis 1-3: máximo 2 cores
- ✅ Níveis 4-10: máximo 4 cores
- ✅ Pelo menos 2 tubos vazios para manobras
- ✅ Não está pré-resolvido
- ✅ Cada cor tem o número correto de bolas

## 🎯 Resultado Esperado

Após a regeneração:
- **Nível 4**: Agora solucionável com 3 cores e 5 tubos
- **Progressão suave**: Dificuldade aumenta gradualmente
- **Sem deadlocks**: Todos os níveis têm solução garantida
- **Experiência melhorada**: Jogadores iniciantes não ficam frustrados

## 📞 Comandos Úteis

```powershell
# Regenerar e iniciar
$env:REGENERATE_LEVELS="true"; dotnet run --project JogoBolinha\JogoBolinha.csproj

# Testar nível específico
Start-Process "http://localhost:5000/Game/PlayLevel?levelNumber=4"

# Ver estatísticas dos níveis
Start-Process "http://localhost:5000/Admin/LevelManagement"

# Limpar banco de dados (CUIDADO!)
Remove-Item "JogoBolinha\jogabolinha.db"
```

## ✨ Melhorias Futuras

- [ ] Modo "Daily Challenge" com níveis especiais
- [ ] Sistema de hints mais inteligente baseado na dificuldade
- [ ] Níveis com mecânicas especiais (tubos bloqueados, cores especiais)
- [ ] Editor de níveis para comunidade

---

**🎮 Bom jogo! Os níveis agora estão balanceados e progressivos!**
