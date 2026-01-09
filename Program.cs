using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;
using System.Xml.Linq;
using System.Globalization;
using OpenTrainDrive.Models;

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
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "Loco")),
    RequestPath = "/loco"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "drive")),
    RequestPath = "/drive"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "waggon")),
    RequestPath = "/waggon"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "Train")),
    RequestPath = "/train"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "timetable")),
    RequestPath = "/timetable"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "timetable-viewer")),
    RequestPath = "/timetable-viewer"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "ZD")),
    RequestPath = "/zd"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "auth")),
    RequestPath = "/auth"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "exit")),
    RequestPath = "/exit"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "Symbols")),
    RequestPath = "/symbols"
});

bool IsUsersEnabled(IWebHostEnvironment env)
{
    var path = Path.Combine(env.ContentRootPath, "settings.xml");
    if (!File.Exists(path))
    {
        return false;
    }
    try
    {
        var doc = XDocument.Load(path);
        var general = doc.Root?.Element("general");
        var attr = general?.Attribute("usersEnabled")?.Value ?? "false";
        if (!string.Equals(attr, "true", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        var usersPath = Path.Combine(env.ContentRootPath, "users.xml");
        if (!File.Exists(usersPath))
        {
            return false;
        }
        var usersDoc = XDocument.Load(usersPath);
        return usersDoc.Root?.Elements("user").Any() == true;
    }
    catch
    {
        return false;
    }
}

async Task<List<UserDto>> LoadUsers(IWebHostEnvironment env)
{
    var path = Path.Combine(env.ContentRootPath, "users.xml");
    if (!File.Exists(path))
    {
        return new List<UserDto>();
    }
    try
    {
        var doc = await Task.Run(() => XDocument.Load(path));
        var users = doc.Root?.Elements("user") ?? Enumerable.Empty<XElement>();
        return users.Select(user => new UserDto(
            user.Attribute("name")?.Value,
            user.Attribute("password")?.Value,
            user.Attribute("role")?.Value ?? "user",
            string.Equals(user.Attribute("enabled")?.Value, "true", StringComparison.OrdinalIgnoreCase)
        )).ToList();
    }
    catch
    {
        return new List<UserDto>();
    }
}

(string user, string role) GetUserFromCookie(HttpContext context)
{
    if (!context.Request.Cookies.TryGetValue("otd-user", out var value) || string.IsNullOrWhiteSpace(value))
    {
        return (string.Empty, string.Empty);
    }
    var parts = value.Split('|');
    if (parts.Length < 2)
    {
        return (string.Empty, string.Empty);
    }
    return (parts[0], parts[1]);
}

bool IsAdminUser(HttpContext context)
{
    if (!IsUsersEnabled(app.Environment))
    {
        return true;
    }
    var (_, role) = GetUserFromCookie(context);
    return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
}
app.MapGet("/settings/settings.css", () =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "OTD", "settings.css");
    if (!File.Exists(path))
    {
        return Results.NotFound();
    }
    return Results.File(path, "text/css");
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

// Plan speichern
app.MapPost("/plan/save", async (PlanSaveDto plan, HttpContext context) =>
{
    if (IsUsersEnabled(app.Environment) && !IsAdminUser(context))
    {
        return Results.Forbid();
    }
    var path = Path.Combine(app.Environment.ContentRootPath, "plan.xml");
    var gridSize = plan.GridSize <= 0 ? 48 : plan.GridSize;
    var doc = new XDocument(
        new XElement("plan",
            new XAttribute("grid", gridSize.ToString(CultureInfo.InvariantCulture)),
            new XElement("symbols",
                (plan.Symbols ?? new List<PlanSymbolDto>()).Select(symbol =>
                {
                    var element = new XElement("symbol",
                        new XAttribute("id", string.IsNullOrWhiteSpace(symbol.Id) ? Guid.NewGuid().ToString() : symbol.Id),
                        new XAttribute("type", symbol.Type ?? string.Empty),
                        new XAttribute("classes", symbol.Classes ?? string.Empty),
                        new XAttribute("x", symbol.X.ToString(CultureInfo.InvariantCulture)),
                        new XAttribute("y", symbol.Y.ToString(CultureInfo.InvariantCulture))
                    );
                    var configFields = symbol.Config ?? new List<PlanConfigFieldDto>();
                    if (configFields.Count > 0)
                    {
                        var config = new XElement("config",
                            configFields.Select(field =>
                                new XElement("field",
                                    new XAttribute("key", field.Key ?? string.Empty),
                                    new XAttribute("value", field.Value ?? string.Empty)
                                ))
                        );
                        element.Add(config);
                    }
                    return element;
                })
            )
        )
    );
    await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
    await doc.SaveAsync(stream, SaveOptions.None, default);
    return Results.Ok(new { saved = plan.Symbols?.Count ?? 0 });
});

// Einstellungen lesen
app.MapGet("/settings.xml", () =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "settings.xml");
    if (!File.Exists(path))
    {
        return Results.NotFound();
    }
    return Results.File(path, "application/xml");
});

