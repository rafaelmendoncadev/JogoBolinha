# PRD - Ball Sort Puzzle Game
## Documento de Requisitos do Produto

**Versão:** 1.0  
**Data:** Setembro 2025  
**Produto:** Jogo Ball Sort Puzzle  
**Plataforma:** ASP.NET Core MVC  

---

## 1. Visão Geral do Produto

### 1.1 Descrição
Desenvolvimento de um jogo web de puzzle baseado na mecânica de classificação de bolinhas coloridas em tubos. O jogador deve organizar bolinhas de diferentes cores em tubos separados, seguindo regras específicas de movimentação.

### 1.2 Objetivos do Negócio
- Criar uma experiência de jogo envolvente e desafiadora
- Implementar sistema de progressão por níveis
- Desenvolver funcionalidades sociais básicas (ranking, perfil)
- Estabelecer base técnica escalável para futuros jogos

### 1.3 Métricas de Sucesso
- Taxa de retenção de usuários > 60% após 7 dias
- Tempo médio de sessão > 10 minutos
- Taxa de conclusão de níveis > 70%
- Pontuação de satisfação do usuário > 4.2/5.0

---

## 2. Público-Alvo

### 2.1 Persona Primária
- **Idade:** 18-45 anos
- **Perfil:** Jogadores casuais que buscam entretenimento rápido
- **Dispositivos:** Desktop e mobile
- **Comportamento:** Sessões curtas durante intervalos

### 2.2 Persona Secundária
- **Idade:** 12-17 anos
- **Perfil:** Estudantes que gostam de jogos de lógica
- **Motivação:** Desafio intelectual e competição com amigos

---

## 3. Funcionalidades Principais

### 3.1 Core Game (MVP)

#### 3.1.1 Mecânica de Jogo
- **Movimentação de bolinhas:** Clicar na bolinha do topo para selecionar, clicar no tubo destino
- **Validação de movimentos:** Apenas movimentos válidos são permitidos
- **Detecção de vitória:** Automática quando todas as cores estão organizadas
- **Sistema de desfazer:** Até 3 movimentos anteriores por nível

#### 3.1.2 Níveis
- **Nível 1-10:** Tutorial progressivo (2-3 cores, 3-4 tubos)
- **Nível 11-30:** Dificuldade média (3-4 cores, 4-5 tubos)
- **Nível 31-50:** Dificuldade alta (4-5 cores, 5-6 tubos)
- **Nível 51+:** Dificuldade extrema (5-6 cores, 6-7 tubos)

#### 3.1.3 Sistema de Pontuação
- **Pontos base:** 100 pontos por nível completado
- **Bônus de eficiência:** +10 pontos por movimento não utilizado
- **Bônus de velocidade:** +50 pontos se completado em < 2 minutos
- **Penalidade por dica:** -20 pontos por dica utilizada

### 3.2 Recursos Auxiliares

#### 3.2.1 Sistema de Dicas
- **Dica simples:** Destacar próximo movimento válido (3 por nível)
- **Dica avançada:** Mostrar sequência de 2-3 movimentos (1 por nível)
- **Custo:** Redução na pontuação final

#### 3.2.2 Customização Visual
- **Temas de cores:** 5 paletas diferentes para as bolinhas
- **Temas de fundo:** 3 ambientes (clássico, neon, natureza)
- **Efeitos visuais:** Animações de movimento e partículas de vitória

### 3.3 Funcionalidades Sociais

#### 3.3.1 Sistema de Perfil
- **Estatísticas pessoais:** Níveis completados, pontuação total, tempo jogado
- **Conquistas:** 15 achievements diferentes
- **Histórico:** Últimos 10 jogos com detalhes

#### 3.3.2 Ranking e Competição
- **Leaderboard global:** Top 100 jogadores por pontuação
- **Leaderboard semanal:** Reset automático toda segunda-feira
- **Comparação com amigos:** Se implementado login social

---

## 4. Requisitos Técnicos

### 4.1 Tecnologias Core
- **Framework:** ASP.NET Core 8.0 MVC
- **Linguagem:** C# 12
- **Banco de Dados:** SQLite
- **ORM:** Entity Framework Core
- **Frontend:** HTML5, CSS3, JavaScript (Vanilla/jQuery)
- **Controle de Versão:** Git

### 4.2 Arquitetura do Sistema

