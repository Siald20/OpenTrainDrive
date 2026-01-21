using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;
using System.Text;
using System.Xml.Linq;
using System.Globalization;
using OpenTrainDrive.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();
Action<StaticFileResponseContext> applyNoCache = ctx =>
{
    if (!app.Environment.IsDevelopment())
    {
        return;
    }
    ctx.Context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
    ctx.Context.Response.Headers["Pragma"] = "no-cache";
    ctx.Context.Response.Headers["Expires"] = "0";
};

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = applyNoCache
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "Elements")),
    RequestPath = "/elements",
    OnPrepareResponse = applyNoCache
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "Loco")),
    RequestPath = "/loco",
    OnPrepareResponse = applyNoCache
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "drive")),
    RequestPath = "/drive",
    OnPrepareResponse = applyNoCache
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "railcar")),
    RequestPath = "/railcar",
    OnPrepareResponse = applyNoCache
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "Train")),
    RequestPath = "/train",
    OnPrepareResponse = applyNoCache
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "timetable")),
    RequestPath = "/timetable",
    OnPrepareResponse = applyNoCache
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "timetable-viewer")),
    RequestPath = "/timetable-viewer",
    OnPrepareResponse = applyNoCache
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "wegzeit")),
    RequestPath = "/wegzeit",
    OnPrepareResponse = applyNoCache
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "station")),
    RequestPath = "/station",
    OnPrepareResponse = applyNoCache
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "signal")),
    RequestPath = "/signal",
    OnPrepareResponse = applyNoCache
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "ZD")),
    RequestPath = "/zd",
    OnPrepareResponse = applyNoCache
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "auth")),
    RequestPath = "/auth",
    OnPrepareResponse = applyNoCache
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "exit")),
    RequestPath = "/exit",
    OnPrepareResponse = applyNoCache
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "OTD", "Symbols")),
    RequestPath = "/symbols",
    OnPrepareResponse = applyNoCache
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "SVG")),
    RequestPath = "/svg",
    OnPrepareResponse = applyNoCache
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

app.MapGet("/iltis.css", () =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "OTD", "iltis.css");
    if (!File.Exists(path))
    {
        return Results.NotFound();
    }
    return Results.File(path, "text/css");
});

// Elemente lesen
app.MapGet("/elements.xml", () =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "elements.xml");
    if (!File.Exists(path))
    {
        return Results.NotFound();
    }
    return Results.File(path, "application/xml");
});

// Elemente speichern
app.MapPost("/elements/save", async (List<SignalElementDto> elements, HttpContext context) =>
{
    if (IsUsersEnabled(app.Environment) && !IsAdminUser(context))
    {
        return Results.Forbid();
    }
    var path = Path.Combine(app.Environment.ContentRootPath, "elements.xml");
    var doc = new XDocument(
        new XElement("elements",
            (elements ?? new List<SignalElementDto>()).Select(element => new XElement("signal",
                new XAttribute("id", string.IsNullOrWhiteSpace(element.Id) ? Guid.NewGuid().ToString() : element.Id),
                new XAttribute("address", element.Address ?? string.Empty),
                new XAttribute("aspects", element.Aspects ?? string.Empty),
                new XAttribute("asb", element.Asb ?? string.Empty),
                new XAttribute("notes", element.Notes ?? string.Empty)
            ))
        )
    );
    await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
    await doc.SaveAsync(stream, SaveOptions.None, default);
    return Results.Ok(new { saved = elements?.Count ?? 0 });
});

// SVG Symbole auflisten
app.MapGet("/svg/list", () =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "SVG");
    if (!Directory.Exists(path))
    {
        return Results.Ok(Array.Empty<string>());
    }
    var files = Directory.GetFiles(path, "*.svg");
    var names = files.Select(Path.GetFileName).Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
    return Results.Ok(names);
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

app.MapGet("/switchingdevices.xml", () =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "switchingdevices.xml");
    if (!File.Exists(path))
    {
        return Results.NotFound();
    }
    return Results.File(path, "application/xml");
});

app.MapGet("/signal.xml", () =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "signal.xml");
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

app.MapPost("/switchingdevices/save", async (XmlSaveDto payload, HttpContext context) =>
{
    if (IsUsersEnabled(app.Environment) && !IsAdminUser(context))
    {
        return Results.Forbid();
    }
    if (string.IsNullOrWhiteSpace(payload.Xml))
    {
        return Results.BadRequest("Empty XML");
    }
    var xml = payload.Xml.Trim();
    if (!xml.StartsWith("<?xml", StringComparison.Ordinal))
    {
        xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + xml;
    }
    try
    {
        _ = XDocument.Parse(xml);
    }
    catch
    {
        return Results.BadRequest("Invalid XML");
    }
    var path = Path.Combine(app.Environment.ContentRootPath, "switchingdevices.xml");
    await File.WriteAllTextAsync(path, xml, new UTF8Encoding(false));
    return Results.Ok(new { saved = true });
});