// Einstellungen speichern
app.MapPost("/settings/save", async (SettingsDto settings, HttpContext context) =>
{
    if (IsUsersEnabled(app.Environment) && !IsAdminUser(context))
    {
        return Results.Forbid();
    }
    var path = Path.Combine(app.Environment.ContentRootPath, "settings.xml");
    static string Bool(bool value) => value ? "true" : "false";
    static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);
    static string Num(double value) => value.ToString(CultureInfo.InvariantCulture);

    var doc = new XDocument(
        new XElement("settings",
            new XElement("general",
                new XAttribute("project", settings.ProjectName ?? string.Empty),
                new XAttribute("language", settings.Language ?? "de"),
                new XAttribute("autosave", Bool(settings.AutoSave)),
                new XAttribute("autosaveInterval", Int(settings.AutoSaveInterval)),
                new XAttribute("autoOpen", Bool(settings.AutoOpen)),
                new XAttribute("usersEnabled", Bool(settings.UsersEnabled))
            ),
            new XElement("ui",
                new XAttribute("theme", settings.Theme ?? "classic"),
                new XAttribute("density", settings.Density ?? "compact"),
                new XAttribute("tooltips", Bool(settings.ShowTooltips)),
                new XAttribute("clock", Bool(settings.ShowClock)),
                new XAttribute("statusbar", Bool(settings.ShowStatusbar))
            ),
            new XElement("connection",
                new XAttribute("system", settings.System ?? "dcc"),
                new XAttribute("host", settings.Host ?? "localhost"),
                new XAttribute("port", Int(settings.Port)),
                new XAttribute("baud", Int(settings.Baud)),
                new XAttribute("autoconnect", Bool(settings.AutoConnect)),
                new XAttribute("heartbeat", Int(settings.Heartbeat))
            ),
            new XElement("operation",
                new XAttribute("maxSpeed", Num(settings.MaxSpeed)),
                new XAttribute("accelFactor", Num(settings.AccelFactor)),
                new XAttribute("brakeFactor", Num(settings.BrakeFactor)),
                new XAttribute("stopOnSignal", Bool(settings.StopOnSignal)),
                new XAttribute("emergencyStop", Bool(settings.EmergencyStop))
            ),
            new XElement("editor",
                new XAttribute("grid", Int(settings.GridSize)),
                new XAttribute("snap", Bool(settings.Snap)),
                new XAttribute("showLabels", Bool(settings.ShowLabels)),
                new XAttribute("showIds", Bool(settings.ShowIds)),
                new XAttribute("confirmDelete", Bool(settings.ConfirmDelete))
            ),
            new XElement("logging",
                new XAttribute("level", settings.LogLevel ?? "info"),
                new XAttribute("keepDays", Int(settings.KeepDays)),
                new XAttribute("path", settings.LogPath ?? "logs")
            )
        )
    );
    await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
    await doc.SaveAsync(stream, SaveOptions.None, default);
    return Results.Ok(new { saved = true });
});

// Benutzer lesen
app.MapGet("/users.xml", (HttpContext context) =>
{
    if (IsUsersEnabled(app.Environment) && !IsAdminUser(context))
    {
        return Results.Forbid();
    }
    var path = Path.Combine(app.Environment.ContentRootPath, "users.xml");
    if (!File.Exists(path))
    {
        return Results.NotFound();
    }
    return Results.File(path, "application/xml");
});

// Benutzer speichern
app.MapPost("/users/save", async (List<UserDto> users, HttpContext context) =>
{
    if (IsUsersEnabled(app.Environment) && !IsAdminUser(context))
    {
        return Results.Forbid();
    }
    var path = Path.Combine(app.Environment.ContentRootPath, "users.xml");
    var doc = new XDocument(
        new XElement("users",
            (users ?? new List<UserDto>()).Select(user => new XElement("user",
                new XAttribute("name", user.Name ?? string.Empty),
                new XAttribute("password", user.Password ?? string.Empty),
                new XAttribute("role", user.Role ?? "user"),
                new XAttribute("enabled", user.Enabled ? "true" : "false")
            ))
        )
    );
    await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
    await doc.SaveAsync(stream, SaveOptions.None, default);
    return Results.Ok(new { saved = users?.Count ?? 0 });
});

