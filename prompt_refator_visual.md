# Prompt: Refatorar o Visual do Aplicativo (UI/UX Refresh)

Objetivo
- Modernizar a interface (Game, Home, Auth, Profile, Admin) para uma aparência mais agradável, coerente e acessível, mantendo Bootstrap e minimizando alterações de backend.

Entregáveis
- Nova paleta de cores + tema claro/escuro consistente
- Tipografia e espaçamento revisados
- Componentes unificados (botões, cards, modais, alerts)
- Polimento da tela do jogo (tubos, bolas, animações, layout)
- Acessibilidade: foco visível, contraste AA, navegação por teclado
- CSS organizado (tokens/design‑system) e sem “mojibake”/artefatos de encoding

1) Design System (tokens CSS)
- Adicionar tokens em `wwwroot/css/themes.css` (usar variáveis CSS):

```
:root {
  --color-bg: #0f172a;           /* slate-900 */
  --color-surface: #111827;      /* slate-800 */
  --color-card: #1f2937;         /* slate-700 */
  --color-text: #e5e7eb;         /* slate-200 */
  --color-muted: #9ca3af;        /* slate-400 */
  --primary-500: #6366f1;        /* indigo */
  --primary-600: #4f46e5;
  --primary-700: #4338ca;
  --success-500: #10b981;
  --warning-500: #f59e0b;
  --danger-500: #ef4444;
  --info-500: #06b6d4;
  --radius: 14px;
  --shadow-1: 0 8px 24px rgba(0,0,0,.2);
  --shadow-2: 0 16px 40px rgba(0,0,0,.35);
  --gradient-hero: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
}

/* Tema Claro */
:root[data-theme="light"] {
  --color-bg: #f8fafc;
  --color-surface: #ffffff;
  --color-card: #ffffff;
  --color-text: #0f172a;
  --color-muted: #64748b;
}
```

- Usar tokens acima como base para estilização (sem quebrar Bootstrap).

2) Tipografia
- Atualizar `_Layout.cshtml` para incluir Google Fonts (ou fallback local caso offline):

```
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;600;700&display=swap" rel="stylesheet">
```

- Em `site.css` aplicar fonte padrão:

```
body { font-family: Inter, system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif; }
```

3) Layout & Containers
- Em `site.css` ajustar largura máxima e respiros:

```
.container, .game-container { max-width: 1200px; margin: 0 auto; padding: 24px; }
.section { margin: 32px 0; }
```

- Header/hero com gradiente (já existe no Game; padronizar no Home/Profile):

```
.hero {
  background: var(--gradient-hero);
  color: #fff; border-radius: var(--radius);
  padding: 24px; box-shadow: var(--shadow-1);
}
```

4) Botões & Estados
- Uniformizar botões Bootstrap com tokens:

```
.btn-primary { background: var(--primary-600); border-color: var(--primary-600); }
.btn-primary:hover { background: var(--primary-700); border-color: var(--primary-700); }
.btn-outline-secondary { color: var(--color-text); border-color: var(--color-muted); }
.btn:focus { box-shadow: 0 0 0 0.2rem rgba(99,102,241,.35); outline: none; }
```

- Adicionar estados de foco visíveis também para `.tube`, `.ball` (acessibilidade).

5) Cards, Modais e Alerts
- Cards (lista de jogos salvos, sessões, leaderboard):

```
.card { background: var(--color-card); border: 1px solid rgba(255,255,255,.06); border-radius: var(--radius); box-shadow: var(--shadow-1); }
.card-header { border-bottom: 1px solid rgba(255,255,255,.08); }
.modal-content { border-radius: var(--radius); box-shadow: var(--shadow-2); }
.alert { border-radius: 12px; }
```

6) Tela do Jogo – Polimento Visual
- Tubos com efeito vidro, bolas com shading suave e animações leves:

```
.tube-body {
  background: linear-gradient(180deg, rgba(255,255,255,.25), rgba(255,255,255,.08));
  backdrop-filter: blur(6px);
  border: 2px solid rgba(255,255,255,.25);
  border-radius: 0 0 16px 16px;
  box-shadow: inset 0 -6px 14px rgba(0,0,0,.15);
}
.ball {
  border: 2px solid rgba(255,255,255,.25);
  box-shadow: 0 4px 10px rgba(0,0,0,.25), inset 0 1px 3px rgba(255,255,255,.35);
  transition: transform .18s ease, box-shadow .18s ease;
}
.ball:hover, .ball:focus { transform: translateY(-2px) scale(1.05); }
```

- Melhorar progress bar: usar valores e cores do tema.

7) Dark/Light Theme + Toggle
- Já existe `theme-manager.js`; alinhar tokens e garantir troca suave:

```
html { transition: background-color .2s ease, color .2s ease; }
body { background: var(--color-bg); color: var(--color-text); }
```

- Revisar `.game-header`, `.game-controls`, `.game-area` para usar cores/tokens e contraste AA.

8) Acessibilidade
- Garantir contraste mínimo AA/AAA para texto principal e botões.
- Estados de foco/hover claros.
- `prefers-reduced-motion`: reduzir animações quando solicitado.

```
@media (prefers-reduced-motion: reduce) {
  * { animation: none !important; transition: none !important; }
}
```

9) Limpeza de Encoding / Mojibake
- Abrir arquivos `.cshtml` e `.cs` que exibem caracteres corrompidos (ex.: "N��vel").
- Salvar todos como UTF‑8 e corrigir strings literais. Revisar `Views/Game/Game.cshtml`, `Program.cs`, `AchievementService.cs`.

10) Ícones e Feedback
- Garantir Font Awesome carregado em `_Layout.cshtml`.
- Mensagens (toasts/alerts) padronizadas com ícones (success/info/warning/danger) e auto‑close suave.

11) Performance
- Minificar CSS/JS no publish (já ocorre). Reduzir sombras/blur em mobile se necessário.
- Lighthouse alvo: Performance > 85, Acessibilidade > 90.

12) Páginas a revisar
- Home (cards de jogos salvos e CTA para continuar)
- Game (já ajustado; aplicar tokens/cores e modais)
- Auth (Login/Register – UX claro, validação visual)
- Profile (SavedGames – tabela/cards responsivos, botões de ação)
- Admin (melhorar legibilidade)

13) Plano de execução
- [ ] Criar tokens em `themes.css` e unificar `site.css`
- [ ] Ajustar `_Layout.cshtml` (fonts, icons) e header/footer
- [ ] Refatorar `Views/Game/Game.cshtml` para usar tokens (cores/sombras/radius) e foco/hover
- [ ] Melhorar Home/Profile com cards e hero
- [ ] Revisar modais/alerts e acessibilidade
- [ ] Corrigir encoding e strings
- [ ] Testar responsividade (320px–1440px) e Lighthouse

Dica
- Faça mudanças incrementais por página e valide visualmente; mantenha classes Bootstrap onde possível e sobreponha somente o necessário via tokens.
