namespace ReportHelper.Core;

public static class GameForm
{
    public const int TitleLimit = 200;

    // The body counter of the game's report form counts lines, not characters.
    public const int BodyLineLimit = 255;

    public static int Lines(string text) => text.AsSpan().Count('\n') + 1;
}