// Auth Status
app.MapGet("/auth/status", (HttpContext context) =>
{
    var enabled = IsUsersEnabled(app.Environment);
    if (!enabled)
    {
        return Results.Ok(new { enabled = false, user = string.Empty, role = "admin", isAdmin = true });
    }
    var (user, role) = GetUserFromCookie(context);
    return Results.Ok(new { enabled = true, user, role, isAdmin = string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase) });
});

// Auth Login
app.MapPost("/auth/login", async (AuthLoginDto login, HttpContext context) =>
{
    if (!IsUsersEnabled(app.Environment))
    {
        return Results.Ok(new { enabled = false });
    }
    var users = await LoadUsers(app.Environment);
    if (users.Count == 0 &&
        string.Equals(login.Username, "admin", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(login.Password, "admin", StringComparison.Ordinal))
    {
        var cookieValue = "admin|admin";
        context.Response.Cookies.Append("otd-user", cookieValue, new CookieOptions
        {
            HttpOnly = false,
            SameSite = SameSiteMode.Lax,
            Secure = false,
            IsEssential = true
        });
        return Results.Ok(new { enabled = true, user = "admin", role = "admin" });
    }
    var match = users.FirstOrDefault(user =>
        user.Enabled &&
        string.Equals(user.Name, login.Username, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(user.Password, login.Password, StringComparison.Ordinal));
    if (match == null)
    {
        return Results.Unauthorized();
    }
    var authCookieValue = $"{match.Name}|{match.Role}";
    context.Response.Cookies.Append("otd-user", authCookieValue, new CookieOptions
    {
        HttpOnly = false,
        SameSite = SameSiteMode.Lax,
        Secure = false,
        IsEssential = true
    });
    return Results.Ok(new { enabled = true, user = match.Name, role = match.Role });
});

// Auth Logout
app.MapPost("/auth/logout", (HttpContext context) =>
{
    context.Response.Cookies.Delete("otd-user");
    return Results.Ok(new { ok = true });
});

// Lokomotiven lesen
app.MapGet("/loko.xml", () =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "loko.xml");
    if (!File.Exists(path))
    {
        return Results.NotFound();
    }
    return Results.File(path, "application/xml");
});

// Lokomotiven speichern
app.MapPost("/loko/save", async (List<LokoDto> locos) =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "loko.xml");
    var doc = new XDocument(
        new XElement("locomotives",
            locos.Select(l => new XElement("locomotive",
                new XAttribute("id", string.IsNullOrWhiteSpace(l.Id) ? Guid.NewGuid().ToString() : l.Id),
                new XElement("name", l.Name ?? string.Empty),
                new XElement("address", l.address ?? string.Empty),
                new XElement("length", l.Length ?? string.Empty),
                new XElement("vmax", l.VMax ?? string.Empty),
                new XElement("vmin", l.VMin ?? string.Empty),
                new XElement("notes", l.Notes ?? string.Empty)
            ))
        )
    );
    await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
    await doc.SaveAsync(stream, SaveOptions.None, default);
    return Results.Ok(new { saved = locos.Count });
});

// Waggon lesen
app.MapGet("/waggon.xml", () =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "waggon.xml");
    if (!File.Exists(path))
    {
        return Results.NotFound();
    }
    return Results.File(path, "application/xml");
});

// Waggon speichern
app.MapPost("/waggon/save", async (List<WaggonDto> waggons) =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "waggon.xml");
    var doc = new XDocument(
        new XElement("waggons",
            waggons.Select(w => new XElement("waggon",
                new XAttribute("id", string.IsNullOrWhiteSpace(w.Id) ? Guid.NewGuid().ToString() : w.Id),
                new XElement("name", w.Name ?? string.Empty),
                new XElement("length", w.Length ?? string.Empty),
                new XElement("vmax", w.VMax ?? string.Empty),
                new XElement("notes", w.Notes ?? string.Empty)
            ))
        )
    );
    await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
    await doc.SaveAsync(stream, SaveOptions.None, default);
    return Results.Ok(new { saved = waggons.Count });
});