#### 4.2.1 Estrutura MVC
```
Controllers/
├── HomeController.cs
├── GameController.cs
├── ProfileController.cs
└── LeaderboardController.cs

Models/
├── Game/
│   ├── GameState.cs
│   ├── Tube.cs
│   ├── Ball.cs
│   └── Level.cs
├── User/
│   ├── Player.cs
│   ├── PlayerStats.cs
│   └── Achievement.cs
└── ViewModels/

Views/
├── Home/
├── Game/
├── Profile/
├── Leaderboard/
└── Shared/

Services/
├── GameLogicService.cs
├── LevelGeneratorService.cs
├── ScoreCalculationService.cs
└── AchievementService.cs
```

#### 4.2.2 Banco de Dados (Entidades Principais)
```sql
-- Players
Players (Id, Username, Email, CreatedAt, LastLogin)

-- Game Sessions
GameSessions (Id, PlayerId, LevelId, StartTime, EndTime, Score, MovesUsed)

-- Levels
Levels (Id, Number, Difficulty, InitialState, SolutionMoves)

-- Achievements
Achievements (Id, Name, Description, Requirement)
PlayerAchievements (PlayerId, AchievementId, UnlockedAt)

-- Leaderboards
Leaderboards (Id, PlayerId, TotalScore, WeeklyScore, LastUpdated)
```

### 4.3 Performance e Escalabilidade
- **Cache:** Redis para sessions ativas e leaderboards
- **Otimização:** Lazy loading, async/await patterns
- **Monitoramento:** Application Insights ou Serilog
- **Deployment:** Docker containers, CI/CD pipeline

---

## 5. Interface de Usuário

### 5.1 Wireframes Principais

#### 5.1.1 Tela Principal do Jogo
```
[Header: Score | Level | Moves]
[Game Area: Tubos com bolinhas]
[Controls: Restart | Hint | Undo]
[Footer: Menu | Settings]
```

#### 5.1.2 Menu Principal
```
[Logo do Jogo]
[Play Button]
[Continue Last Game]
[Leaderboard]
[Profile]
[Settings]
```

### 5.2 Responsividade
- **Desktop:** Layout horizontal, controles laterais
- **Tablet:** Layout adaptativo, botões maiores
- **Mobile:** Layout vertical, gestos touch otimizados

### 5.3 Acessibilidade
- **Contrast ratio:** WCAG 2.1 AA compliance
- **Navegação por teclado:** Tab navigation completa
- **Screen readers:** ARIA labels apropriados
- **Daltonismo:** Opção de símbolos além de cores

---

## 6. Especificações Detalhadas

### 6.1 Algoritmo de Geração de Níveis
```csharp
// Parâmetros por dificuldade
Easy: 2-3 cores, 3-4 tubos, 2-8 bolinhas por cor
Medium: 3-4 cores, 4-5 tubos, 3-10 bolinhas por cor
Hard: 4-5 cores, 5-6 tubos, 4-12 bolinhas por cor
Expert: 5-6 cores, 6-7 tubos, 5-15 bolinhas por cor
```

### 6.2 Validação de Movimentos
1. Verificar se existe bolinha no tubo origem
2. Verificar se tubo destino não está cheio
3. Verificar se cores são compatíveis ou tubo destino vazio
4. Atualizar estado do jogo
5. Verificar condição de vitória

### 6.3 Sistema de Save/Load
- **Auto-save:** A cada movimento válido
- **Session storage:** Estado temporário no navegador
- **Database persistence:** Progresso permanente para usuários logados

---

## 7. Cronograma de Desenvolvimento

### Sprint 1 (2 semanas) - Setup e Core 
- [x] Setup do projeto ASP.NET Core MVC
- [x] Configuração do banco de dados
- [x] Modelos básicos (Game, Tube, Ball, Level)
- [x] Lógica core do jogo (movimento e validação)

### Sprint 2 (2 semanas) - UI Básica
- [x] Interface do jogo (drag & drop ou click)
- [x] Tela de menu principal
- [x] Sistema de níveis básico
- [x] Animações de movimento

### Sprint 3 (2 semanas) - Features Principais
- [x] Sistema de pontuação
- [x] Funcionalidade de undo/redo
- [x] Sistema de dicas
- [x] Detecção de vitória/derrota

### Sprint 4 (1 semana) - Polimento
- [x] Efeitos visuais e sonoros
- [x] Temas e customização
- [x] Otimizações de performance
- [x] Testes unitários

