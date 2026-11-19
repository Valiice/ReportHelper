using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using ReportHelper.Core;

namespace ReportHelper.Windows;

internal sealed class ReportTab(Configuration config, Session session, Action clearHistory)
{
    private const int MaxFieldBytes = 256;
    private const int MaxDescriptionBytes = 16384;
    private const float PlayerListWidth = 240;
    private const float ChatShare = 0.3f;
    private const float DescriptionShare = 0.3f;
    private const uint TankColor = 0xFFFFA060;
    private const uint HealerColor = 0xFF70D878;
    private const uint DpsColor = 0xFF7070F0;
    private static readonly Vector4 OverLimit = new(1, 0.4f, 0.4f, 1);

    private readonly ReportDraft draft = new();
    private string filter = "";

    public void Draw()
    {
        this.DrawPlayers();
        ImGui.SameLine();
        ImGui.BeginGroup();
        if (this.draft.Players.Count > 0)
        {
            this.DrawFields();
            this.DrawChat(session.Chat(this.draft.Players));
            this.DrawDescription();
            this.DrawResult();
        }
        else
        {
            ImGui.TextWrapped("Pick a player on the left. Ctrl+click adds more players from the same place to one report. Their chat from other places is listed too.");
        }

        ImGui.EndGroup();
    }

    private static float RowHeight => ImGui.GetFrameHeightWithSpacing();

    private static uint RoleColor(Role role) => role switch
    {
        Role.Tank => TankColor,
        Role.Healer => HealerColor,
        Role.Dps => DpsColor,
        _ => ImGui.GetColorU32(ImGuiCol.Text),
    };

    private static string PlaceLabel(Place place) => $"{place.Name}  {place.Start:MM-dd HH:mm} ST";

    private static void DrawLimit(int used, int limit, string unit)
    {
        var text = $"{used} / {limit} {unit}";
        if (used > limit)
        {
            ImGui.TextColored(OverLimit, $"{text}, too long for the game's form");
        }
        else
        {
            ImGui.TextDisabled(text);
        }
    }

    private static void CopyButton(string label, string text, bool fits)
    {
        ImGui.BeginDisabled(!fits);
        if (ImGui.Button(label))
        {
            ImGui.SetClipboardText(text);
        }

        ImGui.EndDisabled();
    }

    private void DrawPlayers()
    {
        ImGui.BeginGroup();
        ImGui.SetNextItemWidth(PlayerListWidth);
        ImGui.InputTextWithHint("##filter", "Filter players", ref this.filter, MaxFieldBytes);

        if (ImGui.BeginChild("players", new Vector2(PlayerListWidth, -RowHeight), true))
        {
            if (session.Groups.Count == 0)
            {
                ImGui.TextWrapped("Nobody yet. Players you party with or who talk near you show up here.");
            }

            foreach (var group in session.Groups)
            {
                this.DrawGroup(group);
            }
        }

        ImGui.EndChild();
        this.DrawClearHistory();
        ImGui.EndGroup();
    }

    private void DrawGroup(Group group)
    {
        Member[] shown = [.. group.Members.Where(m => m.Player.Display.Contains(this.filter, StringComparison.OrdinalIgnoreCase))];
        if (shown.Length == 0)
        {
            return;
        }

        ImGui.TextDisabled(PlaceLabel(group.Place));
        foreach (var (player, role) in shown)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, RoleColor(role));
            var clicked = ImGui.Selectable($"{player.Display}##{group.Place.Start.Ticks}", this.draft.IsSelected(group.Place, player));
            ImGui.PopStyleColor();
            if (clicked)
            {
                this.draft.Select(group.Place, player, addToSelection: ImGui.GetIO().KeyCtrl);
            }
        }

        ImGui.Spacing();
    }

    private void DrawClearHistory()
    {
        ImGui.BeginDisabled(!ImGui.GetIO().KeyCtrl);
        if (ImGui.Button("Clear history"))
        {
            clearHistory();
        }

        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip($"Hold Ctrl and click to delete everything recorded. History is kept for {Session.Retention.TotalHours:0} hours.");
        }
    }

    private void DrawFields()
    {
        ImGui.PushItemWidth(-ImGui.CalcTextSize("Template").X - ImGui.GetStyle().ItemInnerSpacing.X);
        TemplatePicker.Draw(config);

        var where = this.draft.Where;
        if (ImGui.InputTextWithHint("Where", this.draft.AutoWhere, ref where, MaxFieldBytes))
        {
            this.draft.Where = where;
        }

        var when = this.draft.When;
        if (ImGui.InputTextWithHint("When", this.draft.AutoWhen, ref when, MaxFieldBytes))
        {
            this.draft.When = when;
        }

        ImGui.PopItemWidth();
    }

    private void DrawChat(IReadOnlyList<Entry> chat)
    {
        if (ImGui.BeginChild("chat", new Vector2(0, ImGui.GetContentRegionAvail().Y * ChatShare), true))
        {
            if (chat.Count == 0)
            {
                ImGui.TextUnformatted("No chat recorded from the selected players.");
            }

            for (var i = 0; i < chat.Count; i++)
            {
                if (i == 0 || chat[i].Place != chat[i - 1].Place)
                {
                    ImGui.TextDisabled(PlaceLabel(chat[i].Place));
                }

                var quoted = this.draft.IsQuoted(chat[i]);
                if (ImGui.Checkbox($"##line{i}", ref quoted))
                {
                    this.draft.SetQuoted(chat[i], quoted);
                }

                ImGui.SameLine();
                ImGui.TextWrapped(ReportDraft.Quote(chat[i]));
            }
        }

        ImGui.EndChild();
    }

    private void DrawDescription()
    {
        ImGui.TextDisabled("What happened");
        var description = this.draft.Description;
        var size = new Vector2(-1, ImGui.GetContentRegionAvail().Y * DescriptionShare);
        if (ImGui.InputTextMultiline("##description", ref description, MaxDescriptionBytes, size))
        {
            this.draft.Description = description;
        }
    }

    private void DrawResult()
    {
        var template = config.CurrentTemplate();
        var title = this.draft.Fill(template.Title);
        var body = this.draft.Fill(template.Body);
        var lines = GameForm.Lines(body);

        var previewSize = new Vector2(-1, ImGui.GetContentRegionAvail().Y - (2 * RowHeight));
        ImGui.InputTextMultiline("##preview", ref body, Encoding.UTF8.GetByteCount(body) + 1, previewSize, ImGuiInputTextFlags.ReadOnly);

        CopyButton("Copy title", title, title.Length <= GameForm.TitleLimit);
        ImGui.SameLine();
        DrawLimit(title.Length, GameForm.TitleLimit, "characters");
        ImGui.SameLine();
        ImGui.TextUnformatted(title);

        CopyButton("Copy report", body, lines <= GameForm.BodyLineLimit);
        ImGui.SameLine();
        DrawLimit(lines, GameForm.BodyLineLimit, "lines");
    }
}