// Zug lesen
app.MapGet("/train.xml", () =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "train.xml");
    if (!File.Exists(path))
    {
        return Results.NotFound();
    }
    return Results.File(path, "application/xml");
});

// Zug speichern
app.MapPost("/train/save", async (TrainSaveDto train, HttpContext context) =>
{
    if (IsUsersEnabled(app.Environment) && !IsAdminUser(context))
    {
        return Results.Forbid();
    }
    var path = Path.Combine(app.Environment.ContentRootPath, "train.xml");
    var items = train.Items ?? new List<TrainItemDto>();
    double ParseNumber(string? text)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }
        return 0.0;
    }
    var totalLength = items.Sum(item =>
    {
        return ParseNumber(item.Length);
    });
    var minVmax = items
        .Select(item => ParseNumber(item.VMax))
        .Where(value => value > 0)
        .DefaultIfEmpty(0)
        .Min();
    var requestedVmax = ParseNumber(train.VMax);
    var finalVmax = minVmax;
    if (minVmax <= 0)
    {
        finalVmax = requestedVmax;
    }
    else if (requestedVmax > 0)
    {
        finalVmax = Math.Min(requestedVmax, minVmax);
    }
    var trainId = string.IsNullOrWhiteSpace(train.Id) ? Guid.NewGuid().ToString() : train.Id;
    var trainElement = new XElement("train",
        new XAttribute("id", trainId),
        new XElement("name", train.Name ?? string.Empty),
        new XElement("number", train.Number ?? string.Empty),
        new XElement("category", train.Category ?? string.Empty),
        new XElement("length", totalLength.ToString(CultureInfo.InvariantCulture)),
        new XElement("vmax", finalVmax.ToString(CultureInfo.InvariantCulture)),
        new XElement("cars",
            items.Select(item => new XElement("car",
                new XAttribute("id", string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString() : item.Id),
                new XAttribute("type", item.Type ?? string.Empty),
                new XAttribute("length", item.Length ?? string.Empty),
                new XAttribute("vmax", item.VMax ?? string.Empty),
                new XElement("name", item.Name ?? string.Empty)
            ))
        )
    );

    XDocument doc;
    XElement root;
    if (File.Exists(path))
    {
        doc = XDocument.Load(path);
        root = doc.Root ?? new XElement("trains");
        if (root.Name != "trains")
        {
            var existingTrain = root.Name == "train" ? root : null;
            root = new XElement("trains");
            if (existingTrain != null)
            {
                if (existingTrain.Attribute("id") == null)
                {
                    existingTrain.SetAttributeValue("id", Guid.NewGuid().ToString());
                }
                root.Add(existingTrain);
            }
            doc = new XDocument(root);
        }
    }
    else
    {
        root = new XElement("trains");
        doc = new XDocument(root);
    }

    var existing = root.Elements("train")
        .FirstOrDefault(e => string.Equals((string?)e.Attribute("id"), trainId, StringComparison.OrdinalIgnoreCase));
    if (existing != null)
    {
        existing.ReplaceWith(trainElement);
    }
    else
    {
        root.Add(trainElement);
    }
    await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
    await doc.SaveAsync(stream, SaveOptions.None, default);
    return Results.Ok(new { saved = items.Count, length = totalLength, vmax = finalVmax });
});

// Zug loeschen
app.MapPost("/train/delete", async (TrainDeleteDto request, HttpContext context) =>
{
    if (IsUsersEnabled(app.Environment) && !IsAdminUser(context))
    {
        return Results.Forbid();
    }
    var path = Path.Combine(app.Environment.ContentRootPath, "train.xml");
    if (!File.Exists(path))
    {
        return Results.NotFound();
    }
    var doc = XDocument.Load(path);
    var root = doc.Root;
    if (root == null)
    {
        return Results.NotFound();
    }
    var id = request.Id ?? string.Empty;
    bool removed = false;

    if (root.Name == "trains")
    {
        var target = root.Elements("train")
            .FirstOrDefault(e => string.Equals((string?)e.Attribute("id"), id, StringComparison.OrdinalIgnoreCase));
        if (target != null)
        {
            target.Remove();
            removed = true;
        }
    }
    else if (root.Name == "train")
    {
        var rootId = (string?)root.Attribute("id");
        if (string.IsNullOrWhiteSpace(id) || string.Equals(rootId, id, StringComparison.OrdinalIgnoreCase))
        {
            doc = new XDocument(new XElement("trains"));
            removed = true;
        }
    }

    if (!removed)
    {
        return Results.NotFound();
    }
    await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
    await doc.SaveAsync(stream, SaveOptions.None, default);
    return Results.Ok(new { deleted = id });
});

