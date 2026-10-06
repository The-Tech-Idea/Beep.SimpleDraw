using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Beep.SimpleDraw.Core;
using Beep.SimpleDraw.Core.Library;
using Beep.Skia;

if (args.Length != 1) throw new ArgumentException("Pass one absent output directory.");
var output = Path.GetFullPath(args[0]);
if (Directory.Exists(output) || File.Exists(output)) throw new IOException("Fixture output already exists.");
Directory.CreateDirectory(output);
var files = new List<object>();
WriteBoard("all-families", SkiaCatalog.Entries);
foreach (var family in SkiaCatalog.Entries.GroupBy(e => e.Family))
    WriteBoard(family.Key, family.ToArray());
File.WriteAllText(Path.Combine(output, "manifest.json"), JsonSerializer.Serialize(new
{
    format = "beep.simpledraw.catalog-fixtures", nativeVersion = SimpleDrawDocument.CurrentVersion,
    catalogCount = SkiaCatalog.Entries.Count,
    purpose = "Actual compiled factories at their default dimensions and state; tiled for browser import verification.",
    files
}, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
Console.WriteLine($"Generated {files.Count} native boards for {SkiaCatalog.Entries.Count} catalog types in {output}");

void WriteBoard(string name, IEnumerable<CatalogEntry> entries)
{
    var session = new DrawingSession();
    session.ResizeViewport(1000, 700);
    var manager = session.Manager;
    float x = 40, y = 40, rowHeight = 0;
    var types = new List<string>();
    try
    {
        foreach (var entry in entries)
        {
            session.InsertTemplate(entry.TypeKey);
            var node = manager.GetComponents().Last();
            if (x > 40 && x + node.Width > 2400) { x = 40; y += rowHeight + 80; rowHeight = 0; }
            node.X = x; node.Y = y; node.Name = entry.Label;
            x += node.Width + 80; rowHeight = Math.Max(rowHeight, node.Height);
            types.Add(entry.TypeKey);
        }
        var json = SimpleDrawDocument.Serialize(manager);
        var restored = new DrawingManager();
        try
        {
            SimpleDrawDocument.Apply(restored, SimpleDrawDocument.Parse(json));
            if (SimpleDrawDocument.Serialize(restored) != json)
                throw new InvalidDataException($"Native roundtrip changed {name}.");
        }
        finally { foreach (var node in restored.GetComponents()) node.Dispose(); }
        var bytes = Encoding.UTF8.GetBytes(json);
        var file = name + ".simpledraw.json";
        File.WriteAllBytes(Path.Combine(output, file), bytes);
        files.Add(new { file, count = types.Count, bytes = bytes.Length,
            sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), types });
    }
    finally { foreach (var node in manager.GetComponents()) node.Dispose(); }
}