app.MapPost("/signal/save", async (XmlSaveDto payload, HttpContext context) =>
{
    if (IsUsersEnabled(app.Environment) && !IsAdminUser(context))
    {
        return Results.Forbid();
    }
    if (string.IsNullOrWhiteSpace(payload.Xml))
    {
        return Results.BadRequest("Empty XML");
    }
    var xml = payload.Xml.Trim();
    if (!xml.StartsWith("<?xml", StringComparison.Ordinal))
    {
        xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + xml;
    }
    try
    {
        _ = XDocument.Parse(xml);
    }
    catch
    {
        return Results.BadRequest("Invalid XML");
    }
    var path = Path.Combine(app.Environment.ContentRootPath, "signal.xml");
    await File.WriteAllTextAsync(path, xml, new UTF8Encoding(false));
    return Results.Ok(new { saved = true });
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
app.MapGet("/loco.xml", () =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "loco.xml");
    if (!File.Exists(path))
    {
        return Results.NotFound();
    }
    return Results.File(path, "application/xml");
});

// Lokomotiven speichern
app.MapPost("/loco/save", async (List<LocoDto> locos) =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "loco.xml");
    var doc = new XDocument(
        new XDeclaration("1.0", "utf-8", null),
        new XComment(" Definition of Locomotives "),
        new XElement("locos",
            (locos ?? new List<LocoDto>()).Select(l =>
            {
                var uid = string.IsNullOrWhiteSpace(l.Uid) ? Guid.NewGuid().ToString() : l.Uid;
                var loco = new XElement("loco",
                    new XAttribute("uid", uid),
                    new XAttribute("name", l.Name ?? string.Empty),
                    new XAttribute("length", l.Length ?? string.Empty),
                    new XAttribute("axles", l.Axles ?? string.Empty),
                    new XAttribute("index", l.Index ?? string.Empty)
                );

                var model = l.Model ?? new LocoModelDto(null, null, null, null, null, null, null, null, null, null, null, null);
                loco.Add(new XElement("model",
                    new XAttribute("manufacturer", model.Manufacturer ?? string.Empty),
                    new XAttribute("scale", model.Scale ?? string.Empty),
                    new XAttribute("catalognumber", model.CatalogNumber ?? string.Empty),
                    new XElement("description", model.Description ?? string.Empty),
                    new XElement("operator", model.Operator ?? string.Empty),
                    new XElement("class", model.ClassName ?? string.Empty),
                    new XElement("fleetnumber", model.FleetNumber ?? string.Empty),
                    new XElement("tractiontype", model.TractionType ?? string.Empty),
                    new XElement("weight", model.Weight ?? string.Empty),
                    new XElement("vmax", model.VMax ?? string.Empty),
                    new XElement("icon", model.Icon ?? string.Empty),
                    new XElement("notes", model.Notes ?? string.Empty)
                ));

                var decoder = l.Decoder ?? new LocoDecoderDto(null, null, null, null, null);
                loco.Add(new XElement("decoder",
                    new XElement("protocol", decoder.Protocol ?? string.Empty),
                    new XElement("address", decoder.Address ?? string.Empty),
                    new XElement("addresstype", decoder.AddressType ?? string.Empty),
                    new XElement("functiontable",
                        (decoder.Functions ?? new List<LocoFunctionDto>()).Select(fn =>
                            new XElement("function",
                                new XAttribute("no", fn.No ?? string.Empty),
                                new XAttribute("description", fn.Description ?? string.Empty),
                                new XAttribute("actuation", fn.Actuation ?? string.Empty),
                                new XAttribute("type", fn.Type ?? string.Empty),
                                new XAttribute("category", fn.Category ?? string.Empty),
                                new XAttribute("visible", fn.Visible ?? string.Empty),
                                new XAttribute("image", fn.Image ?? string.Empty)
                            ))
                    ),
                    new XElement("speedtable",
                        (decoder.Speeds ?? new List<LocoSpeedDto>()).Select(speed =>
                            new XElement("speed",
                                new XAttribute("step", speed.Step ?? string.Empty),
                                new XAttribute("v", speed.V ?? string.Empty)
                            ))
                    )
                ));

                var operation = l.Operation ?? new LocoOperationDto(null, null, null, null, null, null, null);
                loco.Add(new XElement("operation",
                    new XElement("purchasedate", operation.PurchaseDate ?? string.Empty),
                    new XElement("operatingtime_total", operation.OperatingTimeTotal ?? string.Empty),
                    new XElement("operatingtime_service", operation.OperatingTimeService ?? string.Empty),
                    new XElement("traveldistance", operation.TravelDistance ?? string.Empty),
                    new XElement("serviceinterval", operation.ServiceInterval ?? string.Empty),
                    new XElement("refuleinterval", operation.RefuelInterval ?? string.Empty),
                    new XElement("servicetable",
                        (operation.ServiceIssues ?? new List<LocoServiceIssueDto>()).Select(issue =>
                            new XElement("issue",
                                new XAttribute("date", issue.Date ?? string.Empty),
                                new XAttribute("type", issue.Type ?? string.Empty),
                                (issue.Items ?? new List<string>()).Select(item => new XElement("item", item ?? string.Empty))
                            ))
                    )
                ));

                return loco;
            })
        )
    );
    await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
    await doc.SaveAsync(stream, SaveOptions.None, default);
    return Results.Ok(new { saved = locos?.Count ?? 0 });
});