// System Aktionen
app.MapPost("/system/restart", (HttpContext context) =>
{
    if (IsUsersEnabled(app.Environment) && !IsAdminUser(context))
    {
        return Results.Forbid();
    }
    try
    {
        Process.Start(new ProcessStartInfo("shutdown", "/r /t 0") { CreateNoWindow = true, UseShellExecute = false });
    }
    catch
    {
        return Results.Problem("Neustart fehlgeschlagen.");
    }
    return Results.Ok(new { ok = true });
});

app.MapPost("/system/shutdown", (HttpContext context) =>
{
    if (IsUsersEnabled(app.Environment) && !IsAdminUser(context))
    {
        return Results.Forbid();
    }
    try
    {
        Process.Start(new ProcessStartInfo("shutdown", "/s /t 0") { CreateNoWindow = true, UseShellExecute = false });
    }
    catch
    {
        return Results.Problem("Herunterfahren fehlgeschlagen.");
    }
    return Results.Ok(new { ok = true });
});

app.MapPost("/system/exit", (HttpContext context) =>
{
    if (IsUsersEnabled(app.Environment) && !IsAdminUser(context))
    {
        return Results.Forbid();
    }
    Task.Run(() =>
    {
        app.Lifetime.StopApplication();
        System.Threading.Thread.Sleep(500);
        Environment.Exit(0);
    });
    return Results.Ok(new { ok = true });
});

// Zugdaten lesen
app.MapGet("/zd.xml", () =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "zd.xml");
    if (!File.Exists(path))
    {
        return Results.NotFound();
    }
    return Results.File(path, "application/xml");
});

// Zugdaten speichern
app.MapPost("/zd/save", async (ZdSaveDto payload) =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "zd.xml");
    XDocument doc;
    XElement root;
    if (File.Exists(path))
    {
        doc = XDocument.Load(path);
        root = doc.Root ?? new XElement("zugdaten");
        if (root.Name != "zugdaten")
        {
            root = new XElement("zugdaten");
            doc = new XDocument(root);
        }
    }
    else
    {
        root = new XElement("zugdaten");
        doc = new XDocument(root);
    }

    var id = string.IsNullOrWhiteSpace(payload.Id)
        ? $"{payload.Number}{(string.IsNullOrWhiteSpace(payload.Suffix) ? string.Empty : "-" + payload.Suffix)}"
        : payload.Id;
    var target = root.Elements("zug")
        .FirstOrDefault(e => string.Equals((string?)e.Attribute("id"), id, StringComparison.OrdinalIgnoreCase));

    var element = new XElement("zug",
        new XAttribute("id", id),
        new XAttribute("number", payload.Number ?? string.Empty),
        new XAttribute("suffix", payload.Suffix ?? string.Empty),
        new XAttribute("scope", payload.Scope ?? string.Empty),
        new XElement("route", payload.Route ?? string.Empty),
        new XElement("extra", payload.Extra ?? string.Empty),
        new XElement("diskri", payload.Diskri ?? string.Empty)
    );

    if (target != null)
    {
        target.ReplaceWith(element);
    }
    else
    {
        root.Add(element);
    }

    await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
    await doc.SaveAsync(stream, SaveOptions.None, default);
    return Results.Ok(new { saved = true, id });
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

public record LokoDto(string? Id, string? Name, string? address, string? Length, string? VMax, string? VMin, string? Notes);
public record WaggonDto(string? Id, string? Name, string? Length, string? VMax, string? Notes);
public record TrainItemDto(string? Id, string? Type, string? Name, string? Length, string? VMax);
public record TrainSaveDto(string? Id, string? Name, string? Number, string? Category, string? VMax, List<TrainItemDto>? Items);
public record TrainDeleteDto(string? Id);
public record PlanConfigFieldDto(string? Key, string? Value);
public record PlanSymbolDto(string? Id, string? Type, string? Classes, int X, int Y, List<PlanConfigFieldDto>? Config);
public record PlanSaveDto(int GridSize, List<PlanSymbolDto>? Symbols);
public record ZdSaveDto(string? Id, string? Number, string? Suffix, string? Scope, string? Route, string? Extra, string? Diskri);
public record UserDto(string? Name, string? Password, string? Role, bool Enabled);
public record AuthLoginDto(string? Username, string? Password);
