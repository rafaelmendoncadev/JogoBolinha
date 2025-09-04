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
        context.Database.EnsureCreated();
        
        // Generate initial levels if none exist
        if (!context.Levels.Any())
        {
            var levelGenerator = scope.ServiceProvider.GetRequiredService<LevelGeneratorService>();
            for (int i = 1; i <= 50; i++)
            {
                var level = levelGenerator.GenerateLevel(i);
                context.Levels.Add(level);
            }
            context.SaveChanges();
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
                context.Database.EnsureCreated();
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
