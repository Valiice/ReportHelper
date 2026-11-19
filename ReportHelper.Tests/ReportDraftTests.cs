using ReportHelper.Core;
using Xunit;

namespace ReportHelper.Tests;

public class ReportDraftTests
{
    private static readonly Place Duty = new("The Aetherfont", new DateTime(2026, 11, 19, 20, 0, 0));
    private static readonly Place Limsa = new("Limsa Lominsa Lower Decks (Lich)", Duty.Start.AddHours(1));
    private static readonly Player Ann = new("Ann Example", "Lich");
    private static readonly Player Bob = new("Bob Example", "Odin");
    private static readonly Entry AnnParty = new(Duty.Start.AddMinutes(4), Duty, Ann, ChatChannel.Party, "one");
    private static readonly Entry BobTell = new(Duty.Start.AddMinutes(6), Duty, Bob, ChatChannel.Tell, "two");
    private static readonly Entry AnnLater = new(Duty.Start.AddMinutes(9), Duty, Ann, ChatChannel.Party, "three");

    private static ReportDraft Selected(params Player[] players)
    {
        var draft = new ReportDraft();
        foreach (var player in players)
        {
            draft.Select(Duty, player, addToSelection: true);
        }

        return draft;
    }

    [Fact]
    public void APlainClickSelectsOnlyThatPlayer()
    {
        var draft = Selected(Ann);
        draft.Select(Duty, Bob, addToSelection: false);

        Assert.Equal([Bob], draft.Players);
        Assert.True(draft.IsSelected(Duty, Bob));
        Assert.False(draft.IsSelected(Limsa, Bob));
    }

    [Fact]
    public void AddingToTheSelectionTogglesPlayers()
    {
        var draft = Selected(Ann, Bob);
        Assert.Equal([Ann, Bob], draft.Players);

        draft.Select(Duty, Ann, addToSelection: true);
        Assert.Equal([Bob], draft.Players);
    }

    [Fact]
    public void RemovingAPlayerDropsTheirQuotes()
    {
        var draft = Selected(Ann, Bob);
        draft.SetQuoted(AnnParty, true);
        draft.SetQuoted(BobTell, true);
        draft.Select(Duty, Ann, addToSelection: true);

        Assert.False(draft.IsQuoted(AnnParty));
        Assert.True(draft.IsQuoted(BobTell));
    }

    [Fact]
    public void AnotherPlaceStartsANewReport()
    {
        var draft = Selected(Ann);
        draft.Description = "typed";
        draft.SetQuoted(AnnParty, true);
        draft.Select(Limsa, Bob, addToSelection: true);

        Assert.Equal([Bob], draft.Players);
        Assert.Equal("", draft.Description);
        Assert.False(draft.IsQuoted(AnnParty));
    }

    [Fact]
    public void FillListsEveryPlayerAndThePlace()
    {
        var text = Selected(Ann, Bob).Fill("{players} / {where} / {when}");

        Assert.Equal("Ann Example@Lich, Bob Example@Odin / The Aetherfont / 2026-11-19 20:00 (Server Time)", text);
    }

    [Fact]
    public void WhereAddsThePlacesOfQuotedLinesAfterTheSelectedOne()
    {
        var draft = Selected(Ann);
        draft.SetQuoted(new Entry(Limsa.Start.AddMinutes(1), Limsa, Ann, ChatChannel.Tell, "later"), true);
        draft.SetQuoted(AnnParty, true);

        Assert.Equal("The Aetherfont, Limsa Lominsa Lower Decks (Lich)", draft.Fill("{where}"));
    }

    [Fact]
    public void WhenFollowsTheEarliestQuotedLine()
    {
        var draft = Selected(Ann, Bob);
        draft.SetQuoted(AnnLater, true);
        draft.SetQuoted(BobTell, true);

        Assert.Equal("2026-11-19 20:06 (Server Time)", draft.Fill("{when}"));
    }

    [Fact]
    public void TypedWhereAndWhenReplaceTheAutomaticOnes()
    {
        var draft = Selected(Ann);
        draft.Where = "Somewhere else";
        draft.When = "yesterday evening";
        draft.Description = "what happened";

        Assert.Equal("Somewhere else / yesterday evening / what happened", draft.Fill("{where} / {when} / {description}"));
    }

    [Fact]
    public void QuotesAreInTimeOrderAndChannelsAreListedOnce()
    {
        var draft = Selected(Ann, Bob);
        draft.SetQuoted(AnnLater, true);
        draft.SetQuoted(BobTell, true);
        draft.SetQuoted(AnnParty, true);

        Assert.Equal(
            "[20:04 ST] (Party) Ann Example: one\n[20:06 ST] (Tell) Bob Example: two\n[20:09 ST] (Party) Ann Example: three",
            draft.Fill("{quotes}"));
        Assert.Equal("Party, Tell", draft.Fill("{channels}"));
    }

    [Fact]
    public void WithoutQuotesBothQuotesAndChannelsAreEmpty()
    {
        Assert.Equal(" / ", Selected(Ann).Fill("{quotes} / {channels}"));
    }

    [Fact]
    public void EveryListedPlaceholderIsFilled()
    {
        Assert.DoesNotContain("{", Selected(Ann).Fill(Template.Placeholders), StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownPlaceholdersAndStrayBracesAreLeftAlone()
    {
        Assert.Equal("{nope} { Ann Example@Lich", Selected(Ann).Fill("{nope} { {players}"));
    }

    [Fact]
    public void PlaceholderTextInsideChatIsNotExpanded()
    {
        var draft = Selected(Ann);
        draft.SetQuoted(AnnParty with { Text = "{players}" }, true);

        Assert.Equal("[20:04 ST] (Party) Ann Example: {players}", draft.Fill("{quotes}"));
    }

    [Fact]
    public void TheDefaultTemplateLeavesNoPlaceholderBehind()
    {
        var template = Template.Default();
        var draft = Selected(Ann);

        Assert.DoesNotContain("{", draft.Fill(template.Body));
        Assert.Equal("Harassment by Ann Example@Lich in The Aetherfont", draft.Fill(template.Title));
    }

    [Fact]
    public void LinesCountsLineBreaks()
    {
        Assert.Equal(1, GameForm.Lines(""));
        Assert.Equal(3, GameForm.Lines("a\nb\n"));
    }
}
