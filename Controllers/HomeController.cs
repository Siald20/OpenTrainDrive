using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using OpenTrainDrive.Models;

namespace OpenTrainDrive.Controllers;

/// <summary>
/// MVC-Controller fuer die Standardseiten und das Beenden der Anwendung.
/// </summary>
public class HomeController : Controller
{
    private readonly IHostApplicationLifetime _lifetime;

    /// <summary>
    /// Erstellt den Controller und erhaelt den Application-Lifetime-Dienst.
    /// </summary>
    /// <param name="lifetime">Lifetime-Instanz der Host-Anwendung.</param>
    public HomeController(IHostApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
    }

    /// <summary>
    /// Startseite.
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Datenschutzhinweise.
    /// </summary>
    public IActionResult Privacy()
    {
        return View();
    }

    

    /// <summary>
    /// Stoppt die Anwendung kontrolliert.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Stop()
    {
        // Stop the application gracefully and then force exit
        Task.Run(() => {
            _lifetime.StopApplication();
            System.Threading.Thread.Sleep(500); // Give time for graceful shutdown
            Environment.Exit(0);
        });
        return Content("Application stopping...");
    }

    /// <summary>
    /// Fehleransicht mit Request-ID.
    /// </summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
