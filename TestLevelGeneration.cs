using JogoBolinha.Services;
using System;

class TestLevelGeneration
{
    static void Main()
    {
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
                Console.WriteLine($"Estado inicial (primeiros 50 chars): {level.InitialState.Substring(0, Math.Min(50, level.InitialState.Length))}...");
                
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
            }
        }
        
        Console.WriteLine("\n=== Teste de Validação de Níveis ===\n");
        
        // Testar validação com formato compacto
        string validLevel = "T1=A,A,A,A;T2=B,B,B,B;T3=C,C,C,C;T4=;T5=";
        string invalidLevel = "T1=A,A,B;T2=B,C;T3=";
        
        Console.WriteLine($"Nível válido (sorted): {generator.ValidateLevel(validLevel)}");
        Console.WriteLine($"Nível inválido (mixed): {generator.ValidateLevel(invalidLevel)}");
        
        Console.WriteLine("\n=== Teste Concluído ===");
    }
}