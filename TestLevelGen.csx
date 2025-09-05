#r "JogoBolinha/bin/Debug/net8.0/JogoBolinha.dll"

using JogoBolinha.Services;
using JogoBolinha.Models.Game;
using System;

var generator = new LevelGeneratorService();

Console.WriteLine("=== Testando Nova Geração de Níveis com Algoritmo Reverso ===\n");

// Testar níveis de diferentes dificuldades
int[] testLevels = { 1, 5, 10, 15, 25, 35, 50, 65, 80, 101 };

foreach (int levelNumber in testLevels)
{
    Console.WriteLine($"\n--- Gerando Nível {levelNumber} ---");
    
    try
    {
        var level = generator.GenerateLevel(levelNumber);
        
        Console.WriteLine($"Dificuldade: {level.Difficulty}");
        Console.WriteLine($"Cores: {level.Colors}");
        Console.WriteLine($"Tubos: {level.Tubes}");
        Console.WriteLine($"Bolas por cor: {level.BallsPerColor}");
        Console.WriteLine($"Movimentos mínimos: {level.MinimumMoves}");
        
        // Mostrar formato compacto
        if (level.InitialState.Length < 100)
        {
            Console.WriteLine($"Estado inicial: {level.InitialState}");
        }
        else
        {
            Console.WriteLine($"Estado inicial (primeiros 80 chars): {level.InitialState.Substring(0, 80)}...");
        }
        
        // Validar o nível gerado
        bool isValid = generator.ValidateLevel(level.InitialState);
        Console.WriteLine($"Nível válido: {(isValid ? "✓ SIM" : "✗ NÃO")}");
        
        if (!isValid)
        {
            Console.WriteLine("ERRO: Nível gerado não é válido!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERRO ao gerar nível: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}

Console.WriteLine("\n=== Teste de Validação de Níveis ===\n");

// Testar validação com formato compacto
string validLevel = "T1=A,A,A,A;T2=B,B,B,B;T3=C,C,C,C;T4=;T5=";
string invalidLevel = "T1=A,A,B;T2=B,C;T3=";

Console.WriteLine($"Nível válido (sorted): {generator.ValidateLevel(validLevel)}");
Console.WriteLine($"Nível inválido (mixed): {generator.ValidateLevel(invalidLevel)}");

Console.WriteLine("\n=== Verificando Parâmetros por Nível (Conforme PRD) ===\n");

// Verificar se os parâmetros seguem o PRD
var prChecks = new[]
{
    (level: 5, expectedColors: 3, expectedTubes: 4, expectedShuffleMin: 5, expectedShuffleMax: 10, name: "Tutorial"),
    (level: 20, expectedColors: 4, expectedTubes: 5, expectedShuffleMin: 15, expectedShuffleMax: 25, name: "Fácil"),  
    (level: 45, expectedColors: 5, expectedTubes: 6, expectedShuffleMin: 30, expectedShuffleMax: 45, name: "Médio"),
    (level: 75, expectedColors: 6, expectedTubes: 8, expectedShuffleMin: 50, expectedShuffleMax: 70, name: "Difícil"),
    (level: 105, expectedColors: 8, expectedTubes: 10, expectedShuffleMin: 80, expectedShuffleMax: 100, name: "Expert")
};

foreach (var check in prChecks)
{
    var level = generator.GenerateLevel(check.level);
    Console.WriteLine($"Nível {check.level} ({check.name}):");
    Console.WriteLine($"  Cores: {level.Colors} (esperado: {check.expectedColors}+)");
    Console.WriteLine($"  Tubos: {level.Tubes} (esperado: {check.expectedTubes}+)");
    Console.WriteLine($"  Movimentos mínimos: {level.MinimumMoves} (esperado entre {check.expectedShuffleMin}-{check.expectedShuffleMax})");
    
    bool paramsCorrect = level.Colors >= check.expectedColors && 
                         level.Tubes >= check.expectedTubes &&
                         level.MinimumMoves >= check.expectedShuffleMin;
    
    Console.WriteLine($"  Parâmetros corretos: {(paramsCorrect ? "✓" : "✗")}");
}

Console.WriteLine("\n=== Teste Concluído ===");