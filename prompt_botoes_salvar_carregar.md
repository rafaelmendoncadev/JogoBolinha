# Prompt: Adicionar botões "Salvar" e "Carregar" o jogo

Objetivo: dar ao usuário controle explícito para salvar o progresso atual e carregar um jogo salvo, via botões na tela do jogo e endpoints simples no `GameController`.

1) Backend – Endpoints
- Em `JogoBolinha/Controllers/GameController.cs`, adicione:

```
// Salvar explicitamente (alias do AutoSave)
[HttpPost]
public async Task<IActionResult> SaveGame(int gameStateId)
{
    var playerId = GetCurrentPlayerId();
    var gameState = await _context.GameStates.FirstOrDefaultAsync(gs => gs.Id == gameStateId);
    if (gameState == null) return Json(new { success = false, message = "Jogo não encontrado" });
    if (gameState.PlayerId.HasValue && playerId.HasValue && gameState.PlayerId != playerId.Value)
        return Json(new { success = false, message = "Não autorizado" });

    if (!gameState.PlayerId.HasValue && playerId.HasValue)
        gameState.PlayerId = playerId.Value; // adoção

    gameState.LastModified = DateTime.UtcNow;
    await _context.SaveChangesAsync();
    return Json(new { success = true, timestamp = gameState.LastModified });
}

// Listar jogos salvos do usuário logado
[HttpGet]
public async Task<IActionResult> ListSavedGames(int page = 1, int pageSize = 10)
{
    var playerId = GetCurrentPlayerId();
    if (!playerId.HasValue)
        return Json(new { success = false, message = "Usuário não autenticado" });

    page = Math.Max(1, page);
    pageSize = Math.Min(Math.Max(1, pageSize), 50);

    var query = _context.GameStates
        .Include(gs => gs.Level)
        .Where(gs => gs.PlayerId == playerId && gs.Status == GameStatus.InProgress)
        .OrderByDescending(gs => gs.LastModified ?? gs.StartTime);

    var total = await query.CountAsync();
    var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    return Json(new
    {
        success = true,
        total,
        page,
        pageSize,
        games = items.Select(gs => new
        {
            id = gs.Id,
            levelNumber = gs.Level.Number,
            moves = gs.MovesCount,
            lastActivity = (gs.LastModified ?? gs.StartTime).ToString("o")
        })
    });
}
```

2) Frontend – Botões e modal
- Em `JogoBolinha/Views/Game/Game.cshtml`, na área `.game-controls`, adicione:

```
<button class="btn btn-outline-success" id="save-btn" title="Salvar jogo agora">
  <i class="fas fa-save"></i> Salvar
</button>
<button class="btn btn-outline-info" id="load-btn" title="Carregar jogo salvo">
  <i class="fas fa-folder-open"></i> Carregar
</button>
```

- Abaixo, adicione um modal simples para listar jogos salvos:

```
<div class="modal fade" id="load-modal" tabindex="-1">
  <div class="modal-dialog modal-dialog-centered">
    <div class="modal-content">
      <div class="modal-header bg-secondary text-white">
        <h5 class="modal-title"><i class="fas fa-folder-open"></i> Jogos Salvos</h5>
        <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
      </div>
      <div class="modal-body">
        <div id="saved-games-list">
          <div class="text-center text-muted">Carregando...</div>
        </div>
      </div>
      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Fechar</button>
      </div>
    </div>
  </div>
  </div>
```

3) Frontend – JS para salvar/carregar
- Ainda em `Game.cshtml`, no bloco `<script>` principal, adicione:

```
$('#save-btn').on('click', function() {
  $.post('@Url.Action("SaveGame", "Game")', { gameStateId: gameStateId })
    .done(function(resp){
      if (resp.success) {
        showMessage('Jogo salvo!', 'success');
      } else {
        showMessage(resp.message || 'Falha ao salvar', 'error');
      }
    })
    .fail(function(){ showMessage('Erro de conexão ao salvar', 'error'); });
});

$('#load-btn').on('click', function() {
  $('#load-modal').modal('show');
  $('#saved-games-list').html('<div class="text-center text-muted">Carregando...</div>');
  $.get('@Url.Action("ListSavedGames", "Game")')
    .done(function(resp){
      if (!resp.success) { $('#saved-games-list').html('<div class="text-danger">' + (resp.message||'Erro') + '</div>'); return; }
      if (!resp.games || resp.games.length === 0) { $('#saved-games-list').html('<div class="text-muted">Nenhum jogo salvo.</div>'); return; }
      const html = resp.games.map(g => `
        <div class="d-flex justify-content-between align-items-center border rounded p-2 mb-2">
          <div>
            <div><strong>Nível:</strong> ${g.levelNumber}</div>
            <small class="text-muted">Movimentos: ${g.moves} • Última atividade: ${new Date(g.lastActivity).toLocaleString()}</small>
          </div>
          <a class="btn btn-sm btn-primary" href="@Url.Action("Continue", "Game")?gameStateId=${'${g.id}'}">
            <i class="fas fa-play"></i> Carregar
          </a>
        </div>`).join('');
      $('#saved-games-list').html(html);
    })
    .fail(function(){ $('#saved-games-list').html('<div class="text-danger">Erro de conexão</div>'); });
});
```

4) Validação rápida
- Build: `dotnet build JogoBolinha/JogoBolinha.csproj -c Release`.
- Acessar um nível → clicar em “Salvar” → deve mostrar “Jogo salvo!”.
- Clicar em “Carregar” → modal lista jogos; clicar em “Carregar” abre o jogo correspondente (rota `Game/Continue`).

5) Observações
- O botão “Salvar” usa um endpoint dedicado `SaveGame` (equivalente funcional ao `AutoSave`, mas explícito).
- A listagem é paginada (page/pageSize) para crescer com o tempo.
- Segurança: a ação `SaveGame` adota o jogo anônimo se o usuário estiver logado; se um jogo já pertence a outro usuário, a resposta é “Não autorizado”.

