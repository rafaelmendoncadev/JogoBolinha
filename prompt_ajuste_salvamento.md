# Prompt: Ajustes de Salvamento e Adoção de GameStates

Objetivo: garantir que o progresso seja salvo e visível para o usuário, adotando jogos criados anonimamente quando o usuário interagir, adicionando AutoSave no front‑end e, opcionalmente, reparando GameStates órfãos no banco.

1) Aplicar helper de autorização/adoção no `GameController`
- Adicionar o helper abaixo no `GameController` (após `CreateGameViewModel`):

```
private async Task<GameState?> GetAuthorizedGameStateAsync(int gameStateId, int? playerId, bool includeDetails = false)
{
    IQueryable<GameState> q = _context.GameStates;
    if (includeDetails)
    {
        q = q.Include(gs => gs.Level)
             .Include(gs => gs.Tubes).ThenInclude(t => t.Balls)
             .Include(gs => gs.Moves);
    }
    var gs = await q.FirstOrDefaultAsync(gs => gs.Id == gameStateId);
    if (gs == null) return null;

    if (gs.PlayerId.HasValue && playerId.HasValue && gs.PlayerId.Value != playerId.Value)
        return null; // não é o dono

    if (!gs.PlayerId.HasValue && playerId.HasValue)
    {
        gs.PlayerId = playerId.Value; // adoção
        await _context.SaveChangesAsync();
    }
    return gs;
}
```

- Substituir buscas diretas por Id+PlayerId para usar o helper nas ações abaixo:
  - `Continue` (includeDetails: true), `MakeMove`, `UndoMove`, `AutoSave`, `UndoMultipleMoves` (includeDetails: true), `RedoMove` (includeDetails: true), `RestartLevel` (includeDetails: true).
- Remover condicionais do tipo `(playerId == null || gs.PlayerId == playerId)` das queries.
- Compilar: `dotnet build JogoBolinha/JogoBolinha.csproj -c Release`.

2) Adicionar AutoSave leve no front‑end
- Em `JogoBolinha/Views/Game/Game.cshtml`, próximo do `const gameStateId = @Model.GameState.Id;`, inserir:

```
setInterval(() => {
  $.post('@Url.Action("AutoSave", "Game")', { gameStateId: gameStateId });
}, 30000);
```

3) Script de reparo (opcional) para GameStates órfãos
- Criar utilitário no `JogoBolinha.Migration` para atribuir `PlayerId` best‑effort:

```
var orfaos = await ctx.GameStates.Where(gs => gs.PlayerId == null).ToListAsync();
foreach (var gs in orfaos)
{
    var sess = await ctx.GameSessions
        .Where(s => s.LevelId == gs.LevelId && s.PlayerId != null)
        .OrderByDescending(s => s.StartTime)
        .FirstOrDefaultAsync();
    if (sess != null && Math.Abs((sess.StartTime - gs.StartTime).TotalDays) <= 1)
        gs.PlayerId = sess.PlayerId;
}
await ctx.SaveChangesAsync();
```

4) Validação
- SQL rápido:

```
sqlite3 JogoBolinha/jogabolinha.db "SELECT Id, PlayerId, Status, MovesCount, datetime(COALESCE(LastModified, StartTime)) FROM GameStates ORDER BY COALESCE(LastModified, StartTime) DESC LIMIT 10;"
```

- Testes: jogo logado (com 1 movimento), jogo anônimo adotado ao continuar/mover, e AutoSave atualizando `LastModified` após ~30s.

5) Reversão
- Remover helper e voltar às queries originais; retirar o `setInterval` do AutoSave.

