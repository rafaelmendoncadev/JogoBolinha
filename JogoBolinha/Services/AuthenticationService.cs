using JogoBolinha.Data;
using JogoBolinha.Models.User;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JogoBolinha.Services
{
    public interface IAuthenticationService
    {
        Task<(bool Success, string Message, Player? Player)> RegisterAsync(string username, string email, string password);
        Task<(bool Success, string Message, Player? Player)> LoginAsync(string usernameOrEmail, string password);
        Task<bool> IsUsernameAvailableAsync(string username);
        Task<bool> IsEmailAvailableAsync(string email);
        Task<Player?> GetPlayerByIdAsync(int id);
        Task<bool> UpdateLastLoginAsync(int playerId);
        Task<bool> IncrementFailedLoginAttemptsAsync(string usernameOrEmail);
        Task<bool> ResetFailedLoginAttemptsAsync(int playerId);
        Task<bool> LockoutPlayerAsync(int playerId, TimeSpan lockoutDuration);
        Task<bool> IsPlayerLockedOutAsync(int playerId);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly GameDbContext _context;
        private readonly IPasswordHashService _passwordHashService;
        private const int MaxFailedAttempts = 5;
        private readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public AuthenticationService(GameDbContext context, IPasswordHashService passwordHashService)
        {
            _context = context;
            _passwordHashService = passwordHashService;
        }

        public async Task<(bool Success, string Message, Player? Player)> RegisterAsync(string username, string email, string password)
        {
            try
            {
                // Validações básicas
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                {
                    return (false, "Todos os campos são obrigatórios.", null);
                }

                if (password.Length < 6)
                {
                    return (false, "A senha deve ter pelo menos 6 caracteres.", null);
                }

                // Verificar se username já existe
                if (!await IsUsernameAvailableAsync(username))
                {
                    return (false, "Nome de usuário já está em uso.", null);
                }

                // Verificar se email já existe
                if (!await IsEmailAvailableAsync(email))
                {
                    return (false, "Email já está em uso.", null);
                }

                // Gerar salt e hash da senha
                var salt = _passwordHashService.GenerateSalt();
                var passwordHash = _passwordHashService.HashPassword(password, salt);

                // Criar novo player
                var player = new Player
                {
                    Username = username.Trim(),
                    Email = email.Trim().ToLowerInvariant(),
                    PasswordHash = passwordHash,
                    PasswordSalt = salt,
                    EmailConfirmed = false,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    FailedLoginAttempts = 0,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Players.Add(player);
                await _context.SaveChangesAsync();

                return (true, "Conta criada com sucesso!", player);
            }
            catch (Exception)
            {
                return (false, "Erro interno do servidor. Tente novamente.", null);
            }
        }

        public async Task<(bool Success, string Message, Player? Player)> LoginAsync(string usernameOrEmail, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
                {
                    return (false, "Nome de usuário/email e senha são obrigatórios.", null);
                }

                // Buscar player por username ou email
                var player = await _context.Players
                    .FirstOrDefaultAsync(p => p.Username == usernameOrEmail || p.Email == usernameOrEmail.ToLowerInvariant());

                if (player == null)
                {
                    return (false, "Credenciais inválidas.", null);
                }

                // Verificar se a conta está ativa
                if (!player.IsActive)
                {
                    return (false, "Conta desativada. Entre em contato com o suporte.", null);
                }

                // Verificar se a conta está bloqueada
                if (await IsPlayerLockedOutAsync(player.Id))
                {
                    return (false, "Conta temporariamente bloqueada devido a muitas tentativas de login falhadas. Tente novamente mais tarde.", null);
                }

                // Verificar senha
                if (!_passwordHashService.VerifyPassword(password, player.PasswordHash, player.PasswordSalt))
                {
                    await IncrementFailedLoginAttemptsAsync(usernameOrEmail);
                    return (false, "Credenciais inválidas.", null);
                }

                // Login bem-sucedido
                await ResetFailedLoginAttemptsAsync(player.Id);
                await UpdateLastLoginAsync(player.Id);

                return (true, "Login realizado com sucesso!", player);
            }
            catch (Exception)
            {
                return (false, "Erro interno do servidor. Tente novamente.", null);
            }
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            return !await _context.Players.AnyAsync(p => p.Username == username);
        }

        public async Task<bool> IsEmailAvailableAsync(string email)
        {
            return !await _context.Players.AnyAsync(p => p.Email == email.ToLowerInvariant());
        }

        public async Task<Player?> GetPlayerByIdAsync(int id)
        {
            return await _context.Players.FindAsync(id);
        }

        public async Task<bool> UpdateLastLoginAsync(int playerId)
        {
            try
            {
                var player = await _context.Players.FindAsync(playerId);
                if (player != null)
                {
                    player.LastLogin = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IncrementFailedLoginAttemptsAsync(string usernameOrEmail)
        {
            try
            {
                var player = await _context.Players
                    .FirstOrDefaultAsync(p => p.Username == usernameOrEmail || p.Email == usernameOrEmail.ToLowerInvariant());
                
                if (player != null)
                {
                    player.FailedLoginAttempts++;
                    
                    // Se atingiu o máximo de tentativas, bloquear a conta
                    if (player.FailedLoginAttempts >= MaxFailedAttempts)
                    {
                        player.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                    }
                    
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ResetFailedLoginAttemptsAsync(int playerId)
        {
            try
            {
                var player = await _context.Players.FindAsync(playerId);
                if (player != null)
                {
                    player.FailedLoginAttempts = 0;
                    player.LockoutEnd = null;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> LockoutPlayerAsync(int playerId, TimeSpan lockoutDuration)
        {
            try
            {
                var player = await _context.Players.FindAsync(playerId);
                if (player != null)
                {
                    player.LockoutEnd = DateTime.UtcNow.Add(lockoutDuration);
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsPlayerLockedOutAsync(int playerId)
        {
            try
            {
                var player = await _context.Players.FindAsync(playerId);
                return player?.LockoutEnd.HasValue == true && player.LockoutEnd > DateTime.UtcNow;
            }
            catch
            {
                return false;
            }
        }
    }
}