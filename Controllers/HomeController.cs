using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using OpenTrainDrive.Models;

namespace OpenTrainDrive.Controllers;

public class HomeController : Controller
{
    private readonly IHostApplicationLifetime _lifetime;

    public HomeController(IHostApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    

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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
