using ReportHelper.Core;
using Xunit;

namespace ReportHelper.Tests;

public sealed class HistoryFileTests : IDisposable
{
    private static readonly Place Duty = new("The Aetherfont", new DateTime(2026, 11, 19, 21, 0, 0, DateTimeKind.Utc));
    private static readonly Player Ann = new("Ann Example", "Lich");
    private static readonly Entry Seen = new(Duty.Start.AddMinutes(1), Duty, Ann);
    private static readonly Entry Said = new(Duty.Start.AddMinutes(2), Duty, Ann, ChatChannel.Party, "line with \"quotes\"\nand a break");

    private readonly string path = Path.Combine(Path.GetTempPath(), $"reporthelper-{Guid.NewGuid():N}.jsonl");

    public void Dispose() => File.Delete(this.path);

    [Fact]
    public void AMissingFileLoadsAsEmpty()
    {
        Assert.Empty(new HistoryFile(this.path).Load());
    }

    [Fact]
    public void AppendedEntriesComeBackUnchanged()
    {
        var file = new HistoryFile(this.path);
        file.Append([Seen]);
        file.Append([Said]);

        Assert.Equal([Seen, Said], file.Load());
    }

    [Fact]
    public void RewriteReplacesTheContents()
    {
        var file = new HistoryFile(this.path);
        file.Append([Seen, Said]);
        file.Rewrite([Said]);

        Assert.Equal([Said], file.Load());
    }

    [Fact]
    public void DamagedLinesAreSkipped()
    {
        var file = new HistoryFile(this.path);
        file.Append([Seen]);
        File.AppendAllLines(this.path, ["{\"Time\":\"2026-11-19T21:0", "null", "{}", "not json"]);
        file.Append([Said]);

        Assert.Equal([Seen, Said], file.Load());
    }
}