// Waggon lesen
app.MapGet("/railcar.xml", () =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "railcar.xml");
    if (!File.Exists(path))
    {
        return Results.NotFound();
    }
    return Results.File(path, "application/xml");
});

// Waggon speichern
app.MapPost("/railcar/save", async (List<RailcarDto> railcars) =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "railcar.xml");
    var doc = new XDocument(
        new XDeclaration("1.0", "utf-8", null),
        new XComment(" Definition of Rail Cars (Coaches, Waggons etc.) "),
        new XElement("railcars",
            (railcars ?? new List<RailcarDto>()).Select(w =>
            {
                var uid = string.IsNullOrWhiteSpace(w.Uid) ? Guid.NewGuid().ToString() : w.Uid;
                var railcar = new XElement("railcar",
                    new XAttribute("uid", uid),
                    new XAttribute("name", w.Name ?? string.Empty),
                    new XAttribute("length", w.Length ?? string.Empty),
                    new XAttribute("axles", w.Axles ?? string.Empty),
                    new XAttribute("index", w.Index ?? string.Empty)
                );

                var model = w.Model ?? new RailcarModelDto(null, null, null, null, null, null, null, null, null, null, null, null, null);
                railcar.Add(new XElement("model",
                    new XAttribute("manufacturer", model.Manufacturer ?? string.Empty),
                    new XAttribute("scale", model.Scale ?? string.Empty),
                    new XAttribute("catalognumber", model.CatalogNumber ?? string.Empty),
                    new XElement("description", model.Description ?? string.Empty),
                    new XElement("operator", model.Operator ?? string.Empty),
                    new XElement("class", model.ClassName ?? string.Empty),
                    new XElement("fleetnumber", model.FleetNumber ?? string.Empty),
                    new XElement("cartype", model.CarType ?? string.Empty),
                    new XElement("weight_empty", model.WeightEmpty ?? string.Empty),
                    new XElement("weight_full", model.WeightFull ?? string.Empty),
                    new XElement("vmax", model.VMax ?? string.Empty),
                    new XElement("image", model.Image ?? string.Empty),
                    new XElement("notes", model.Notes ?? string.Empty)
                ));

                var decoder = w.Decoder ?? new RailcarDecoderDto(null, null, null, null);
                railcar.Add(new XElement("decoder",
                    new XElement("protocol", decoder.Protocol ?? string.Empty),
                    new XElement("address", decoder.Address ?? string.Empty),
                    new XElement("addresstype", decoder.AddressType ?? string.Empty),
                    new XElement("functiontable",
                        (decoder.Functions ?? new List<RailcarFunctionDto>()).Select(fn =>
                            new XElement("function",
                                new XAttribute("no", fn.No ?? string.Empty),
                                new XAttribute("description", fn.Description ?? string.Empty),
                                new XAttribute("actuation", fn.Actuation ?? string.Empty),
                                new XAttribute("type", fn.Type ?? string.Empty),
                                new XAttribute("category", fn.Category ?? string.Empty),
                                new XAttribute("visible", fn.Visible ?? string.Empty),
                                new XAttribute("image", fn.Image ?? string.Empty)
                            ))
                    )
                ));

                var operation = w.Operation ?? new RailcarOperationDto(null, null, null, null, null);
                railcar.Add(new XElement("operation",
                    new XElement("purchasedate", operation.PurchaseDate ?? string.Empty),
                    new XElement("operatingtime", operation.OperatingTime ?? string.Empty),
                    new XElement("traveldistance", operation.TravelDistance ?? string.Empty),
                    new XElement("serviceinterval", operation.ServiceInterval ?? string.Empty),
                    new XElement("servicetable",
                        (operation.ServiceIssues ?? new List<RailcarServiceIssueDto>()).Select(issue =>
                            new XElement("issue",
                                new XAttribute("date", issue.Date ?? string.Empty),
                                new XAttribute("type", issue.Type ?? string.Empty),
                                (issue.Items ?? new List<string>()).Select(item => new XElement("item", item ?? string.Empty))
                            ))
                    )
                ));

                return railcar;
            })
        )
    );
    await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
    await doc.SaveAsync(stream, SaveOptions.None, default);
    return Results.Ok(new { saved = railcars?.Count ?? 0 });
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
app.MapPost("/train/save", async (TrainDto train, HttpContext context) =>
{
    if (IsUsersEnabled(app.Environment) && !IsAdminUser(context))
    {
        return Results.Forbid();
    }
    var path = Path.Combine(app.Environment.ContentRootPath, "train.xml");
    var items = train.Items ?? new List<TrainVehicleDto>();
    double ParseNumber(string? text)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }
        return 0.0;
    }

    var totalLength = items.Sum(item => ParseNumber(item.Length));
    var totalAxles = items.Sum(item => ParseNumber(item.Axles));
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

    var trainId = string.IsNullOrWhiteSpace(train.Uid) ? Guid.NewGuid().ToString() : train.Uid;
    var trainElement = new XElement("train",
        new XAttribute("uid", trainId),
        new XAttribute("name", train.Name ?? string.Empty),
        new XAttribute("length", totalLength.ToString(CultureInfo.InvariantCulture)),
        new XAttribute("axles", totalAxles.ToString(CultureInfo.InvariantCulture)),
        new XAttribute("index", train.Index ?? string.Empty),
        new XElement("description", train.Description ?? string.Empty),
        new XElement("weight", train.Weight ?? string.Empty),
        new XElement("vmax", finalVmax.ToString(CultureInfo.InvariantCulture)),
        new XElement("image", train.Image ?? string.Empty),
        new XElement("notes", train.Notes ?? string.Empty),
        new XElement("vehicles",
            items.Select((item, idx) =>
                new XElement(item.Type == "loco" ? "loco" : "railcar",
                    new XAttribute("f_uid", item.FUid ?? string.Empty),
                    new XAttribute("position", (item.Position > 0 ? item.Position : idx + 1).ToString(CultureInfo.InvariantCulture))
                ))
        )
    );

    XDocument doc;
    XElement root;
    if (File.Exists(path))
    {
        try
        {
            doc = XDocument.Load(path);
            root = doc.Root ?? new XElement("trains");
            if (root.Name != "trains")
            {
                root = new XElement("trains");
                doc = new XDocument(root);
            }
        }
        catch
        {
            root = new XElement("trains");
            doc = new XDocument(root);
        }
    }
    else
    {
        root = new XElement("trains");
        doc = new XDocument(root);
    }

    var existing = root.Elements("train")
        .FirstOrDefault(e => string.Equals((string?)e.Attribute("uid"), trainId, StringComparison.OrdinalIgnoreCase));
    if (existing != null)
    {
        existing.ReplaceWith(trainElement);
    }
    else
    {
        root.Add(trainElement);
    }
    doc.Declaration = new XDeclaration("1.0", "utf-8", null);
    if (doc.Nodes().All(node => node.NodeType != System.Xml.XmlNodeType.Comment))
    {
        doc.AddFirst(new XComment(" Definition of Trains "));
    }
    await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
    await doc.SaveAsync(stream, SaveOptions.None, default);
    return Results.Ok(new { saved = items.Count, length = totalLength, vmax = finalVmax, axles = totalAxles });
});

