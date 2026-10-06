namespace Beep.SimpleDraw.Core.Documents;

/// <summary>Only the revision that completed successfully is acknowledged.</summary>
public sealed class AutosaveState
{
    public long Revision { get; private set; }
    public long SavedRevision { get; private set; }
    public bool Pending => Revision > SavedRevision;
    public string? Error { get; private set; }
    public void Restore(long revision)
    {
        if (revision < 0 || revision > 9_007_199_254_740_990) throw new ArgumentOutOfRangeException(nameof(revision));
        Revision = SavedRevision = revision; Error = null;
    }
    public void Changed() { Revision = checked(Revision + 1); Error = null; }
    public void Acknowledge(long revision)
    {
        if (revision < SavedRevision || revision > Revision) throw new ArgumentOutOfRangeException(nameof(revision));
        SavedRevision = revision; Error = null;
    }
    public void Failed(string error) => Error = error;
}
