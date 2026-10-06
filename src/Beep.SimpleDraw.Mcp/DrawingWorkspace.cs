using System.Text.Json;
using Beep.SimpleDraw.Core;
using Beep.SimpleDraw.Core.Library;

namespace Beep.SimpleDraw.Mcp;

public sealed record BoardFile(Guid Id, string Name, long Revision, string Document, string Library);
public sealed class BoardState : IDisposable
{
    public BoardFile File { get; set; }
    public DrawingSession Session { get; } = new();
    public SketchLibrary Library { get; set; }
    public BoardState(BoardFile file) { File = file; Session.OpenDocument(file.Document); Library = SketchLibrary.Parse(file.Library); }
    public void Dispose() { Session.NewDocument(); }
}

/// <summary>One native drawing format, persisted with atomic replacement and a cross-process workspace lock.</summary>
public sealed class DrawingWorkspace : IDisposable
{
    private readonly Dictionary<Guid, BoardState> _cache = [];
    private readonly object _gate = new();
    public string Root { get; }
    public string Boards { get; }
    public string Artifacts { get; }
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public DrawingWorkspace(string root)
    {
        Root = Path.GetFullPath(root); Directory.CreateDirectory(Root); RejectLink(Root);
        Boards = Path.Combine(Root,"boards"); Artifacts = Path.Combine(Root,"artifacts");
        Directory.CreateDirectory(Boards); Directory.CreateDirectory(Artifacts); RejectLink(Boards); RejectLink(Artifacts);
    }
    private static void RejectLink(string path) { if (File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint)) throw new InvalidDataException("Workspace entries must not be symbolic links."); }
    private T Locked<T>(Func<T> operation)
    {
        lock (_gate) {
            var path = Path.Combine(Root,"workspace.lock");
            if (File.Exists(path)) RejectLink(path);
            FileStream? lease = null;
            for (int i = 0; i < 100 && lease is null; i++) {
                try { lease = new FileStream(path,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None); }
                catch (IOException) { if (i == 99) throw new InvalidDataException("Workspace is busy; retry the operation."); Thread.Sleep(20); }
            }
            using (lease) return operation();
        }
    }
    private string BoardPath(Guid id) => Path.Combine(Boards,id.ToString("D")+".json");
    private BoardState Load(Guid id)
    {
        var path = BoardPath(id); if (!File.Exists(path)) throw new InvalidDataException("Drawing not found. Use list_drawings."); RejectLink(path);
        if (new FileInfo(path).Length > 4 * 1024 * 1024) throw new InvalidDataException("Invalid persisted drawing size.");
        var file = JsonSerializer.Deserialize<BoardFile>(File.ReadAllText(path),Json) ?? throw new InvalidDataException("Invalid persisted drawing.");
        if (file.Id != id || file.Revision < 1) throw new InvalidDataException("Invalid persisted drawing identity.");
        if (_cache.TryGetValue(id,out var state) && state.File.Revision == file.Revision) return state;
        var next = new BoardState(file);
        if (_cache.Remove(id,out state)) state.Dispose();
        _cache.Add(id,next); return next;
    }
    private static void Name(string name) { if (string.IsNullOrWhiteSpace(name) || name.Length > 80 || name.Any(char.IsControl)) throw new InvalidDataException("Drawing name must contain 1 through 80 printable characters."); }
    private static void Atomic(string path, string text)
    {
        if (File.Exists(path)) RejectLink(path);
        var temp = path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try { File.WriteAllText(temp,text); File.Move(temp,path,true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    public BoardFile Create(string name, string? document = null) => Locked(() => {
        Name(name); if (Directory.EnumerateFiles(Boards,"*.json").Count() >= 256) throw new InvalidDataException("Workspace drawing limit is 256.");
        var session = new DrawingSession(); if (document is not null) session.OpenDocument(document);
        var file = new BoardFile(Guid.NewGuid(),name.Trim(),1,SimpleDrawDocument.Serialize(session.Manager),new SketchLibrary().Serialize());
        session.NewDocument(); Atomic(BoardPath(file.Id),JsonSerializer.Serialize(file,Json)); return file;
    });
    public object[] List() => Locked(() => Directory.EnumerateFiles(Boards,"*.json").Select(path => {
        if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(path),"D",out var id)) throw new InvalidDataException("Unexpected workspace board entry.");
        var state = Load(id); return (object)new {state.File.Id,state.File.Name,state.File.Revision,objects=state.Session.Manager.GetComponents().Count,connections=state.Session.Manager.GetLines().Count};
    }).ToArray());
    public T Read<T>(Guid id, Func<BoardState,T> operation) => Locked(() => operation(Load(id)));
    public object Change(Guid id, long expectedRevision, Action<BoardState> operation) => Locked(() => {
        var state = Load(id);
        if (state.File.Revision != expectedRevision) throw new InvalidDataException($"Revision conflict: expected {expectedRevision}, current {state.File.Revision}. Read the drawing and retry.");
        var before = SimpleDrawDocument.Serialize(state.Session.Manager); var beforeLibrary = state.Library.Serialize();
        var selected = state.Session.SelectedComponents.Select(c=>c.Id).ToArray();
        try {
            operation(state);
            var document = SimpleDrawDocument.Serialize(state.Session.Manager); var library = state.Library.Serialize();
            var next = state.File with {Revision=checked(state.File.Revision+1),Document=document,Library=library};
            Atomic(BoardPath(id),JsonSerializer.Serialize(next,Json)); state.File = next;
        } catch {
            // Validate before mutating in each operation. If a late validation/write fails, restore the scene explicitly.
            if (SimpleDrawDocument.Serialize(state.Session.Manager) != before) state.Session.OpenDocument(before);
            if (state.Library.Serialize()!=beforeLibrary) state.Library=SketchLibrary.Parse(beforeLibrary);
            state.Session.Manager.SelectionManager.ClearSelection();
            foreach (var c in state.Session.Manager.GetComponents().Where(c=>selected.Contains(c.Id))) if(!c.IsSelected) state.Session.SelectShape(c,true);
            throw;
        }
        return Snapshot(state);
    });
    public static object Snapshot(BoardState state) => new {
        state.File.Id,state.File.Name,state.File.Revision,
        diagram=SimpleDrawDocument.Parse(SimpleDrawDocument.Serialize(state.Session.Manager)),
        selection=state.Session.SelectedComponents.Select(c=>c.Id).ToArray(),
        canUndo=state.Session.Manager.CanUndo,canRedo=state.Session.Manager.CanRedo,
        viewport=new {width=state.Session.ViewportSize.Width,height=state.Session.ViewportSize.Height,zoom=state.Session.Manager.Zoom,panX=state.Session.Manager.PanOffset.X,panY=state.Session.Manager.PanOffset.Y},
        library=state.Library.Entries.Select(e=>new {e.Id,e.Name}).ToArray()
    };
    public string WriteArtifact(Guid id, long revision, string extension, byte[] bytes)
    {
        if (extension is not ("simpledraw.json" or "png" or "svg" or "library.json")) throw new InvalidDataException("Unsupported export.");
        var hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant()[..16];
        var path = Path.Combine(Artifacts,$"{id:D}-r{revision}-{hash}.{extension}");
        if (File.Exists(path)) RejectLink(path);
        var temp = path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try { File.WriteAllBytes(temp,bytes); File.Move(temp,path,true); } finally { if(File.Exists(temp)) File.Delete(temp); }
        return path;
    }
    public void Dispose() { lock(_gate) { foreach(var state in _cache.Values) state.Dispose(); _cache.Clear(); } }
}
