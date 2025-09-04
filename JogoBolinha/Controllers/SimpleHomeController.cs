using Microsoft.AspNetCore.Mvc;

namespace JogoBolinha.Controllers;

public class SimpleHomeController : Controller
{
    [HttpGet]
    [Route("simple")]
    public IActionResult SimpleIndex()
    {
        ViewBag.Message = "Simple Home Page Working";
        ViewBag.Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
        ViewBag.Time = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
        
        return View();
    }
}