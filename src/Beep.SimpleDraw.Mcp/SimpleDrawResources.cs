using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Beep.SimpleDraw.Core;

namespace Beep.SimpleDraw.Mcp;

[McpServerResourceType]
public sealed class SimpleDrawResources(DrawingWorkspace workspace)
{
    [McpServerResource(UriTemplate="simpledraw://guide",Name="SimpleDraw agent guide",MimeType="text/plain"),Description("Workflow and contracts for Claude and Codex drawing tools.")]
    public string Guide() => "Use list_components then describe_component to discover exact Beep.Skia types and persisted property schemas. create_drawing returns id/revision. Every mutation requires expectedRevision; always use the revision returned by the previous mutation. get_drawing returns the full native graph and stable ports. Use add_component/add_sketch, property/style/path/text tools, connectors, selections and reusable libraries. render_drawing gives a real inline Beep.Skia PNG; export_drawing saves native JSON/PNG/SVG/library files. Open native .simpledraw.json files with SimpleDraw's Open control. Runtime-only CLR properties are listed but not editable native document data. Existing browser tabs are not remotely mutated. Undo history is local to the server process and resets on restart or an external write. Persisted drawing data is shared across clients in the configured workspace. HTTP mode is loopback only.";
    [McpServerResource(UriTemplate="simpledraw://drawings/{drawingId}",Name="Native SimpleDraw document",MimeType="application/json")]
    public string Drawing(string drawingId) => workspace.Read(Guid.Parse(drawingId),b=>SimpleDrawDocument.Serialize(b.Session.Manager));
    [McpServerResource(UriTemplate="simpledraw://components/{type}",Name="Beep.Skia component schema",MimeType="application/json")]
    public string Component(string type) => JsonSerializer.Serialize(CatalogDescription.Describe(type),DrawingWorkspace.Json);
}
