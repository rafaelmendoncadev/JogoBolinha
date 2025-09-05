using Microsoft.EntityFrameworkCore;
using JogoBolinha.Data;
using JogoBolinha.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Configure for Railway deployment
if (Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT") != null)
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure SQLite database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=jogabolinha.db";
builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlite(connectionString));

// Configure Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
            ? CookieSecurePolicy.SameAsRequest 
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Events.OnRedirectToLogin = context =>
        {
            // Don't redirect AJAX requests to login page
            if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                context.Response.StatusCode = 401;
                return Task.CompletedTask;
            }
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });

// Register game services
builder.Services.AddScoped<GameLogicService>();
builder.Services.AddScoped<LevelGeneratorService>();
builder.Services.AddScoped<LevelGeneratorServiceV2>(); // Novo serviço de geração melhorado
builder.Services.AddScoped<ScoreCalculationService>();
builder.Services.AddScoped<AchievementService>();
builder.Services.AddScoped<GameSessionService>();
builder.Services.AddScoped<HintService>();
builder.Services.AddScoped<GameStateManager>();

// Register authentication services
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

var app = builder.Build();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    try
    {
        context.Database.Migrate();
        
        // Use o novo gerador de níveis V2
        var levelGeneratorV2 = scope.ServiceProvider.GetRequiredService<LevelGeneratorServiceV2>();
        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        // Verificar se devemos regenerar todos os níveis (forçar regeneração)
        bool forceRegenerate = builder.Configuration.GetValue<bool>("RegenerateLevels", false) || 
                              Environment.GetEnvironmentVariable("REGENERATE_LEVELS") == "true";
        
        if (forceRegenerate)
        {
            startupLogger.LogWarning("REGENERATE_LEVELS flag detectada. Regenerando TODOS os níveis...");
            
            // Deletar todos os game states existentes
            var allGameStates = context.GameStates.ToList();
            foreach (var gameState in allGameStates)
            {
                var moves = context.GameMoves.Where(gm => gm.GameStateId == gameState.Id);
                var balls = context.Balls.Where(b => b.GameStateId == gameState.Id);
                var tubes = context.Tubes.Where(t => t.GameStateId == gameState.Id);
                
                context.GameMoves.RemoveRange(moves);
                context.Balls.RemoveRange(balls);
                context.Tubes.RemoveRange(tubes);
            }
            context.GameStates.RemoveRange(allGameStates);
            
            // Deletar todos os níveis
            context.Levels.RemoveRange(context.Levels);
            context.SaveChanges();
            
            startupLogger.LogWarning("Todos os níveis e game states foram removidos. Regenerando...");
        }
        
        // Check for problematic levels and regenerate them
        var problematicLevels = context.Levels.Where(l => 
            l.Tubes < l.Colors + 2 || // Regra de solvabilidade
            (l.Number <= 10 && l.Colors > 4) || // Níveis fáceis não devem ter mais de 4 cores
            (l.Number == 4 && l.Tubes < 5) || // Nível 4 específico deve ter pelo menos 5 tubos
            (l.Number <= 3 && l.Colors > 2) // Níveis 1-3 devem ter no máximo 2 cores
        ).ToList();
        
        if (problematicLevels.Any())
        {
            startupLogger.LogWarning("Encontrados {Count} níveis problemáticos. Regenerando com nova lógica V2...", problematicLevels.Count);
            
            foreach (var problematicLevel in problematicLevels)
            {
                startupLogger.LogWarning("Regenerando Nível {Number}: cores={Colors}, tubos={Tubes}", 
                    problematicLevel.Number, problematicLevel.Colors, problematicLevel.Tubes);
                
                // Delete existing level and related data
                var gameStates = context.GameStates.Where(gs => gs.LevelId == problematicLevel.Id).ToList();
                foreach (var gameState in gameStates)
                {
                    var moves = context.GameMoves.Where(gm => gm.GameStateId == gameState.Id);
                    var balls = context.Balls.Where(b => b.GameStateId == gameState.Id);
                    var tubes = context.Tubes.Where(t => t.GameStateId == gameState.Id);
                    
                    context.GameMoves.RemoveRange(moves);
                    context.Balls.RemoveRange(balls);
                    context.Tubes.RemoveRange(tubes);
                }
                context.GameStates.RemoveRange(gameStates);
                context.Levels.Remove(problematicLevel);
                
                // Generate new solvable level using V2
                var newLevel = levelGeneratorV2.GenerateLevel(problematicLevel.Number);
                context.Levels.Add(newLevel);
            }
            context.SaveChanges();
            startupLogger.LogWarning("Regenerados {Count} níveis problemáticos com sucesso", problematicLevels.Count);
        }

        // Align early levels (1-20) to V2 configs for proper progression and solvability
        try
        {
            for (int n = 1; n <= 20; n++)
            {
                var existing = context.Levels.FirstOrDefault(l => l.Number == n);
                var expected = levelGeneratorV2.GenerateLevel(n);

                // If missing or significantly different, replace
                bool needsReplace = existing == null
                    || existing.Colors != expected.Colors
                    || existing.BallsPerColor != expected.BallsPerColor
                    || existing.Tubes < expected.Colors + 2
                    || existing.Tubes < expected.Tubes || (n == 2 && existing.InitialState != expected.InitialState); // garantir tutorial fixo para n�vel 2

                if (needsReplace)
                {
                    startupLogger.LogWarning("Ajustando Nível {Number} para nova configuração V2", n);

                    if (existing != null)
                    {
                        var gameStates = context.GameStates.Where(gs => gs.LevelId == existing.Id).ToList();
                        foreach (var gameState in gameStates)
                        {
                            var moves = context.GameMoves.Where(gm => gm.GameStateId == gameState.Id);
                            var balls = context.Balls.Where(b => b.GameStateId == gameState.Id);
                            var tubes = context.Tubes.Where(t => t.GameStateId == gameState.Id);

                            context.GameMoves.RemoveRange(moves);
                            context.Balls.RemoveRange(balls);
                            context.Tubes.RemoveRange(tubes);
                        }
                        context.GameStates.RemoveRange(gameStates);
                        context.Levels.Remove(existing);
                        context.SaveChanges();
                    }

                    context.Levels.Add(expected);
                }
            }
            context.SaveChanges();
        }
        catch (Exception ex)
        {
            var startupLogger2 = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            startupLogger2.LogError(ex, "Falha ao alinhar níveis iniciais V2");
        }
        
        // Generate initial levels if none exist (usando V2)
        if (!context.Levels.Any())
        {
            startupLogger.LogInformation("Nenhum nível encontrado. Gerando 50 níveis iniciais com LevelGeneratorServiceV2...");
            for (int i = 1; i <= 50; i++)
            {
                var level = levelGeneratorV2.GenerateLevel(i);
                context.Levels.Add(level);
                
                if (i <= 10)
                {
                    startupLogger.LogInformation("Nível {Number} criado: {Colors} cores, {Tubes} tubos, {BallsPerColor} bolas/cor", 
                        level.Number, level.Colors, level.Tubes, level.BallsPerColor);
                }
            }
            context.SaveChanges();
            startupLogger.LogInformation("50 níveis criados com sucesso!");
        }
    }
    catch (Exception ex)
    {
        // Log the exception but don't fail the application startup
        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        startupLogger.LogError(ex, "An error occurred while creating the database. Details: {Message}", ex.Message);
        
        // In production, try to at least ensure the database file exists
        if (!app.Environment.IsDevelopment())
        {
            try 
            {
                var fallbackConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                startupLogger.LogInformation("Attempting to create database at: {ConnectionString}", fallbackConnectionString);
                context.Database.Migrate();
            }
            catch (Exception dbEx)
            {
                startupLogger.LogError(dbEx, "Failed to create database even with fallback attempt");
            }
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // Temporarily disable HSTS for debugging
    // app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Add detailed logging middleware for debugging
app.Use(async (context, next) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("[REQUEST] {Method} {Path} | Remote: {RemoteIp} | Host: {Host} | Protocol: {Protocol}", 
        context.Request.Method, 
        context.Request.Path, 
        context.Connection.RemoteIpAddress,
        context.Request.Host,
        context.Request.Protocol);
    
    try
    {
        await next();
        
        logger.LogInformation("[RESPONSE] Status: {StatusCode} | {Method} {Path} | Content-Type: {ContentType}", 
            context.Response.StatusCode,
            context.Request.Method, 
            context.Request.Path,
            context.Response.ContentType);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ERROR] Exception in {Method} {Path}", 
            context.Request.Method, 
            context.Request.Path);
        throw;
    }
});

// Configure HTTPS redirection based on environment
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
else if (Environment.GetEnvironmentVariable("RAILWAY_ENVIRONMENT") != null)
{
    // Railway provides HTTPS termination at the edge, so we don't need HTTPS redirection
    // But we should trust forwarded headers
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
    });
}
else
{
    // For other production environments (like Azure)
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Add a simple root endpoint for testing
app.MapGet("/test", () => new 
{ 
    status = "OK", 
    time = DateTime.UtcNow, 
    environment = app.Environment.EnvironmentName 
});

// Log application startup
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application starting. Environment: {Environment}, ContentRoot: {ContentRoot}", 
    app.Environment.EnvironmentName, 
    app.Environment.ContentRootPath);

app.Run();
