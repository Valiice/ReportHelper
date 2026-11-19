using ReportHelper.Core;
using Xunit;

namespace ReportHelper.Tests;

public class SessionTests
{
    private static readonly DateTime Start = new(2026, 11, 19, 21, 0, 0);
    private static readonly Player Ann = new("Ann Example", "Lich");
    private static readonly Player Bob = new("Bob Example", "Odin");

    private static DateTime At(double minutes) => Start.AddMinutes(minutes);

    [Fact]
    public void SeenListsAPlayerOncePerPlace()
    {
        var session = new Session();
        session.StartPlace("The Aetherfont", At(0));
        session.Seen(Ann, At(1));
        session.Seen(Ann, At(2));

        var group = Assert.Single(session.Groups);
        Assert.Equal("The Aetherfont", group.Place.Name);
        Assert.Equal([Ann], group.Players);
        Assert.Single(session.Entries);
    }

    [Fact]
    public void AMemberHasTheLatestKnownRole()
    {
        var session = new Session();
        session.Seen(Ann, At(0), Role.Tank);
        session.Seen(Ann, At(1), Role.Tank);
        session.Seen(Ann, At(2), Role.Healer);
        session.AddChat(Ann, ChatChannel.Party, "hi", At(3));
        session.AddChat(Bob, ChatChannel.Say, "hey", At(4));

        Assert.Equal([new Member(Ann, Role.Healer), new Member(Bob, Role.Unknown)], Assert.Single(session.Groups).Members);
        Assert.Equal(4, session.Entries.Count);
    }

    [Fact]
    public void SameNameOnAnotherWorldIsAnotherPlayer()
    {
        var session = new Session();
        session.Seen(Ann, At(1));
        session.Seen(Ann with { World = "Odin" }, At(1));

        Assert.Equal(2, Assert.Single(session.Groups).Players.Count);
    }

    [Fact]
    public void GroupsAreNewestFirstAndEmptyPlacesAreNotListed()
    {
        var session = new Session();
        session.StartPlace("First", At(0));
        session.Seen(Ann, At(1));
        session.StartPlace("Passed through", At(10));
        session.StartPlace("Second", At(30));
        session.Seen(Ann, At(31));

        Assert.Equal(["Second", "First"], session.Groups.Select(g => g.Place.Name));
    }

    [Fact]
    public void BeforeAnyPlaceIsKnownPlayersGoToUnknownArea()
    {
        var session = new Session();
        session.Seen(Ann, At(0));

        Assert.Equal(Session.UnknownArea, Assert.Single(session.Groups).Place.Name);
    }

    [Fact]
    public void AnyoneWhoSpeaksIsListedInTheCurrentPlace()
    {
        var session = new Session();
        session.StartPlace("Limsa Lominsa Lower Decks (Lich)", At(0));
        session.AddChat(Ann, ChatChannel.Tell, "hi", At(1));
        session.AddChat(Bob, ChatChannel.Shout, "hey", At(2));

        Assert.Equal([Ann, Bob], Assert.Single(session.Groups).Players);
    }

    [Fact]
    public void ChatSpansPlacesAndKeepsWhereEachLineWasSaid()
    {
        var session = new Session();
        session.StartPlace("First", At(0));
        session.AddChat(Ann, ChatChannel.Party, "in first", At(1));
        session.StartPlace("Second", At(30));
        session.AddChat(Ann, ChatChannel.Tell, "in second", At(31));

        Assert.Equal([("First", "in first"), ("Second", "in second")], session.Chat([Ann]).Select(e => (e.Place.Name, e.Text)));
    }

    [Fact]
    public void ChatOfSeveralPlayersIsMergedInTimeOrderWithoutSeenEntries()
    {
        var session = new Session();
        session.StartPlace("Duty", At(0));
        session.Seen(Ann, At(1));
        session.AddChat(Ann, ChatChannel.Party, "one", At(2));
        session.AddChat(Bob, ChatChannel.Party, "two", At(3));
        session.AddChat(Ann, ChatChannel.Party, "three", At(4));

        Assert.Equal(["one", "two", "three"], session.Chat([Ann, Bob]).Select(e => e.Text));
        Assert.Equal(["two"], session.Chat([Bob]).Select(e => e.Text));
    }

    [Fact]
    public void PruneDropsEntriesOlderThanTheRetention()
    {
        var session = new Session();
        session.AddChat(Ann, ChatChannel.Party, "old", At(0));
        session.AddChat(Bob, ChatChannel.Party, "fresh", At(60));

        Assert.False(session.Prune(At(0) + Session.Retention));
        Assert.True(session.Prune(At(1) + Session.Retention));
        Assert.Equal(["fresh"], session.Entries.Select(e => e.Text));
        Assert.Equal([Bob], Assert.Single(session.Groups).Players);
    }

    [Fact]
    public void PruneKeepsTheNewestMaxEntries()
    {
        var session = new Session();
        for (var i = 0; i <= Session.MaxEntries; i++)
        {
            session.AddChat(Ann, ChatChannel.Say, $"line {i}", At(0));
        }

        Assert.True(session.Prune(At(1)));
        Assert.Equal(Session.MaxEntries, session.Entries.Count);
        Assert.Equal("line 1", session.Entries[0].Text);
    }

    [Fact]
    public void APrunedPlayerIsRecordedAgainWhenSeenAgain()
    {
        var session = new Session();
        session.StartPlace("Duty", At(0));
        session.Seen(Ann, At(0));
        session.Prune(At(1) + Session.Retention);
        session.Seen(Ann, At(2) + Session.Retention);

        Assert.Equal([Ann], Assert.Single(session.Groups).Players);
    }

    [Fact]
    public void TakeUnsavedReturnsEachNewEntryOnce()
    {
        var session = new Session();
        session.Seen(Ann, At(0));
        session.AddChat(Ann, ChatChannel.Party, "hi", At(1));

        Assert.Equal(2, session.TakeUnsaved().Count);
        Assert.Empty(session.TakeUnsaved());
    }

    [Fact]
    public void RestoredEntriesAreListedAndAlreadySaved()
    {
        var place = new Place("Duty", At(0));
        var session = new Session([new Entry(At(1), place, Ann, ChatChannel.Party, "hi")]);

        Assert.Equal([Ann], Assert.Single(session.Groups).Players);
        Assert.Empty(session.TakeUnsaved());
    }

    [Fact]
    public void ClearForgetsEverything()
    {
        var session = new Session();
        session.AddChat(Ann, ChatChannel.Party, "hi", At(1));
        session.Clear();

        Assert.Empty(session.Entries);
        Assert.Empty(session.Groups);
        Assert.Empty(session.TakeUnsaved());
    }
}
