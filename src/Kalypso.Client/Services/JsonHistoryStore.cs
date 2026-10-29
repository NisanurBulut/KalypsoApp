using System.Text.Json;
using Kalypso.Contracts;

namespace Kalypso.Client.Services;

/// <summary>Keeps the history in one JSON file. The app passes a path inside its private data folder.</summary>
public sealed class JsonHistoryStore(string path) : IHistoryStore
{
    public const int MaxItems = 200;

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<IReadOnlyList<LinkResponse>> LoadAsync()
    {
        await _gate.WaitAsync();
        try { return await ReadAsync(); }
        finally { _gate.Release(); }
    }

    public Task AddAsync(LinkResponse link) => UpdateAsync(items =>
    {
        items.RemoveAll(i => i.Code == link.Code);
        items.Insert(0, link);
    });

    public Task ReplaceAllAsync(IEnumerable<LinkResponse> links) => UpdateAsync(items =>
    {
        var replacement = links.ToList();
        items.Clear();
        items.AddRange(replacement);
    });

    public Task RemoveAsync(string code) => UpdateAsync(items => items.RemoveAll(i => i.Code == code));

    public Task ClearAsync() => UpdateAsync(items => items.Clear());

    private async Task UpdateAsync(Action<List<LinkResponse>> change)
    {
        await _gate.WaitAsync();
        try
        {
            var items = (await ReadAsync()).ToList();
            change(items);
            if (items.Count > MaxItems)
                items.RemoveRange(MaxItems, items.Count - MaxItems);

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(items, Options));
        }
        finally { _gate.Release(); }
    }

    private async Task<IReadOnlyList<LinkResponse>> ReadAsync()
    {
        if (!File.Exists(path))
            return [];
        try
        {
            return JsonSerializer.Deserialize<List<LinkResponse>>(await File.ReadAllTextAsync(path), Options) ?? [];
        }
        catch (JsonException)
        {
            return []; // a damaged file must not stop the app; it is rewritten on the next change
        }
    }
}
