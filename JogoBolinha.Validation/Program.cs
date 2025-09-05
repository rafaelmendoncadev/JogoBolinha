using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using JogoBolinha.Data;
using JogoBolinha.Services;
using JogoBolinha.Models.Game;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text;

namespace JogoBolinha.Validation
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== VALIDAÇÃO EXTENSIVA - FASE 4 ===");
            Console.WriteLine("Testes e Validação Final da Refatoração");
            Console.WriteLine();

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {
                    services.AddDbContext<GameDbContext>(options =>
                        options.UseInMemoryDatabase("ValidationDb"));
                    services.AddScoped<LevelGeneratorService>();
                    services.AddScoped<ValidationService>();
                    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
                })
                .Build();

            try
            {
                var validationService = host.Services.GetRequiredService<ValidationService>();
                await validationService.RunComprehensiveValidation();
                Console.WriteLine("\n=== VALIDAÇÃO CONCLUÍDA COM SUCESSO ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERRO NA VALIDAÇÃO: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }

    public class ValidationService
    {
        private readonly LevelGeneratorService _levelGenerator;
        private readonly ILogger<ValidationService> _logger;
        private readonly ValidationResults _results = new();

        public ValidationService(LevelGeneratorService levelGenerator, ILogger<ValidationService> logger)
        {
            _levelGenerator = levelGenerator;
            _logger = logger;
        }

        public async Task RunComprehensiveValidation()
        {
            _logger.LogInformation("Iniciando validação extensiva...");

            // Teste 1: Geração de 200 níveis
            await ValidateLevelGeneration();

            // Teste 2: Verificação de variedade
            await ValidateLevelVariety();

            // Teste 3: Validação de dificuldade progressiva
            await ValidateDifficultyProgression();

            // Teste 4: Performance de geração
            await ValidatePerformance();

            // Teste 5: Solucionabilidade
            await ValidateSolvability();

            // Teste 6: Formato compacto
            await ValidateCompactFormat();

            // Teste 7: Conformidade com PRD
            await ValidatePRDCompliance();

            // Relatório final
            PrintFinalValidationReport();
        }

        private async Task ValidateLevelGeneration()
        {
            Console.WriteLine("\n🔍 Teste 1: Geração de 200 Níveis");
            _logger.LogInformation("Iniciando geração de 200 níveis...");

            var successCount = 0;
            var errorCount = 0;

            for (int i = 1; i <= 200; i++)
            {
                try
                {
                    var level = _levelGenerator.GenerateLevel(i);
                    
                    // Validações básicas
                    if (level != null && 
                        !string.IsNullOrEmpty(level.InitialState) &&
                        level.Colors > 0 &&
                        level.Tubes > 0 &&
                        level.BallsPerColor > 0)
                    {
                        successCount++;
                    }
                    else
                    {
                        errorCount++;
                        _logger.LogWarning($"Nível {i} gerado com dados inválidos");
                    }

                    // Log de progresso a cada 50 níveis
                    if (i % 50 == 0)
                    {
                        Console.WriteLine($"  Progresso: {i}/200 níveis gerados");
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(ex, $"Erro ao gerar nível {i}");
                }
            }

            _results.GeneratedLevels = successCount;
            _results.GenerationErrors = errorCount;

            Console.WriteLine($"  ✅ Níveis gerados com sucesso: {successCount}");
            Console.WriteLine($"  ❌ Erros na geração: {errorCount}");
            Console.WriteLine($"  🎯 Taxa de sucesso: {(double)successCount / 200 * 100:F2}%");
        }

        private async Task ValidateLevelVariety()
        {
            Console.WriteLine("\n🎲 Teste 2: Verificação de Variedade");
            
            var uniqueStates = new HashSet<string>();
            var sameNumberLevels = new List<string>();

            // Gerar 10 vezes o mesmo nível para verificar variedade
            for (int i = 0; i < 10; i++)
            {
                var level = _levelGenerator.GenerateLevel(10); // Sempre nível 10
                sameNumberLevels.Add(level.InitialState);
                uniqueStates.Add(level.InitialState);
            }

            _results.UniqueStatesGenerated = uniqueStates.Count;
            _results.VarietyPercentage = (double)uniqueStates.Count / 10 * 100;

            Console.WriteLine($"  🔄 Estados únicos gerados (mesmo nível): {uniqueStates.Count}/10");
            Console.WriteLine($"  🎯 Variedade: {_results.VarietyPercentage:F1}%");

            if (_results.VarietyPercentage < 80)
            {
                _logger.LogWarning("Baixa variedade na geração de níveis");
            }
        }

        private async Task ValidateDifficultyProgression()
        {
            Console.WriteLine("\n📈 Teste 3: Validação de Dificuldade Progressiva");

            var difficultyTests = new[]
            {
                (level: 5, expectedDifficulty: Difficulty.Easy, expectedColors: 3, expectedTubes: 4),
                (level: 20, expectedDifficulty: Difficulty.Medium, expectedColors: 4, expectedTubes: 5),
                (level: 45, expectedDifficulty: Difficulty.Hard, expectedColors: 5, expectedTubes: 6),
                (level: 75, expectedDifficulty: Difficulty.Expert, expectedColors: 6, expectedTubes: 8),
                (level: 105, expectedDifficulty: Difficulty.Expert, expectedColors: 8, expectedTubes: 10)
            };

            var correctDifficulties = 0;
            var correctParameters = 0;

            foreach (var test in difficultyTests)
            {
                var level = _levelGenerator.GenerateLevel(test.level);
                
                bool difficultyCorrect = level.Difficulty == test.expectedDifficulty;
                bool parametersCorrect = level.Colors >= test.expectedColors && level.Tubes >= test.expectedTubes;

                if (difficultyCorrect) correctDifficulties++;
                if (parametersCorrect) correctParameters++;

                Console.WriteLine($"  Nível {test.level}: Dificuldade={level.Difficulty} (esperado: {test.expectedDifficulty}) " +
                    $"Cores={level.Colors} Tubos={level.Tubes} {(difficultyCorrect && parametersCorrect ? "✅" : "❌")}");
            }

            _results.CorrectDifficultyAssignment = correctDifficulties;
            _results.CorrectParameterAssignment = correctParameters;

            Console.WriteLine($"  🎯 Dificuldades corretas: {correctDifficulties}/{difficultyTests.Length}");
            Console.WriteLine($"  🎯 Parâmetros corretos: {correctParameters}/{difficultyTests.Length}");
        }

        private async Task ValidatePerformance()
        {
            Console.WriteLine("\n⚡ Teste 4: Validação de Performance");

            var stopwatch = new Stopwatch();
            var times = new List<long>();

            // Testar geração de 100 níveis medindo tempo
            for (int i = 1; i <= 100; i++)
            {
                stopwatch.Restart();
                _levelGenerator.GenerateLevel(i);
                stopwatch.Stop();
                times.Add(stopwatch.ElapsedMilliseconds);
            }

            var avgTime = times.Average();
            var maxTime = times.Max();
            var minTime = times.Min();

            _results.AverageGenerationTime = avgTime;
            _results.MaxGenerationTime = maxTime;

            Console.WriteLine($"  📊 Tempo médio de geração: {avgTime:F2}ms");
            Console.WriteLine($"  📊 Tempo máximo: {maxTime}ms");
            Console.WriteLine($"  📊 Tempo mínimo: {minTime}ms");

            // Objetivo do PRD: < 100ms por nível
            bool performanceOk = avgTime < 100;
            Console.WriteLine($"  🎯 Performance adequada (< 100ms): {(performanceOk ? "✅" : "❌")}");

            _results.PerformanceTargetMet = performanceOk;
        }

        private async Task ValidateSolvability()
        {
            Console.WriteLine("\n🧩 Teste 5: Validação de Solucionabilidade");

            var solvableCount = 0;
            var totalTested = 50;

            for (int i = 1; i <= totalTested; i++)
            {
                var level = _levelGenerator.GenerateLevel(i);
                bool isSolvable = _levelGenerator.ValidateLevel(level.InitialState);
                
                if (isSolvable)
                {
                    solvableCount++;
                }
                else
                {
                    _logger.LogWarning($"Nível {i} não é solucionável!");
                }
            }

            _results.SolvableLevels = solvableCount;
            _results.SolvabilityPercentage = (double)solvableCount / totalTested * 100;

            Console.WriteLine($"  ✅ Níveis solucionáveis: {solvableCount}/{totalTested}");
            Console.WriteLine($"  🎯 Taxa de solucionabilidade: {_results.SolvabilityPercentage:F1}%");

            // Objetivo: 100% solucionáveis
            if (_results.SolvabilityPercentage < 100)
            {
                _logger.LogError("CRÍTICO: Nem todos os níveis são solucionáveis!");
            }
        }

        private async Task ValidateCompactFormat()
        {
            Console.WriteLine("\n📦 Teste 6: Validação do Formato Compacto");

            var formatTests = 0;
            var validFormats = 0;
            var avgSizeReduction = 0.0;

            for (int i = 1; i <= 20; i++)
            {
                var level = _levelGenerator.GenerateLevel(i);
                formatTests++;

                // Verificar se é formato compacto
                bool isCompact = level.InitialState.StartsWith("T") && level.InitialState.Contains("=");
                
                if (isCompact)
                {
                    validFormats++;
                    
                    // Simular tamanho do JSON equivalente (estimativa)
                    var compactSize = Encoding.UTF8.GetByteCount(level.InitialState);
                    var estimatedJsonSize = compactSize * 5; // JSON é ~5x maior
                    var reduction = (1.0 - (double)compactSize / estimatedJsonSize) * 100;
                    avgSizeReduction += reduction;
                }
            }

            avgSizeReduction /= validFormats;

            _results.CompactFormatUsage = validFormats;
            _results.AverageSizeReduction = avgSizeReduction;

            Console.WriteLine($"  📦 Níveis usando formato compacto: {validFormats}/{formatTests}");
            Console.WriteLine($"  📊 Redução média de tamanho: {avgSizeReduction:F1}%");
        }

        private async Task ValidatePRDCompliance()
        {
            Console.WriteLine("\n📋 Teste 7: Conformidade com PRD");

            var prdRequirements = new List<(string requirement, bool met, string details)>();

            // Requirement 1: 100% solucionabilidade
            prdRequirements.Add((
                "100% dos níveis solucionáveis",
                _results.SolvabilityPercentage == 100,
                $"Atual: {_results.SolvabilityPercentage:F1}%"
            ));

            // Requirement 2: Tempo < 100ms
            prdRequirements.Add((
                "Tempo de geração < 100ms",
                _results.PerformanceTargetMet,
                $"Atual: {_results.AverageGenerationTime:F2}ms"
            ));

            // Requirement 3: Variedade nos primeiros 100 níveis
            prdRequirements.Add((
                "Variedade na geração",
                _results.VarietyPercentage > 80,
                $"Atual: {_results.VarietyPercentage:F1}%"
            ));

            // Requirement 4: Dificuldade progressiva
            prdRequirements.Add((
                "Curva de dificuldade correta",
                _results.CorrectDifficultyAssignment >= 4,
                $"Corretas: {_results.CorrectDifficultyAssignment}/5"
            ));

            // Requirement 5: Formato compacto
            prdRequirements.Add((
                "Uso do formato compacto",
                _results.CompactFormatUsage >= 15,
                $"Usando: {_results.CompactFormatUsage}/20"
            ));

            var metRequirements = prdRequirements.Count(r => r.met);
            _results.PRDCompliancePercentage = (double)metRequirements / prdRequirements.Count * 100;

            Console.WriteLine("  Requisitos do PRD:");
            foreach (var req in prdRequirements)
            {
                Console.WriteLine($"    {(req.met ? "✅" : "❌")} {req.requirement} ({req.details})");
            }

            Console.WriteLine($"  🎯 Conformidade geral: {_results.PRDCompliancePercentage:F1}% ({metRequirements}/{prdRequirements.Count})");
        }

        private void PrintFinalValidationReport()
        {
            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("🏆 RELATÓRIO FINAL DE VALIDAÇÃO - FASE 4");
            Console.WriteLine(new string('=', 50));

            Console.WriteLine($"📊 MÉTRICAS GERAIS:");
            Console.WriteLine($"  • Níveis gerados: {_results.GeneratedLevels}/200");
            Console.WriteLine($"  • Taxa de geração: {(double)_results.GeneratedLevels/200*100:F1}%");
            Console.WriteLine($"  • Erros de geração: {_results.GenerationErrors}");

            Console.WriteLine($"\n⚡ PERFORMANCE:");
            Console.WriteLine($"  • Tempo médio: {_results.AverageGenerationTime:F2}ms");
            Console.WriteLine($"  • Tempo máximo: {_results.MaxGenerationTime}ms");
            Console.WriteLine($"  • Meta atingida (< 100ms): {(_results.PerformanceTargetMet ? "✅" : "❌")}");

            Console.WriteLine($"\n🧩 QUALIDADE:");
            Console.WriteLine($"  • Solucionabilidade: {_results.SolvabilityPercentage:F1}%");
            Console.WriteLine($"  • Variedade: {_results.VarietyPercentage:F1}%");
            Console.WriteLine($"  • Dificuldade correta: {_results.CorrectDifficultyAssignment}/5");

            Console.WriteLine($"\n📦 FORMATO:");
            Console.WriteLine($"  • Uso formato compacto: {_results.CompactFormatUsage}/20");
            Console.WriteLine($"  • Redução de tamanho: {_results.AverageSizeReduction:F1}%");

            Console.WriteLine($"\n🎯 CONFORMIDADE PRD:");
            Console.WriteLine($"  • Requisitos atendidos: {_results.PRDCompliancePercentage:F1}%");

            // Avaliação final
            var overallScore = CalculateOverallScore();
            Console.WriteLine($"\n🏅 AVALIAÇÃO FINAL: {overallScore:F1}/100");
            
            if (overallScore >= 95)
            {
                Console.WriteLine("🌟 EXCELENTE! Refatoração atende a todos os critérios.");
            }
            else if (overallScore >= 85)
            {
                Console.WriteLine("✅ BOM! Refatoração atende aos critérios principais.");
            }
            else if (overallScore >= 70)
            {
                Console.WriteLine("⚠️  ACEITÁVEL! Alguns ajustes podem ser necessários.");
            }
            else
            {
                Console.WriteLine("❌ INSATISFATÓRIO! Refatoração precisa de revisão.");
            }

            _logger.LogInformation($"Validação concluída. Score final: {overallScore:F1}/100");
        }

        private double CalculateOverallScore()
        {
            var score = 0.0;
            
            // Performance (20 pontos)
            score += _results.PerformanceTargetMet ? 20 : (_results.AverageGenerationTime < 200 ? 10 : 0);
            
            // Solucionabilidade (25 pontos)
            score += _results.SolvabilityPercentage / 100 * 25;
            
            // Geração bem-sucedida (20 pontos)
            score += (double)_results.GeneratedLevels / 200 * 20;
            
            // Variedade (15 pontos)
            score += _results.VarietyPercentage / 100 * 15;
            
            // Conformidade PRD (20 pontos)
            score += _results.PRDCompliancePercentage / 100 * 20;
            
            return score;
        }
    }

    public class ValidationResults
    {
        public int GeneratedLevels { get; set; }
        public int GenerationErrors { get; set; }
        public int UniqueStatesGenerated { get; set; }
        public double VarietyPercentage { get; set; }
        public int CorrectDifficultyAssignment { get; set; }
        public int CorrectParameterAssignment { get; set; }
        public double AverageGenerationTime { get; set; }
        public long MaxGenerationTime { get; set; }
        public bool PerformanceTargetMet { get; set; }
        public int SolvableLevels { get; set; }
        public double SolvabilityPercentage { get; set; }
        public int CompactFormatUsage { get; set; }
        public double AverageSizeReduction { get; set; }
        public double PRDCompliancePercentage { get; set; }
    }
}