### Sprint 5 (1 semana) - Recursos Sociais
- [ ] Sistema de perfil básico
- [ ] Leaderboard simples
- [ ] Conquistas/achievements
- [ ] Estatísticas do jogador

### Sprint 6 (1 semana) - Deploy e QA
- [ ] Testes de integração
- [ ] Deployment em ambiente de produção
- [ ] Monitoramento e analytics
- [ ] Documentação final

---

## 8. Critérios de Aceitação

### 8.1 Funcionalidades Core (Obrigatórias)
- ✅ Jogo funciona corretamente em browsers modernos
- ✅ Movimentação de bolinhas é intuitiva e responsiva
- ✅ Validação de regras é 100% precisa
- ✅ Sistema de níveis progressivos funcional
- ✅ Detecção de vitória automática e precisa

### 8.2 Performance (Obrigatórias)
- ✅ Tempo de carregamento inicial < 3 segundos
- ✅ Responsividade de interface < 100ms
- ✅ Suporte para pelo menos 100 usuários simultâneos
- ✅ Compatibilidade com Chrome, Firefox, Safari, Edge

### 8.3 UX/UI (Desejáveis)
- 🎯 Design atrativo e moderno
- 🎯 Animações suaves e agradáveis
- 🎯 Interface intuitiva sem necessidade de tutorial
- 🎯 Feedback visual claro para todas as ações

### 8.4 Recursos Sociais (Desejáveis)
- 🎯 Sistema de ranking funcional
- 🎯 Perfil de usuário com estatísticas
- 🎯 Pelo menos 10 conquistas implementadas

---

## 9. Riscos e Mitigações

### 9.1 Riscos Técnicos
| Risco | Probabilidade | Impacto | Mitigação |
|-------|---------------|---------|-----------|
| Performance em mobile | Média | Alto | Testes early, otimização CSS/JS |
| Compatibilidade browsers | Baixa | Médio | Polyfills, testes cross-browser |
| Complexidade do algoritmo | Média | Médio | Prototipagem inicial, testes unitários |

### 9.2 Riscos de Produto
| Risco | Probabilidade | Impacto | Mitigação |
|-------|---------------|---------|-----------|
| Baixo engagement | Média | Alto | Testes A/B, feedback usuários |
| Dificuldade inadequada | Média | Médio | Testes de usabilidade |
| Concorrência | Alta | Baixo | Diferenciação por qualidade técnica |

---

## 10. Métricas e KPIs

### 10.1 Métricas de Engagement
- **DAU/MAU ratio:** Daily/Monthly Active Users
- **Session Duration:** Tempo médio por sessão
- **Levels Completed:** Taxa de conclusão por nível
- **Return Rate:** Usuários que voltam em 24h/7d

### 10.2 Métricas Técnicas
- **Page Load Time:** < 3s para primeira carga
- **Error Rate:** < 1% de erros JavaScript
- **Uptime:** > 99.5% disponibilidade
- **Response Time:** < 200ms para ações do jogo

### 10.3 Ferramentas de Analytics
- **Google Analytics 4:** Comportamento geral
- **Custom Events:** Ações específicas do jogo
- **A/B Testing:** Otimização de features
- **Performance Monitoring:** Application Insights

---

## 11. Considerações Futuras

### 11.1 Roadmap Pós-MVP
- **Modo Multiplayer:** Competição em tempo real
- **Torneios:** Eventos sazonais com prêmios
- **Level Editor:** Usuários criam próprios níveis
- **Mobile App:** Versão nativa iOS/Android

### 11.2 Monetização (Opcional)
- **Cosmetics:** Venda de temas e customizações
- **Premium Account:** Recursos extras e sem anúncios
- **Hints Pack:** Pacotes de dicas adicionais

---

## 12. Aprovações e Sign-off

| Stakeholder | Role | Status | Data | Comentários |
|------------|------|--------|------|-------------|
| Product Owner | Aprovação Final | ⏳ Pending | | |
| Tech Lead | Aprovação Técnica | ⏳ Pending | | |
| UI/UX Designer | Aprovação Design | ⏳ Pending | | |
| QA Lead | Aprovação Testes | ⏳ Pending | | |

---

**Documento criado por:** Desenvolvedor  
**Próxima revisão:** Sprint Review 1  
**Contato:** Para dúvidas e sugestões sobre este PRD