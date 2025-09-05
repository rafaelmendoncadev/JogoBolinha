# PRD: Refatoração do Sistema de Níveis - Jogo da Bolinha

**1. Resumo Executivo**
    *   **1.1 Visão Geral:** Este PRD descreve a necessidade de refatorar o sistema de geração e armazenamento de níveis do Jogo da Bolinha para resolver problemas de inconsistência, performance e falta de variedade.
    *   **1.2 Problema:** O sistema atual de níveis apresenta bugs, como níveis sem solução, repetição de layouts e dificuldade mal calibrada. A geração de níveis é lenta e o formato de armazenamento (`InitialState` como JSON) é ineficiente.
    *   **1.3 Solução:** Redesenhar o algoritmo de geração de níveis para garantir que sejam solucionáveis, introduzir maior variedade e um sistema de dificuldade mais granular. Otimizar o formato de armazenamento dos níveis para melhorar a performance.

**2. Objetivos e Métricas**
    *   **2.1 Objetivos Principais:**
        1.  **Garantir Solucionabilidade:** 100% dos níveis gerados devem ter uma solução válida.
        2.  **Melhorar Performance:** Reduzir o tempo de geração de um novo nível em 50%.
        3.  **Aumentar Variedade:** Garantir que não haja repetição de layouts de níveis nos primeiros 100 níveis.
        4.  **Calibrar Dificuldade:** Implementar uma curva de dificuldade suave e progressiva.
    *   **2.2 Métricas de Sucesso:**
        *   Redução de 90% nos relatos de "nível impossível".
        *   Tempo de carregamento de um nível < 100ms.
        *   Aumento de 20% na taxa de conclusão de níveis.

**3. Requisitos Funcionais**
    *   **3.1 Algoritmo de Geração de Níveis:**
        *   **3.1.1 Geração Reversa:** O novo algoritmo deve gerar níveis a partir de um estado final (solucionado) e aplicar um número de movimentos aleatórios e válidos em ordem inversa para criar o desafio.
        *   **3.1.2 Validação de Solucionabilidade:** O número de movimentos inversos aplicados definirá a dificuldade e garantirá que sempre haja um caminho para a solução.
        *   **3.1.3 Parâmetros de Dificuldade:** O algoritmo deve aceitar parâmetros para controlar a dificuldade:
            *   Número de tubos.
            *   Número de cores.
            *   Número de movimentos "aleatórios" para embaralhar.
            *   Número de tubos vazios.
    *   **3.2 Armazenamento de Níveis:**
        *   **3.2.1 Formato Compacto:** Substituir o JSON no campo `InitialState` por um formato de string mais compacto. Ex: `T1=R,G,B,R;T2=G,B,R,G;T3=B,R,G,B;T4=;T5=`.
        *   **3.2.2 Seed de Geração:** Armazenar a "seed" usada para gerar o nível, permitindo a regeneração do mesmo nível se necessário, em vez de todo o layout.
    *   **3.3 Curva de Dificuldade:**
        *   **Níveis 1-10 (Tutorial):** 3 cores, 4 tubos, 1 tubo vazio, 5-10 movimentos de embaralhamento.
        *   **Níveis 11-30 (Fácil):** 4 cores, 5 tubos, 1 tubo vazio, 15-25 movimentos.
        *   **Níveis 31-60 (Médio):** 5 cores, 6-7 tubos, 1-2 tubos vazios, 30-45 movimentos.
        *   **Níveis 61-100 (Difícil):** 6-7 cores, 8-9 tubos, 2 tubos vazios, 50-70 movimentos.
        *   **Níveis 100+ (especialista):** 8+ cores, 10+ tubos, 2 tubos vazios, 80+ movimentos.
    *   **3.4 Migração de Níveis Existentes:**
        *   Criar um script para converter os níveis existentes no banco de dados para o novo formato de armazenamento.
        *   Os níveis antigos devem ser re-validados pelo novo algoritmo de solucionabilidade. Níveis inválidos devem ser descartados e gerados novamente.

**4. Requisitos Técnicos**
    *   **4.1 `LevelGeneratorService`:**
        *   Refatorar o método `GenerateLevel(int levelNumber)` para implementar a geração reversa.
        *   Adicionar um método `ValidateLevel(string levelLayout)` para verificar a solucionabilidade.
    *   **4.2 Modelo `Level`:**
        *   Manter o campo `InitialState` (string), mas agora armazenando o formato compacto.
        *   Adicionar um campo `GenerationSeed` (long) para armazenar a seed.
    *   **4.3 `GameController`:**
        *   Atualizar a lógica de criação de `GameState` para parsear o novo formato de `InitialState`.
    *   **4.4 Script de Migração:**
        *   Um script C# executável via `dotnet run` que:
            1.  Lê todos os níveis do banco.
            2.  Tenta converter o JSON para o novo formato.
            3.  Valida a solucionabilidade.
            4.  Se válido, atualiza o nível.
            5.  Se inválido, gera um novo nível para aquele número.

**5. Plano de Implementação**
    *   **Fase 1 (3 dias): Refatoração do `LevelGeneratorService`**
        *   Implementar o algoritmo de geração reversa.
        *   Adicionar a lógica de dificuldade baseada em parâmetros.
    *   **Fase 2 (2 dias): Atualização do Armazenamento e Parsing**
        *   Modificar o modelo `Level`.
        *   Atualizar o `GameController` para interpretar o novo formato.
    *   **Fase 3 (2 dias): Script de Migração**
        *   Desenvolver e testar o script de migração em um ambiente de desenvolvimento.
    *   **Fase 4 (1 dia): Testes e Validação**
        *   Gerar 200 níveis e validar manualmente a dificuldade e variedade.
        *   Executar testes unitários para o `LevelGeneratorService`.
        *   Executar o script de migração em staging.

**6. Riscos e Mitigações**
    *   **Risco:** O algoritmo de geração reversa pode ser complexo de implementar.
        *   **Mitigação:** Começar com uma implementação simples e adicionar complexidade gradualmente. Basear-se em algoritmos conhecidos de "shuffling".
    *   **Risco:** A migração de dados pode falhar e corromper níveis existentes.
        *   **Mitigação:** Fazer backup completo do banco de dados antes da migração. Testar o script exaustivamente em ambiente de desenvolvimento e staging.