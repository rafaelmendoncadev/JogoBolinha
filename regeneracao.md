# Prompt de Regeneração de Níveis (JogoBolinha)

Objetivo: regenerar os níveis do jogo com a lógica V2, garantindo progressão correta (nível 2 mais fácil que o 3) e solvabilidade, e validar que o nível 2 está no layout tutorial fixo. Execute os passos exatamente nesta ordem.

Instruções (para um agente no terminal, Windows/PowerShell):

1) Preparar ambiente
- Sete a variável de ambiente para forçar a regeneração e alinhar níveis iniciais:

```
$env:REGENERATE_LEVELS = "true"
```

2) Compilar
- Compile o projeto principal em Release:

```
dotnet build JogoBolinha/JogoBolinha.csproj -c Release
```

3) Executar para disparar o alinhamento/regeneração
- Rode o aplicativo uma vez para que o bloco de startup aplique a migração e alinhe níveis 1–20 (inclui apagar GameStates dos níveis ajustados):

```
dotnet run --project JogoBolinha/JogoBolinha.csproj -c Release
```

- Aguarde iniciar e finalizar a primeira requisição. Em ambiente local, pressione Ctrl+C após ver os logs de inicialização concluídos.

4) Validar o nível 2 e progressão
- Verifique no banco (SQLite) ou via endpoint de debug, se disponível:
  - Endpoint (se ativo): `GET /Debug/CheckLevel?levelNumber=2`
  - Esperado: `Colors=2, Tubes=5, BallsPerColor=2` e `InitialState = "T1=0,1;T2=1,0;T3=;T4=;T5="`.

- Alternativa por SQL (PowerShell + sqlite3):

```
sqlite3 .\JogoBolinha\jogabolinha.db "SELECT Number, Colors, Tubes, BallsPerColor, substr(InitialState,1,80) FROM Levels WHERE Number IN (2,3) ORDER BY Number;"
```

- Critérios de aceitação:
  - Nível 2 tem 2 cores, 2 bolas/cor, 5 tubos (3 vazios) e estado compacto tutorial: `T1=0,1;T2=1,0;T3=;T4=;T5=`.
  - Nível 3 tem 2 cores, 3 bolas/cor, 4 tubos (2 vazios), portanto mais desafiador que o 2.

5) Opcional: limpeza total (se necessário)
- Para começar do zero em dev: delete o arquivo do banco e repita os passos 2–4.

```
Remove-Item -Force .\JogoBolinha\jogabolinha.db -ErrorAction SilentlyContinue
```

Notas
- O alinhamento de níveis 1–20 ocorre no startup (Program.cs) usando LevelGeneratorServiceV2. Se um nível estiver divergente (cores/bolas/tubos) ou se o nível 2 não corresponder ao tutorial, ele será substituído e seus GameStates relacionados serão removidos.
- Se estiver em produção, prefira habilitar uma flag dedicada (ex.: REPAIR_EARLY_LEVELS) antes de rodar este procedimento. 

Saída esperada
- Logs de inicialização indicando migração/ajustes de níveis.
- Consulta ao nível 2 confirmando o layout fixo super fácil e o nível 3 mais desafiador.

---

# Prompt de Investigação e Correção (quando o nível 2 não fica no tutorial)

Objetivo: executar uma investigação rápida em 3 frentes e, se necessário, regenerar novamente com os ajustes.

Pré‑requisitos: PowerShell, `rg` (ripgrep, opcional), `sqlite3` (opcional), projeto compilável.

1) Verificar se o LevelGeneratorServiceV2 implementa o layout fixo para o nível 2

- Procurar o branch do nível 2 no gerador V2:

```
rg -n "levelNumber == 2" JogoBolinha/Services/LevelGeneratorServiceV2.cs
```

- Abrir o arquivo e confirmar que o método `GenerateLevel` retorna diretamente o estado compacto para `levelNumber == 2`:

Esperado no código:

```
if (levelNumber == 2) {
  var compact = "T1=0,1;T2=1,0;T3=;T4=;T5=";
  return new Level { Number = 2, Colors=2, Tubes=5, BallsPerColor=2, InitialState = compact, MinimumMoves = 2, ... };
}
```

- Compilar para garantir que o código atual está válido:

```
dotnet build JogoBolinha/JogoBolinha.csproj -c Release
```

2) Conferir se a lógica de “níveis problemáticos/alinhamento” captura o nível 2 atual

- Localizar o bloco de alinhamento dos níveis iniciais em `Program.cs`:

```
rg -n "Align early levels|Ajustando Nível" JogoBolinha/Program.cs
```

- Confirmar que a condição força substituição do nível 2 quando o `InitialState` não bate com o tutorial:

Condição esperada (pseudo):

```
needsReplace = existing == null
  || existing.Colors != expected.Colors
  || existing.BallsPerColor != expected.BallsPerColor
  || existing.Tubes < expected.Colors + 2
  || existing.Tubes < expected.Tubes
  || (n == 2 && existing.InitialState != expected.InitialState);
```

3) Re‑executar a regeneração com flags e validar

- Forçar regeneração e alinhamento completos (limpa estados e substitui níveis divergentes):

```
$env:REGENERATE_LEVELS = "true"
dotnet run --project JogoBolinha/JogoBolinha.csproj -c Release
```

- Validar no banco (necessita sqlite3):

```
sqlite3 .\JogoBolinha\jogabolinha.db "SELECT Number, Colors, Tubes, BallsPerColor, InitialState FROM Levels WHERE Number=2;"
```

- Esperado: `2 | 2 | 5 | 2 | T1=0,1;T2=1,0;T3=;T4=;T5=`

- Se ainda persistir o layout antigo, remova apenas o nível 2 e deixe o app recriar:

```
sqlite3 .\JogoBolinha\jogabolinha.db "DELETE FROM Levels WHERE Number=2;"
dotnet run --project JogoBolinha/JogoBolinha.csproj -c Release
```

- Opcional (com endpoint de debug habilitado):

```
curl http://localhost:5000/Debug/CheckLevel?levelNumber=2
```

Critérios de encerramento
- O nível 2 aparece com 2 cores, 2 bolas/cor, 5 tubos (3 vazios) e estado `T1=0,1;T2=1,0;T3=;T4=;T5=`.
- O nível 3 permanece com 2 cores, 3 bolas/cor, 4 tubos (2 vazios), mais desafiador que o 2.