// Bahnhoefe lesen
app.MapGet("/station.xml", () =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "station.xml");
    if (!File.Exists(path))
    {
        return Results.NotFound();
    }
    return Results.File(path, "application/xml");
});

// Bahnhoefe speichern
app.MapPost("/station/save", async (List<StationDto> stations, HttpContext context) =>
{
    if (IsUsersEnabled(app.Environment) && !IsAdminUser(context))
    {
        return Results.Forbid();
    }
    var path = Path.Combine(app.Environment.ContentRootPath, "station.xml");
    var doc = new XDocument(
        new XElement("stations",
            (stations ?? new List<StationDto>()).Select(station => new XElement("station",
                new XAttribute("id", string.IsNullOrWhiteSpace(station.Id) ? Guid.NewGuid().ToString() : station.Id),
                new XAttribute("name", station.Name ?? string.Empty),
                new XAttribute("code", station.Code ?? string.Empty),
                new XAttribute("abbr", station.Abbr ?? string.Empty),
                new XAttribute("type", station.Type ?? string.Empty),
                new XAttribute("region", station.Region ?? string.Empty),
                new XAttribute("notes", station.Notes ?? string.Empty)
            ))
        )
    );
    await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
    await doc.SaveAsync(stream, SaveOptions.None, default);
    return Results.Ok(new { saved = stations?.Count ?? 0 });
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
    XDocument doc;
    try
    {
        doc = XDocument.Load(path);
    }
    catch
    {
        return Results.Problem("train.xml ist ungueltig.");
    }
    var root = doc.Root;
    if (root == null)
    {
        return Results.NotFound();
    }
    var id = request.Id ?? request.Uid ?? string.Empty;
    bool removed = false;

    if (root.Name == "trains")
    {
        var target = root.Elements("train")
            .FirstOrDefault(e => string.Equals((string?)e.Attribute("uid"), id, StringComparison.OrdinalIgnoreCase));
        if (target != null)
        {
            target.Remove();
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

public record LocoFunctionDto(string? No, string? Description, string? Actuation, string? Type, string? Category, string? Visible, string? Image);
public record LocoSpeedDto(string? Step, string? V);
public record LocoModelDto(string? Manufacturer, string? Scale, string? CatalogNumber, string? Description, string? Operator, string? ClassName, string? FleetNumber, string? TractionType, string? Weight, string? VMax, string? Icon, string? Notes);
public record LocoDecoderDto(string? Protocol, string? Address, string? AddressType, List<LocoFunctionDto>? Functions, List<LocoSpeedDto>? Speeds);
public record LocoServiceIssueDto(string? Date, string? Type, List<string>? Items);
public record LocoOperationDto(string? PurchaseDate, string? OperatingTimeTotal, string? OperatingTimeService, string? TravelDistance, string? ServiceInterval, string? RefuelInterval, List<LocoServiceIssueDto>? ServiceIssues);
public record LocoDto(string? Uid, string? Name, string? Length, string? Axles, string? Index, LocoModelDto? Model, LocoDecoderDto? Decoder, LocoOperationDto? Operation);
public record RailcarFunctionDto(string? No, string? Description, string? Actuation, string? Type, string? Category, string? Visible, string? Image);
public record RailcarModelDto(string? Manufacturer, string? Scale, string? CatalogNumber, string? Description, string? Operator, string? ClassName, string? FleetNumber, string? CarType, string? WeightEmpty, string? WeightFull, string? VMax, string? Image, string? Notes);
public record RailcarDecoderDto(string? Protocol, string? Address, string? AddressType, List<RailcarFunctionDto>? Functions);
public record RailcarServiceIssueDto(string? Date, string? Type, List<string>? Items);
public record RailcarOperationDto(string? PurchaseDate, string? OperatingTime, string? TravelDistance, string? ServiceInterval, List<RailcarServiceIssueDto>? ServiceIssues);
public record RailcarDto(string? Uid, string? Name, string? Length, string? Axles, string? Index, RailcarModelDto? Model, RailcarDecoderDto? Decoder, RailcarOperationDto? Operation);
public record TrainVehicleDto(string? Type, string? FUid, int Position, string? Length, string? Axles, string? VMax);
public record TrainDto(string? Uid, string? Name, string? Index, string? Image, string? Description, string? Weight, string? VMax, string? Notes, List<TrainVehicleDto>? Items);
public record TrainDeleteDto(string? Id, string? Uid);
public record StationDto(string? Id, string? Name, string? Code, string? Abbr, string? Type, string? Region, string? Notes);
public record SignalElementDto(string? Id, string? Address, string? Aspects, string? Asb, string? Notes);
public record PlanConfigFieldDto(string? Key, string? Value);
public record PlanSymbolDto(string? Id, string? Type, string? Classes, int X, int Y, List<PlanConfigFieldDto>? Config);
public record PlanSaveDto(int GridSize, List<PlanSymbolDto>? Symbols);
public record XmlSaveDto(string? Xml);
public record ZdSaveDto(string? Id, string? Number, string? Suffix, string? Scope, string? Route, string? Extra, string? Diskri);
public record UserDto(string? Name, string? Password, string? Role, bool Enabled);
public record AuthLoginDto(string? Username, string? Password);
