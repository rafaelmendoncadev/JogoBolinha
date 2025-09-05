-- Delete existing problematic levels to force regeneration
DELETE FROM GameStates WHERE LevelId IN (SELECT Id FROM Levels WHERE Number = 4);
DELETE FROM GameMoves WHERE GameStateId NOT IN (SELECT Id FROM GameStates);
DELETE FROM Balls WHERE GameStateId NOT IN (SELECT Id FROM GameStates);  
DELETE FROM Tubes WHERE GameStateId NOT IN (SELECT Id FROM GameStates);
DELETE FROM Levels WHERE Number = 4;

-- Also clean up other potentially problematic early levels
DELETE FROM GameStates WHERE LevelId IN (SELECT Id FROM Levels WHERE Number <= 10);
DELETE FROM GameMoves WHERE GameStateId NOT IN (SELECT Id FROM GameStates);
DELETE FROM Balls WHERE GameStateId NOT IN (SELECT Id FROM GameStates);
DELETE FROM Tubes WHERE GameStateId NOT IN (SELECT Id FROM GameStates);
DELETE FROM Levels WHERE Number <= 10;

-- Force regeneration of all early levels with new logic
SELECT 'Levels deleted, will be regenerated with new solvable logic' as Status;