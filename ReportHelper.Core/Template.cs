namespace ReportHelper.Core;

public sealed class Template
{
    public const string Placeholders = "{players} {where} {when} {description} {quotes} {channels}";

    private const string DefaultBody = """
        ■ Character Name(s) and Home World (if known):
        {players}

        ■ World and area/duty where the incident occurred:
        {where}

        ■ Approximate date and time of the incident:
        {when}

        ■ Detailed description of the incident:
        {description}

        ■ Comments made by the player(s):
        {quotes}

        ■ Chat mode used:
        {channels}
        """;

    public string Name { get; set; } = "";

    public string Title { get; set; } = "";

    public string Body { get; set; } = "";

    public static Template Default() => new()
    {
        Name = "Harassment",
        Title = "Harassment by {players} in {where}",
        Body = DefaultBody,
    };
}
