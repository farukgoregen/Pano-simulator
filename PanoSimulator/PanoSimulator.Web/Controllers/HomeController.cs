using Microsoft.AspNetCore.Mvc;

namespace PanoSimulator.Web.Controllers;

public class HomeController : Controller
{
    // Seviye tanımlarını ileride LevelService üzerinden alacağız (Faz 6).
    // Şimdilik hardcoded liste — JSON'dan okuma Faz 6'da gelecek.
    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
