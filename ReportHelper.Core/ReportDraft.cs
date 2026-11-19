using System.Globalization;
using System.Text.RegularExpressions;

namespace ReportHelper.Core;

public sealed partial class ReportDraft
{
    private readonly List<Player> players = [];
    private readonly HashSet<Entry> quoted = [];

    public Place? Place { get; private set; }

    public IReadOnlyList<Player> Players => this.players;

    public string Where { get; set; } = "";

    public string When { get; set; } = "";

    public string Description { get; set; } = "";

    public string AutoWhere => string.Join(", ", this.Places().Select(p => p.Name));

    public string AutoWhen
    {
        get
        {
            var time = this.quoted.Count > 0 ? this.quoted.Min(e => e.Time) : this.Place?.Start;
            return time?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " (Server Time)";
        }
    }

    public static string Quote(Entry entry) =>
        $"[{entry.Time.ToString("HH:mm", CultureInfo.InvariantCulture)} ST] ({entry.Channel}) {entry.Player.Name}: {entry.Text}";

    public void Select(Place place, Player player, bool addToSelection)
    {
        if (!addToSelection || place != this.Place)
        {
            this.StartOver(place);
            this.players.Add(player);
        }
        else if (this.players.Remove(player))
        {
            this.quoted.RemoveWhere(e => e.Player == player);
        }
        else
        {
            this.players.Add(player);
        }
    }

    public bool IsSelected(Place place, Player player) => place == this.Place && this.players.Contains(player);

    public bool IsQuoted(Entry entry) => this.quoted.Contains(entry);

    public void SetQuoted(Entry entry, bool on) => _ = on ? this.quoted.Add(entry) : this.quoted.Remove(entry);

    public string Fill(string text)
    {
        Entry[] quotes = [.. this.quoted.OrderBy(e => e.Time)];

        // One pass, so placeholder-looking text inside chat is never expanded.
        return Placeholder().Replace(text, m => m.Groups[1].Value switch
        {
            "players" => string.Join(", ", this.players.Select(p => p.Display)),
            "where" => this.Where.Length > 0 ? this.Where : this.AutoWhere,
            "when" => this.When.Length > 0 ? this.When : this.AutoWhen,
            "description" => this.Description,
            "quotes" => string.Join('\n', quotes.Select(Quote)),
            "channels" => string.Join(", ", quotes.Select(e => e.Channel).Distinct()),
            _ => m.Value,
        });
    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex Placeholder();

    private IEnumerable<Place> Places()
    {
        Place[] selected = this.Place == null ? [] : [this.Place];
        return selected.Concat(this.quoted.OrderBy(e => e.Time).Select(e => e.Place)).Distinct();
    }

    private void StartOver(Place place)
    {
        this.Place = place;
        this.players.Clear();
        this.quoted.Clear();
        this.Where = "";
        this.When = "";
        this.Description = "";
    }
}
