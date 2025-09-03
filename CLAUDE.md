# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Ball Sort Puzzle Game - A web-based puzzle game where players organize colored balls in tubes. Built with ASP.NET Core 8.0 MVC, targeting Portuguese-speaking users.

## Technology Stack

- **Framework:** ASP.NET Core 8.0 MVC
- **Language:** C# 12
- **Database:** SQLite with Entity Framework Core
- **Frontend:** HTML5, CSS3, Vanilla JavaScript/jQuery
- **Development:** Git version control

## Project Architecture

### MVC Structure
```
Controllers/
├── HomeController.cs      - Main menu and navigation
├── GameController.cs      - Core game functionality
├── ProfileController.cs   - User profiles and stats
└── LeaderboardController.cs - Rankings and competition

Models/
├── Game/
│   ├── GameState.cs      - Current game state
│   ├── Tube.cs          - Game tube container
│   ├── Ball.cs          - Ball entity
│   └── Level.cs         - Level configuration
├── User/
│   ├── Player.cs        - Player entity
│   ├── PlayerStats.cs   - Player statistics
│   └── Achievement.cs   - Achievement system
└── ViewModels/          - View-specific models

Services/
├── GameLogicService.cs          - Core game rules and validation
├── LevelGeneratorService.cs     - Dynamic level creation
├── ScoreCalculationService.cs   - Scoring algorithm
└── AchievementService.cs        - Achievement tracking
```

### Database Entities
- **Players:** User profiles and authentication
- **GameSessions:** Individual game records with scores and moves
- **Levels:** Level definitions and solutions
- **Achievements:** Achievement system with player progress
- **Leaderboards:** Global and weekly rankings

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