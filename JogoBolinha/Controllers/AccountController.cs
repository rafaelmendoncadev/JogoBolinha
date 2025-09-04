using JogoBolinha.Models.ViewModels;
using JogoBolinha.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JogoBolinha.Controllers
{
    public class AccountController : Controller
    {
        private readonly Services.IAuthenticationService _authService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(Services.IAuthenticationService authService, ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.RegisterAsync(model.Username, model.Email, model.Password);
            
            if (result.Success && result.Player != null)
            {
                // Fazer login automático após registro
                await SignInPlayerAsync(result.Player, false);
                
                TempData["SuccessMessage"] = "Conta criada com sucesso! Bem-vindo ao Jogo da Bolinha!";
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = model.ReturnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.LoginAsync(model.UsernameOrEmail, model.Password);
            
            if (result.Success && result.Player != null)
            {
                await SignInPlayerAsync(result.Player, model.RememberMe);
                
                TempData["SuccessMessage"] = "Login realizado com sucesso!";
                
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["InfoMessage"] = "Logout realizado com sucesso!";
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                var player = await _authService.GetPlayerByIdAsync(userId);
                if (player != null)
                {
                    return View(player);
                }
            }
            
            return RedirectToAction("Login");
        }

        // APIs para validação em tempo real
        [HttpGet]
        public async Task<IActionResult> CheckUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Json(new { available = false, message = "Nome de usuário é obrigatório" });
            }

            var available = await _authService.IsUsernameAvailableAsync(username);
            return Json(new { 
                available = available, 
                message = available ? "Nome de usuário disponível" : "Nome de usuário já está em uso" 
            });
        }

        [HttpGet]
        public async Task<IActionResult> CheckEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { available = false, message = "Email é obrigatório" });
            }

            if (!IsValidEmail(email))
            {
                return Json(new { available = false, message = "Email inválido" });
            }

            var available = await _authService.IsEmailAvailableAsync(email);
            return Json(new { 
                available = available, 
                message = available ? "Email disponível" : "Email já está em uso" 
            });
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task SignInPlayerAsync(Models.User.Player player, bool rememberMe)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, player.Id.ToString()),
                new Claim(ClaimTypes.Name, player.Username),
                new Claim(ClaimTypes.Email, player.Email),
                new Claim("SecurityStamp", player.SecurityStamp)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, authProperties);
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}