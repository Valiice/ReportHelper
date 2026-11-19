namespace ReportHelper.Core;

public sealed class Session
{
    public const int MaxEntries = 20000;
    public const string UnknownArea = "Unknown area";
    public static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    private readonly List<Entry> entries;
    private readonly List<Entry> unsaved = [];
    private readonly HashSet<(Place, Player, Role)> known;
    private Place? current;
    private IReadOnlyList<Group>? groups;

    public Session(IEnumerable<Entry>? restored = null)
    {
        this.entries = [.. restored ?? []];
        this.known = [.. this.entries.Select(Key)];
    }

    public IReadOnlyList<Entry> Entries => this.entries;

    public IReadOnlyList<Group> Groups => this.groups ??=
    [
        .. this.entries
            .GroupBy(e => e.Place)
            .Select(g => new Group(g.Key, [.. g.GroupBy(e => e.Player).Select(LatestRole)]))
            .OrderByDescending(g => g.Place.Start),
    ];

    public void StartPlace(string name, DateTime now) => this.current = new Place(name, now);

    public void Seen(Player player, DateTime now, Role role = Role.Unknown)
    {
        var entry = new Entry(now, this.CurrentPlace(now), player, Role: role);
        if (!this.known.Contains(Key(entry)))
        {
            this.Add(entry);
        }
    }

    public void AddChat(Player player, ChatChannel channel, string text, DateTime now, Role role = Role.Unknown) =>
        this.Add(new Entry(now, this.CurrentPlace(now), player, channel, text, role));

    public IReadOnlyList<Entry> Chat(IReadOnlyCollection<Player> players) =>
        [.. this.entries.Where(e => e.IsChat && players.Contains(e.Player))];

    public IReadOnlyList<Entry> TakeUnsaved()
    {
        Entry[] taken = [.. this.unsaved];
        this.unsaved.Clear();
        return taken;
    }

    public bool Prune(DateTime now)
    {
        var cutoff = now - Retention;
        var firstFresh = this.entries.FindIndex(e => e.Time >= cutoff);
        var expired = firstFresh < 0 ? this.entries.Count : firstFresh;
        var remove = Math.Max(expired, this.entries.Count - MaxEntries);
        if (remove == 0)
        {
            return false;
        }

        this.entries.RemoveRange(0, remove);
        this.Reindex();
        return true;
    }

    public void Clear()
    {
        this.entries.Clear();
        this.unsaved.Clear();
        this.Reindex();
    }

    private static (Place, Player, Role) Key(Entry entry) => (entry.Place, entry.Player, entry.Role);

    private static Member LatestRole(IGrouping<Player, Entry> entries) =>
        new(entries.Key, entries.Select(e => e.Role).LastOrDefault(r => r != Role.Unknown));

    private Place CurrentPlace(DateTime now) => this.current ??= new Place(UnknownArea, now);

    private void Add(Entry entry)
    {
        this.entries.Add(entry);
        this.unsaved.Add(entry);
        this.known.Add(Key(entry));
        this.groups = null;
    }

    private void Reindex()
    {
        this.known.Clear();
        this.known.UnionWith(this.entries.Select(Key));
        this.groups = null;
    }
}
