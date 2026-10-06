using Microsoft.JSInterop;
namespace Beep.SimpleDraw.Web.Services;
public sealed record StoreResult(string Status, string? Error = null, long Revision = 0, string? Document = null, string? Raw = null);
public sealed class BrowserDocumentStore(IJSRuntime js, bool library = false) : IAsyncDisposable
{
    private IJSObjectReference? _module;
    private string Operation(string name) => library ? name + "Library" : name;
    public async Task<StoreResult> ConnectAsync<T>(DotNetObjectReference<T> reference) where T : class
    {
        _module = await js.InvokeAsync<IJSObjectReference>("import", "./js/documents.js");
        return await _module.InvokeAsync<StoreResult>(Operation("connect"), reference);
    }
    public ValueTask<StoreResult> SaveAsync(string json, long revision, bool recovery = false) => _module!.InvokeAsync<StoreResult>(Operation("save"), json, revision, recovery);
    public ValueTask<StoreResult> RetryAsync() => _module!.InvokeAsync<StoreResult>(Operation("retry"));
    public ValueTask GuardAsync(bool unsafeToLeave) => _module!.InvokeVoidAsync(Operation("guard"), unsafeToLeave);
    public async ValueTask DisposeAsync()
    {
        if (_module is null) return;
        await _module.InvokeVoidAsync(Operation("disconnect")); await _module.DisposeAsync();
    }
}
