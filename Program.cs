using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

// Temporary file logging to capture startup/shutdown errors
try
{
    var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
    Directory.CreateDirectory(logsDir);
    var logPath = Path.Combine(logsDir, "app.log");
    var logStream = new StreamWriter(new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
    Console.SetOut(logStream);
    Console.SetError(logStream);
    AppDomain.CurrentDomain.UnhandledException += (s, e) => logStream.WriteLine($"UnhandledException: {e.ExceptionObject}");
    TaskScheduler.UnobservedTaskException += (s, e) => { logStream.WriteLine($"UnobservedTaskException: {e.Exception}"); e.SetObserved(); };
    logStream.WriteLine($"--- App start {DateTime.Now:O} ---");
}
catch
{
    // if logging setup fails, continue without file logging
}

// Write quick markers so we can detect start/stop even if console is redirected
try
{
    var markersDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
    Directory.CreateDirectory(markersDir);
    File.WriteAllText(Path.Combine(markersDir, "started.txt"), DateTime.Now.ToString("O"));
    AppDomain.CurrentDomain.ProcessExit += (s, e) =>
    {
        try { File.WriteAllText(Path.Combine(markersDir, "stopped.txt"), DateTime.Now.ToString("O")); } catch { }
    };
}
catch { }

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Open the default browser once the application has started and server addresses are available
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        var server = app.Services.GetRequiredService<IServer>();
        var addressesFeature = server.Features.Get<IServerAddressesFeature>();
        var url = addressesFeature?.Addresses?.FirstOrDefault() ?? "http://localhost:5000";
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open browser for {url}: {ex}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error attempting to open browser: {ex}");
    }
});

app.Run();

