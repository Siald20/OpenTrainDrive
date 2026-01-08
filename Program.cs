using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;
using System.Xml.Linq;
using System.Globalization;

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
        Path.Combine(app.Environment.ContentRootPath, "OTD", "Symbols")),
    RequestPath = "/symbols"
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
app.MapPost("/plan/save", async (PlanSaveDto plan) =>
{
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
                new XElement("adress", l.Adress ?? string.Empty),
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
app.MapPost("/train/save", async (TrainSaveDto train) =>
{
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
app.MapPost("/train/delete", async (TrainDeleteDto request) =>
{
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

public record LokoDto(string? Id, string? Name, string? Adress, string? Length, string? VMax, string? VMin, string? Notes);
public record WaggonDto(string? Id, string? Name, string? Length, string? VMax, string? Notes);
public record TrainItemDto(string? Id, string? Type, string? Name, string? Length, string? VMax);
public record TrainSaveDto(string? Id, string? Name, string? VMax, List<TrainItemDto>? Items);
public record TrainDeleteDto(string? Id);
public record PlanConfigFieldDto(string? Key, string? Value);
public record PlanSymbolDto(string? Id, string? Type, string? Classes, int X, int Y, List<PlanConfigFieldDto>? Config);
public record PlanSaveDto(int GridSize, List<PlanSymbolDto>? Symbols);
