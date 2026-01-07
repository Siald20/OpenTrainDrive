using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;

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

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "Elements")),
    RequestPath = "/elements"
});

// Serve generated plan.xml explicitly without exposing entire content root
app.MapGet("/plan.xml", () =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "plan.xml");
    if (!File.Exists(path))
    {
        return Results.NotFound();
    }
    return Results.File(path, "application/xml");
});

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Open default browser at the first bound URL (configurable via OTD_AUTO_OPEN=false)
var autoOpen = !string.Equals(app.Configuration["OTD_AUTO_OPEN"], "false", StringComparison.OrdinalIgnoreCase);
if (autoOpen)
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        try
        {
            var server = app.Services.GetRequiredService<IServer>();
            var addressesFeature = server.Features.Get<IServerAddressesFeature>();
            var url = addressesFeature?.Addresses?.FirstOrDefault() ?? "http://localhost:65001";
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch (Exception)
            {
                // ignore if shell cannot start a browser in this environment
            }
        }
        catch
        {
            // ignore failures obtaining server addresses
        }
    });
}

app.Run();

