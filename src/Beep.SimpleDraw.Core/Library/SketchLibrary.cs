using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Beep.Skia;
using Beep.Skia.Components;
using Beep.Skia.Serialization;

namespace Beep.SimpleDraw.Core.Library;

/// <summary>Immutable library metadata and a strict native envelope containing the selected DiagramDto subset.</summary>
public sealed record LibraryEntry(Guid Id, string Name, string Document)
{
    public DiagramDto ToDiagram() => SimpleDrawDocument.Parse(Document);
}

public sealed class SketchLibrary
{
    public const int MaxBytes = 1024 * 1024;
    public const int MaxEntryBytes = 256 * 1024;
    public const int MaxEntries = 100;
    public const int MaxEntryShapes = 100;
    private readonly List<LibraryEntry> _entries = [];
    public IReadOnlyList<LibraryEntry> Entries => _entries.AsReadOnly();
    private sealed record Envelope(string Format, int Version, List<LibraryEntry> Entries);
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    { PropertyNameCaseInsensitive = false, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, WriteIndented = true, NewLine = "\n" };

    public LibraryEntry SaveSelection(DrawingManager manager, string name)
    {
        var selected = manager.SelectionManager.SelectedComponents.Select(c=>c.Id).ToHashSet();
        if (selected.Count is 0 or > MaxEntryShapes) throw new InvalidDataException("Select between 1 and 100 objects to save to the library.");
        var full = SimpleDrawDocument.Parse(SimpleDrawDocument.Serialize(manager));
        var diagram = SimpleDrawDocument.SelectionSubset(full, selected);
        if (diagram.Components.Count != selected.Count) throw new InvalidDataException("The selection contains an object outside this drawing.");
        foreach (var group in manager.GetComponents().Where(c=>c.GroupId!=Guid.Empty).GroupBy(c=>c.GroupId))
            if (group.Any(c=>selected.Contains(c.Id)) && group.Any(c=>!selected.Contains(c.Id)))
                throw new InvalidDataException("Select the complete group before saving it to the library.");
        var left = diagram.Components.Min(c=>c.X); var top = diagram.Components.Min(c=>c.Y);
        foreach (var c in diagram.Components) { c.X-=left; c.Y-=top; }
        var entry = new LibraryEntry(Guid.NewGuid(), name.Trim(), SimpleDrawDocument.Serialize(diagram));
        Add(entry); return entry;
    }

    public void Add(LibraryEntry entry, int? index = null)
    {
        var position = index ?? _entries.Count;
        if (position < 0 || position > _entries.Count) throw new ArgumentOutOfRangeException(nameof(index));
        var proposed = _entries.ToList(); proposed.Insert(position,entry); Serialize(proposed);
        _entries.Insert(position,entry);
    }
    public LibraryEntry Remove(Guid id)
    {
        var entry = _entries.SingleOrDefault(e=>e.Id==id) ?? throw new InvalidDataException("Library entry was not found.");
        _entries.Remove(entry); return entry;
    }
    public LibraryEntry Find(Guid id) => _entries.SingleOrDefault(e=>e.Id==id) ?? throw new InvalidDataException("Library entry was not found.");
    public void Import(string json)
    {
        var imported = Parse(json);
        var incoming = imported.Entries.Select(e=>e with {Id=Guid.NewGuid()}).ToArray();
        Serialize(_entries.Concat(incoming).ToList()); // Validate the complete merge before changing anything.
        _entries.AddRange(incoming);
    }
    public string Serialize() => Serialize(_entries);
    private static string Serialize(List<LibraryEntry> entries)
    {
        Validate(entries);
        var json = JsonSerializer.Serialize(new Envelope("beep.simpledraw.library", 1, entries),Options);
        if (Encoding.UTF8.GetByteCount(json)>MaxBytes) throw new InvalidDataException("Library exceeds the 1 MiB limit. Export entries in smaller libraries.");
        return json;
    }
    public static SketchLibrary Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || Encoding.UTF8.GetByteCount(json)>MaxBytes) throw new InvalidDataException("Choose a non-empty library file smaller than 1 MiB.");
        try
        {
            var envelope = JsonSerializer.Deserialize<Envelope>(json,Options);
            if (envelope is null || envelope.Format!="beep.simpledraw.library" || envelope.Version!=1)
                throw new InvalidDataException("Expected a SimpleDraw library version 1 file.");
            Validate(envelope.Entries);
            var result = new SketchLibrary(); result._entries.AddRange(envelope.Entries); return result;
        }
        catch (JsonException ex) { throw new InvalidDataException("Invalid SimpleDraw library file.",ex); }
    }
    private static void Validate(List<LibraryEntry>? entries)
    {
        if (entries is null || entries.Count>MaxEntries) throw new InvalidDataException("A library may contain at most 100 entries.");
        var ids = new HashSet<Guid>();
        foreach (var entry in entries)
        {
            if (entry is null || entry.Id==Guid.Empty || !ids.Add(entry.Id) || string.IsNullOrWhiteSpace(entry.Name)
                || entry.Name.Length>80 || entry.Name.Any(char.IsControl) || entry.Document is null
                || Encoding.UTF8.GetByteCount(entry.Document)>MaxEntryBytes)
                throw new InvalidDataException("Invalid library identity, name or entry size.");
            var diagram = entry.ToDiagram();
            if (diagram.Components.Count is 0 or >MaxEntryShapes) throw new InvalidDataException("A library entry needs between 1 and 100 objects.");
            if (diagram.Components.Min(c=>c.X)!=0 || diagram.Components.Min(c=>c.Y)!=0)
                throw new InvalidDataException("Library coordinates must be normalized to a zero origin.");
        }
    }
}
