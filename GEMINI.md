# GEMINI.md

This file provides guidance to Gemini when working with code in this repository.

## Project Overview

This is a web-based Ball Sort Puzzle game built with ASP.NET Core 8.0 MVC. The game is targeted at Portuguese-speaking users. The backend is C# with Entity Framework Core and a SQLite database. The frontend is HTML, CSS, and vanilla JavaScript/jQuery. The project includes a testing project using xUnit and Moq. The application is designed to be deployed to Azure or Railway.

## Building and Running

From the root directory:

### Build and Run
- `cd JogoBolinha && dotnet build` - Build the main application
- `cd JogoBolinha && dotnet run` - Run the development server
- `cd JogoBolinha && dotnet watch run` - Run with hot reload

### Testing
- `dotnet test JogoBolinha.Tests/` - Run all tests
- `dotnet test JogoBolinha.Tests/ --logger "console;verbosity=detailed"` - Run tests with detailed output
- `dotnet test JogoBolinha.Tests/ --collect:"XPlat Code Coverage"` - Run tests with code coverage

### Database Operations
- `cd JogoBolinha && dotnet ef migrations add <MigrationName>` - Add new migration
- `cd JogoBolinha && dotnet ef database update` - Apply migrations to database
- `cd JogoBolinha && dotnet ef database drop` - Drop the database (jogabolinha.db)

## Development Conventions

The project follows a standard ASP.NET Core MVC project structure. It uses a layered architecture with services for business logic. Entity Framework Core is used for data access, with migrations for database schema changes. The project has a comprehensive set of documentation, including PRDs and deployment guides.

### Core Architecture Components

The application follows a layered architecture with clear separation of concerns:

**Data Layer** (`Data/GameDbContext.cs`): 
- EF Core DbContext managing entities with complex relationships
- Optimized with strategic indexes on frequently queried fields

**Service Layer** (`Services/`):
- `GameLogicService` - Core game mechanics and rule validation
- `GameStateManager` - Manages game state persistence and retrieval
- `LevelGeneratorService` - Procedural level generation algorithms
- `ScoreCalculationService` - Complex scoring with bonuses and penalties
- `HintService` - AI-powered hint system (simple and advanced)
- `GameSessionService` - Game session lifecycle management
- `AchievementService` - Achievement tracking and unlocking
- `AuthenticationService` + `PasswordHashService` - Secure user authentication

**Controller Layer**:
- `GameController` - Game gameplay endpoints and state management
- `AccountController` - Authentication and user registration
- `HomeController` - Main navigation and static pages
- `ProfileController` - User profile and statistics management

## Project Structure

```
JogoBolinha/                    # Main ASP.NET Core MVC application
├── Controllers/                # MVC Controllers (Game, Account, Home, Profile)
├── Data/                      # Entity Framework DbContext
├── Models/                    # Domain models
│   ├── Game/                  # Game-related entities (GameState, Level, Tube, Ball, etc.)
│   ├── User/                  # User-related entities (Player, PlayerStats, Achievement, etc.)
│   └── ViewModels/            # MVC ViewModels for UI data transfer
├── Services/                  # Business logic layer
├── Views/                     # Razor views and layouts
├── wwwroot/                   # Static assets (CSS, JS, images)
├── Migrations/                # EF Core database migrations
├── Program.cs                 # Application entry point and DI configuration
└── appsettings.json          # Configuration settings

JogoBolinha.Tests/             # xUnit test project
├── Models/                    # Model unit tests
└── Services/                  # Service unit tests
```
