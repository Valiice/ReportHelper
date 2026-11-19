using System.Text.Json.Serialization;

namespace ReportHelper.Core;

public enum ChatChannel
{
    Party,
    Alliance,
    Tell,
    Say,
    Shout,
    Yell,
    Emote,
}

public enum Role
{
    Unknown,
    Tank,
    Healer,
    Dps,
}

public sealed record Place(string Name, DateTime Start);

public sealed record Entry(
    DateTime Time, Place Place, Player Player, ChatChannel? Channel = null, string Text = "", Role Role = Role.Unknown)
{
    [JsonIgnore]
    public bool IsChat => this.Channel != null;
}

public sealed record Member(Player Player, Role Role);

public sealed record Group(Place Place, IReadOnlyList<Member> Members)
{
    public IReadOnlyList<Player> Players => [.. this.Members.Select(m => m.Player)];
}
