#r "nuget: Microsoft.EntityFrameworkCore.Sqlite, 8.0.0"
#r "JogoBolinha/bin/Release/net8.0/JogoBolinha.dll"

using Microsoft.EntityFrameworkCore;
using JogoBolinha.Data;

var connectionString = "Data Source=JogoBolinha/jogabolinha.db";
var options = new DbContextOptionsBuilder<GameDbContext>()
    .UseSqlite(connectionString)
    .Options;

using var context = new GameDbContext(options);

Console.WriteLine("Recent GameStates (last 10):");
Console.WriteLine("Id\tPlayerId\tStatus\t\tMovesCount\tLastModified/StartTime");
Console.WriteLine("-------------------------------------------------------------------");

var recentGameStates = await context.GameStates
    .OrderByDescending(gs => gs.LastModified ?? gs.StartTime)
    .Take(10)
    .Select(gs => new {
        gs.Id,
        gs.PlayerId,
        gs.Status,
        gs.MovesCount,
        DateTime = gs.LastModified ?? gs.StartTime
    })
    .ToListAsync();

foreach (var gs in recentGameStates)
{
    Console.WriteLine($"{gs.Id}\t{gs.PlayerId?.ToString() ?? "NULL"}\t\t{gs.Status}\t\t{gs.MovesCount}\t\t{gs.DateTime:yyyy-MM-dd HH:mm:ss}");
}

// Count orphaned GameStates (without PlayerId)
var orphanedCount = await context.GameStates
    .Where(gs => gs.PlayerId == null)
    .CountAsync();

Console.WriteLine($"\nOrphaned GameStates (no PlayerId): {orphanedCount}");

// Count GameStates with PlayerId
var adoptedCount = await context.GameStates
    .Where(gs => gs.PlayerId != null)
    .CountAsync();

Console.WriteLine($"Adopted GameStates (with PlayerId): {adoptedCount}");