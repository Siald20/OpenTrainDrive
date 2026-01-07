using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;

namespace OpenTrainDrive.Controllers;

[Route("[controller]")]
public class PlanGenController : Controller
{
    private readonly IWebHostEnvironment _environment;

    public PlanGenController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return NotFound();
    }

    public sealed record SavePlanRequest(int GridSize, IReadOnlyList<PlacedSymbol> Symbols);

    public sealed record PlacedSymbol(
        string Id,
        string Type,
        string Classes,
        int X,
        int Y
    );

    [HttpPost("save")]
    public IActionResult Save([FromBody] SavePlanRequest request)
    {
        if (request.GridSize <= 0 || request.GridSize > 200)
        {
            return BadRequest("Invalid grid size.");
        }

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement("plan",
                new XAttribute("version", "1"),
                new XElement("grid",
                    new XAttribute("size", request.GridSize)
                ),
                new XElement("symbols",
                    (request.Symbols ?? Array.Empty<PlacedSymbol>())
                        .Take(5000)
                        .Select(symbol => new XElement("symbol",
                            new XAttribute("id", symbol.Id ?? string.Empty),
                            new XAttribute("type", symbol.Type ?? string.Empty),
                            new XAttribute("classes", symbol.Classes ?? string.Empty),
                            new XAttribute("x", symbol.X),
                            new XAttribute("y", symbol.Y)
                        ))
                )
            )
        );

        var xml = document.ToString(SaveOptions.None);

        var outputDir = _environment.ContentRootPath;
        Directory.CreateDirectory(outputDir);
        var outputPath = Path.Combine(outputDir, "plan.xml");
        System.IO.File.WriteAllText(outputPath, xml, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        return Ok(new { path = "/plan.xml" });
    }
}
