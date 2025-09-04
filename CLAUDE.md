# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Ball Sort Puzzle Game - A web-based puzzle game where players organize colored balls in tubes. Built with ASP.NET Core 8.0 MVC, targeting Portuguese-speaking users.

## Technology Stack

- **Framework:** ASP.NET Core 8.0 MVC (.NET 9.0 for tests)
- **Language:** C# 12
- **Database:** SQLite with Entity Framework Core
- **Frontend:** HTML5, CSS3, Vanilla JavaScript/jQuery
- **Testing:** xUnit with Moq and InMemory database
- **Authentication:** Cookie-based authentication
- **Development:** Git version control

## Common Development Commands

From the root directory:

### Build and Run
- `cd JogoBolinha && dotnet build` - Build the main application
- `cd JogoBolinha && dotnet run` - Run the development server
- `cd JogoBolinha && dotnet watch run` - Run with hot reload

### Testing
- `dotnet test JogoBolinha.Tests/` - Run all tests (25 tests currently)
- `dotnet test JogoBolinha.Tests/ --logger "console;verbosity=detailed"` - Run tests with detailed output
- `dotnet test JogoBolinha.Tests/ --collect:"XPlat Code Coverage"` - Run tests with code coverage

### Database Operations
- `cd JogoBolinha && dotnet ef migrations add <MigrationName>` - Add new migration
- `cd JogoBolinha && dotnet ef database update` - Apply migrations to database
- `cd JogoBolinha && dotnet ef database drop` - Drop the database (jogabolinha.db)

## Project Architecture

### Core Architecture Components

The application follows a layered architecture with clear separation of concerns:

**Data Layer** (`Data/GameDbContext.cs`): 
- EF Core DbContext managing 11 entities with complex relationships
- Optimized with strategic indexes on frequently queried fields
- Cascade and restrict delete behaviors for referential integrity

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

### Database Design Principles

The database schema uses sophisticated Entity Framework relationships:
- **One-to-One**: Player ↔ PlayerStats (cascade delete)
- **One-to-Many**: GameState → Tubes/Balls/Moves (cascade delete)
- **Many-to-One**: GameState → Level (restrict delete to preserve history)
- **Many-to-Many**: Player ↔ Achievements via PlayerAchievement junction table

Key performance indexes:
- Unique indexes on Player.Username and Player.Email
- Composite index on GameSession(PlayerId, LevelId) for leaderboards
- Score indexes on Leaderboard for ranking queries

## Game Mechanics

### Core Rules
- Move balls by clicking source ball then destination tube
- Only top ball in tube can be moved
- Ball can only be placed on same color or in empty tube
- Goal: Sort all balls by color into separate tubes

### Difficulty Progression
- Levels 1-10: Tutorial (2-3 colors, 3-4 tubes)
- Levels 11-30: Medium (3-4 colors, 4-5 tubes)
- Levels 31-50: Hard (4-5 colors, 5-6 tubes)
- Levels 51+: Expert (5-6 colors, 6-7 tubes)

### Scoring System
- Base: 100 points per level
- Efficiency bonus: +10 points per unused move
- Speed bonus: +50 points if completed in <2 minutes
- Hint penalty: -20 points per hint used

## Key Features to Implement

### MVP Features
1. Core game mechanics with drag/drop or click interaction
2. Progressive level system with 50+ levels
3. Undo system (up to 3 moves)
4. Hint system (simple and advanced hints)
5. Score calculation and tracking

### Advanced Features
1. User profiles with statistics
2. Global and weekly leaderboards  
3. Achievement system (15 different achievements)
4. Visual themes and customization
5. Responsive design for desktop/mobile

## Development Guidelines

### Performance Requirements
- Page load time: <3 seconds
- UI responsiveness: <100ms
- Support for 100+ concurrent users
- Cross-browser compatibility (Chrome, Firefox, Safari, Edge)

### Accessibility
- WCAG 2.1 AA compliance
- Keyboard navigation support
- Screen reader compatibility
- Colorblind-friendly options (symbols + colors)

### Code Standards
- Use async/await patterns for database operations
- Implement proper error handling and validation
- Follow MVC separation of concerns
- Write unit tests for game logic services

## Sprint Progress
Based on the PRD, Sprint 1 (setup and core models) is complete. Focus development on Sprint 2-6 features: UI implementation, game features, polishing, social features, and deployment.