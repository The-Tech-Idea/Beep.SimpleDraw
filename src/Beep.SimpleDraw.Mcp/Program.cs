using Beep.SimpleDraw.Mcp;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

var taskArgs = args.ToList();
string Option(string key, string fallback) { var i = taskArgs.IndexOf(key); if (i < 0) return fallback; if (i + 1 >= taskArgs.Count) throw new ArgumentException($"Missing {key} value."); var value = taskArgs[i + 1]; taskArgs.RemoveRange(i, 2); return value; }
var workspace = Option("--workspace", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Beep.SimpleDraw", "mcp"));
var portText = Option("--port", "5246");
var http = taskArgs.Remove("--http");
if (taskArgs.Count != 0) throw new ArgumentException("Use --workspace <directory>, optionally --http --port <port>.");
void Configure(IServiceCollection services, ILoggingBuilder logging) {
    logging.ClearProviders(); logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
    services.AddSingleton(new DrawingWorkspace(workspace));
}
if (!http) {
    var host = Host.CreateApplicationBuilder(Array.Empty<string>());
    Configure(host.Services,host.Logging);
    host.Services.AddMcpServer().WithStdioServerTransport().WithTools<SimpleDrawTools>().WithResources<SimpleDrawResources>();
    await host.Build().RunAsync();
} else {
    if (!int.TryParse(portText, out var port) || port is < 1024 or > 65535) throw new ArgumentException("Invalid local port.");
    var builder = WebApplication.CreateBuilder(Array.Empty<string>());
    Configure(builder.Services,builder.Logging);
    builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
    builder.Services.AddMcpServer().WithHttpTransport(o => o.Stateless = true).WithTools<SimpleDrawTools>().WithResources<SimpleDrawResources>();
    var app = builder.Build();
    app.Use(async (context,next) => {
        var allowedHosts=new[]{ $"127.0.0.1:{port}", $"localhost:{port}" };
        var origin=context.Request.Headers.Origin.ToString();
        if(!allowedHosts.Contains(context.Request.Host.Value,StringComparer.OrdinalIgnoreCase) ||
            origin.Length>0 && (!Uri.TryCreate(origin,UriKind.Absolute,out var uri) || uri.Scheme!="http" || !allowedHosts.Contains(uri.Authority,StringComparer.OrdinalIgnoreCase))) {
            context.Response.StatusCode=403; return;
        }
        try { await next(context); }
        catch (System.Text.Json.JsonException) when (!context.Response.HasStarted) {
            context.Response.StatusCode=400; await context.Response.WriteAsync("Invalid MCP JSON-RPC request.");
        }
    });
    app.MapMcp("/mcp"); await app.RunAsync();
}
public partial class Program { }
