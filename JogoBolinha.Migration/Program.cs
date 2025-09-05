using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using JogoBolinha.Data;
using JogoBolinha.Services;
using JogoBolinha.Models.Game;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== SCRIPT DE MIGRAÇÃO DE NÍVEIS - FASE 3 ===");
        Console.WriteLine("Refatoração do Sistema de Geração de Níveis");
        Console.WriteLine();

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((hostContext, services) =>
            {
                var connectionString = hostContext.Configuration.GetConnectionString("DefaultConnection") 
                    ?? "Data Source=../JogoBolinha/jogabolinha.db";
                
                services.AddDbContext<GameDbContext>(options =>
                    options.UseSqlite(connectionString));
                services.AddScoped<LevelGeneratorService>();
                services.AddScoped<MigrationService>();
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            })
            .Build();

        try
        {
            var migrationService = host.Services.GetRequiredService<MigrationService>();
            await migrationService.MigrateLevels();
            Console.WriteLine("\n=== MIGRAÇÃO CONCLUÍDA COM SUCESSO ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nERRO FATAL: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }
}

public class MigrationService
{
    private readonly GameDbContext _context;
    private readonly LevelGeneratorService _levelGeneratorService;
    private readonly ILogger<MigrationService> _logger;
    private readonly MigrationStatistics _statistics = new();

    public MigrationService(GameDbContext context, LevelGeneratorService levelGeneratorService, ILogger<MigrationService> logger)
    {
        _context = context;
        _levelGeneratorService = levelGeneratorService;
        _logger = logger;
    }

    public async Task MigrateLevels()
    {
        _logger.LogInformation("Iniciando migração de níveis...");
        
        // Fase 1: Backup do banco atual
        await CreateBackup();

        // Fase 2: Ler todos os níveis do banco
        var levels = await _context.Levels.ToListAsync();
        _logger.LogInformation($"Encontrados {levels.Count} níveis para migração");
        Console.WriteLine($"📊 Encontrados {levels.Count} níveis para migração");

        // Fase 3: Processar cada nível
        foreach (var level in levels)
        {
            await ProcessLevel(level);
        }

        // Fase 4: Salvar alterações
        await _context.SaveChangesAsync();
        _logger.LogInformation("Alterações salvas no banco de dados");

        // Fase 5: Relatório final
        PrintFinalReport();
    }

    private bool IsOldJsonFormat(string initialState)
    {
        return initialState.Trim().StartsWith("{");
    }

    private List<List<string>> ParseOldJsonFormat(string json)
    {
        var tubes = new List<List<string>>();
        var jsonDoc = JsonDocument.Parse(json);
        var tubesData = jsonDoc.RootElement.GetProperty("Tubes").EnumerateArray();

        foreach (var tubeData in tubesData)
        {
            var tube = new List<string>();
            var ballsData = tubeData.GetProperty("Balls").EnumerateArray();
            foreach (var ballData in ballsData)
            {
                tube.Add(ballData.GetProperty("Color").GetString()!);
            }
            tubes.Add(tube);
        }

        return tubes;
    }

    private bool IsLevelValid(List<List<string>> tubes)
    {
        var colorCounts = new Dictionary<string, int>();
        foreach (var tube in tubes)
        {
            foreach (var ball in tube)
            {
                if (!colorCounts.ContainsKey(ball)) colorCounts[ball] = 0;
                colorCounts[ball]++;
            }
        }

        if (colorCounts.Count == 0) return true;

        var firstColorCount = colorCounts.First().Value;
        return colorCounts.All(kv => kv.Value == firstColorCount);
    }

    private string ConvertToCompactFormat(List<List<string>> tubes)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < tubes.Count; i++)
        {
            sb.Append($"T{i + 1}=");
            sb.Append(string.Join(",", tubes[i].Select(GetColorCode)));
            if (i < tubes.Count - 1) sb.Append(';');
        }
        return sb.ToString();
    }

    private string GetColorCode(string color)
    {
        int index = Array.IndexOf(ColorPalette, color);
        return index != -1 ? index.ToString() : "0";
    }

    private async Task CreateBackup()
    {
        _logger.LogInformation("Criando backup do banco de dados...");
        
        var backupPath = $"jogabolinha_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        var currentDbPath = "../JogoBolinha/jogabolinha.db";
        
        if (File.Exists(currentDbPath))
        {
            File.Copy(currentDbPath, backupPath);
            _logger.LogInformation($"Backup criado: {backupPath}");
            Console.WriteLine($"✅ Backup criado: {backupPath}");
        }
        else
        {
            _logger.LogWarning("Arquivo do banco não encontrado para backup");
            Console.WriteLine("⚠️  Arquivo do banco não encontrado para backup");
        }
    }

    private async Task ProcessLevel(Level level)
    {
        Console.WriteLine($"\n--- Processando Nível {level.Number} ---");
        
        try
        {
            if (IsCompactFormat(level.InitialState))
            {
                _statistics.AlreadyCompact++;
                Console.WriteLine($"Nível {level.Number}: Já está no formato compacto ✓");
                
                if (_levelGeneratorService.ValidateLevel(level.InitialState))
                {
                    _statistics.ValidLevels++;
                    Console.WriteLine($"Nível {level.Number}: Válido ✓");
                }
                else
                {
                    _statistics.InvalidLevels++;
                    Console.WriteLine($"Nível {level.Number}: Inválido - Regenerando...");
                    await RegenerateLevel(level);
                }
                return;
            }

            if (IsOldJsonFormat(level.InitialState))
            {
                Console.WriteLine($"Nível {level.Number}: Formato JSON detectado - Convertendo...");
                try
                {
                    var tubes = ParseOldJsonFormat(level.InitialState);
                    if (IsLevelValid(tubes))
                    {
                        level.InitialState = ConvertToCompactFormat(tubes);
                        level.GenerationSeed = Guid.NewGuid().GetHashCode();
                        
                        _statistics.ConvertedLevels++;
                        _statistics.ValidLevels++;
                        Console.WriteLine($"Nível {level.Number}: Convertido com sucesso ✓");
                    }
                    else
                    {
                        _statistics.InvalidLevels++;
                        Console.WriteLine($"Nível {level.Number}: Conversão inválida - Regenerando...");
                        await RegenerateLevel(level);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao converter nível {level.Number}");
                    _statistics.InvalidLevels++;
                    Console.WriteLine($"Nível {level.Number}: Falha na conversão - Regenerando...");
                    await RegenerateLevel(level);
                }
            }
            else
            {
                _statistics.InvalidLevels++;
                Console.WriteLine($"Nível {level.Number}: Formato desconhecido - Regenerando...");
                await RegenerateLevel(level);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao processar nível {level.Number}");
            _statistics.ErrorLevels++;
            Console.WriteLine($"Nível {level.Number}: Erro fatal ✗");
        }
    }

    private async Task RegenerateLevel(Level level)
    {
        try
        {
            var newLevel = _levelGeneratorService.GenerateLevel(level.Number);
            
            level.InitialState = newLevel.InitialState;
            level.Colors = newLevel.Colors;
            level.Tubes = newLevel.Tubes;
            level.BallsPerColor = newLevel.BallsPerColor;
            level.MinimumMoves = newLevel.MinimumMoves;
            level.GenerationSeed = newLevel.GenerationSeed;
            level.Difficulty = newLevel.Difficulty;
            
            _statistics.RegeneratedLevels++;
            _statistics.ValidLevels++;
            Console.WriteLine($"Nível {level.Number}: Regenerado com sucesso ✓");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao regenerar nível {level.Number}");
            _statistics.ErrorLevels++;
            Console.WriteLine($"Nível {level.Number}: Erro fatal na regeneração ✗");
        }
    }

    private bool IsCompactFormat(string initialState)
    {
        return initialState.StartsWith("T") && initialState.Contains("=");
    }

    private void PrintFinalReport()
    {
        Console.WriteLine("\n=== RELATÓRIO FINAL DE MIGRAÇÃO ===");
        Console.WriteLine($"📊 Níveis já no formato compacto: {_statistics.AlreadyCompact}");
        Console.WriteLine($"🔄 Níveis convertidos de JSON: {_statistics.ConvertedLevels}");
        Console.WriteLine($"🔄 Níveis regenerados: {_statistics.RegeneratedLevels}");
        Console.WriteLine($"✅ Níveis válidos: {_statistics.ValidLevels}");
        Console.WriteLine($"❌ Níveis com erro: {_statistics.ErrorLevels}");
        Console.WriteLine($"📈 Total processado: {_statistics.TotalProcessed}");
        
        var successRate = _statistics.TotalProcessed > 0 
            ? (double)(_statistics.ValidLevels) / _statistics.TotalProcessed * 100 
            : 0;
        Console.WriteLine($"🎯 Taxa de sucesso: {successRate:F2}%");

        _logger.LogInformation("Migração concluída. Estatísticas: " +
            $"Compacto={_statistics.AlreadyCompact}, " +
            $"Convertido={_statistics.ConvertedLevels}, " +
            $"Regenerado={_statistics.RegeneratedLevels}, " +
            $"Válido={_statistics.ValidLevels}, " +
            $"Erro={_statistics.ErrorLevels}");
    }

    private static readonly string[] ColorPalette = {
        "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7",
        "#DDA0DD", "#98D8C8", "#F7DC6F", "#BB8FCE", "#85C1E9",
        "#F8C471", "#82E0AA", "#F1948A", "#D7BDE2", "#A9DFBF"
    };
}

public class MigrationStatistics
{
    public int AlreadyCompact { get; set; } = 0;
    public int ConvertedLevels { get; set; } = 0;
    public int RegeneratedLevels { get; set; } = 0;
    public int ValidLevels { get; set; } = 0;
    public int InvalidLevels { get; set; } = 0;
    public int ErrorLevels { get; set; } = 0;
    
    public int TotalProcessed => AlreadyCompact + ConvertedLevels + RegeneratedLevels + ErrorLevels;
}