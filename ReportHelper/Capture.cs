using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using Lumina.Excel.Sheets;
using ReportHelper.Core;
using Role = ReportHelper.Core.Role;

namespace ReportHelper;

internal sealed class Capture : IDisposable
{
    private const byte FilledAllianceSlot = 1;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private readonly SavedHistory history;
    private readonly Session session;
    private readonly Configuration config;
    private DateTime nextPoll;

    // Naming the place waits for the local player, who is missing while a zone loads.
    private bool placeChanged = true;

    public Capture(SavedHistory history, Configuration config)
    {
        this.history = history;
        this.session = history.Session;
        this.config = config;
        Services.ClientState.TerritoryChanged += this.OnTerritoryChanged;
        Services.Framework.Update += this.OnUpdate;
        Services.ChatGui.ChatMessage += this.OnChatMessage;
    }

    public void Dispose()
    {
        Services.ChatGui.ChatMessage -= this.OnChatMessage;
        Services.Framework.Update -= this.OnUpdate;
        Services.ClientState.TerritoryChanged -= this.OnTerritoryChanged;
        this.history.Save(DateTime.UtcNow);
    }

    private static string WorldName(uint id) =>
        Services.DataManager.GetExcelSheet<World>().GetRowOrDefault(id)?.Name.ExtractText() ?? "";

    private static Role RoleOf(ClassJob? job) => job?.Role switch
    {
        1 => Role.Tank,
        4 => Role.Healer,
        2 or 3 => Role.Dps,
        _ => Role.Unknown,
    };

    private static Role NearbyRole(PlayerPayload sender) => RoleOf(Services.ObjectTable
        .OfType<IPlayerCharacter>()
        .FirstOrDefault(p => p.HomeWorld.RowId == sender.World.RowId && p.Name.TextValue == sender.PlayerName)?
        .ClassJob.ValueNullable);

    private static ChatChannel? Channel(XivChatType type) => type switch
    {
        XivChatType.Party or XivChatType.CrossParty => ChatChannel.Party,
        XivChatType.Alliance => ChatChannel.Alliance,
        XivChatType.TellIncoming => ChatChannel.Tell,
        XivChatType.Say => ChatChannel.Say,
        XivChatType.Shout => ChatChannel.Shout,
        XivChatType.Yell => ChatChannel.Yell,
        XivChatType.StandardEmote or XivChatType.CustomEmote => ChatChannel.Emote,
        _ => null,
    };

    // Null for the local player's own lines, which carry no player payload.
    private static PlayerPayload? Sender(IChatMessage message, ChatChannel channel)
    {
        var sender = message.Sender.Payloads.OfType<PlayerPayload>().FirstOrDefault();
        if (channel != ChatChannel.Emote)
        {
            return sender;
        }

        // An own emote names its target first in the body, so it must not be read as the sender.
        var ownName = Services.ObjectTable.LocalPlayer?.Name.TextValue;
        if (sender == null && ownName != null && message.Sender.TextValue.Contains(ownName, StringComparison.Ordinal))
        {
            return null;
        }

        return message.Message.Payloads.OfType<PlayerPayload>().FirstOrDefault() ?? sender;
    }

    private static string PlaceName(IPlayerCharacter me)
    {
        var row = Services.DataManager.GetExcelSheet<TerritoryType>().GetRowOrDefault(Services.ClientState.TerritoryType);
        var duty = row?.ContentFinderCondition.ValueNullable?.Name.ExtractText();
        if (!string.IsNullOrEmpty(duty))
        {
            return char.ToUpperInvariant(duty[0]) + duty[1..];
        }

        var zone = row?.PlaceName.ValueNullable?.Name.ExtractText();
        return $"{(string.IsNullOrEmpty(zone) ? Session.UnknownArea : zone)} ({me.CurrentWorld.Value.Name.ExtractText()})";
    }

    private void OnTerritoryChanged(uint _) => this.placeChanged = true;

    private void OnUpdate(IFramework framework)
    {
        var now = DateTime.UtcNow;
        if (now < this.nextPoll)
        {
            return;
        }

        this.nextPoll = now + PollInterval;

        try
        {
            this.PollParty(now);
            this.history.Save(now);
        }
        catch (Exception e)
        {
            Services.Log.Error(e, "ReportHelper: poll failed");
        }
    }

    private void PollParty(DateTime now)
    {
        if (Services.ObjectTable.LocalPlayer is not { } me)
        {
            return;
        }

        if (this.placeChanged)
        {
            this.placeChanged = false;
            this.session.StartPlace(PlaceName(me), now);
        }

        foreach (var m in Services.PartyList)
        {
            if (m.EntityId != me.EntityId)
            {
                this.Seen(m.Name.TextValue, m.World.RowId, m.ClassJob.ValueNullable, now);
            }
        }

        this.PollAlliance(now);
    }

    private unsafe void PollAlliance(DateTime now)
    {
        var manager = GroupManager.Instance();
        if (manager == null || !manager->MainGroup.IsAlliance)
        {
            return;
        }

        foreach (ref readonly var m in manager->MainGroup.AllianceMembers)
        {
            if ((m.Flags & FilledAllianceSlot) != 0)
            {
                this.Seen(m.NameString, m.HomeWorld, Services.DataManager.GetExcelSheet<ClassJob>().GetRowOrDefault(m.ClassJob), now);
            }
        }
    }

    private void Seen(string name, uint world, ClassJob? job, DateTime now)
    {
        if (name.Length > 0 && world != 0)
        {
            this.session.Seen(new Player(name, WorldName(world)), now, RoleOf(job));
        }
    }

    private void OnChatMessage(IHandleableChatMessage message)
    {
        if (Channel(message.LogKind) is not { } channel
            || this.config.MutedChannels.Contains(channel)
            || Sender(message, channel) is not { } sender)
        {
            return;
        }

        var player = new Player(sender.PlayerName, sender.World.Value.Name.ExtractText());
        this.session.AddChat(player, channel, message.Message.TextValue, DateTime.UtcNow, NearbyRole(sender));
    }
}
