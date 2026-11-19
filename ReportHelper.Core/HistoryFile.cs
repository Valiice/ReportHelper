using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReportHelper.Core;

public sealed class HistoryFile(string path)
{
    private static readonly JsonSerializerOptions Options = new() { Converters = { new JsonStringEnumConverter() } };

    public IReadOnlyList<Entry> Load()
    {
        if (!File.Exists(path))
        {
            return [];
        }

        List<Entry> entries = [];
        foreach (var line in File.ReadLines(path))
        {
            if (Parse(line) is { Place: not null, Player: not null } entry)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    public void Append(IReadOnlyCollection<Entry> entries)
    {
        if (entries.Count > 0)
        {
            File.AppendAllLines(path, entries.Select(Serialize));
        }
    }

    public void Rewrite(IEnumerable<Entry> entries) => File.WriteAllLines(path, entries.Select(Serialize));

    private static string Serialize(Entry entry) => JsonSerializer.Serialize(entry, Options);

    private static Entry? Parse(string line)
    {
        try
        {
            return JsonSerializer.Deserialize<Entry>(line, Options);
        }
        catch (JsonException)
        {
            // A crash can leave a half-written last line.
            return null;
        }
    }
}
