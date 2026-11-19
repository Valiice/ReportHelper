using Dalamud.Bindings.ImGui;
using ReportHelper.Core;

namespace ReportHelper.Windows;

internal sealed class SettingsTab(Configuration config)
{
    public void Draw()
    {
        ImGui.TextWrapped($"Chat to record. Lines already recorded stay until they are {Session.Retention.TotalHours:0} hours old or the history is cleared.");
        foreach (var channel in Enum.GetValues<ChatChannel>())
        {
            var recorded = !config.MutedChannels.Contains(channel);
            if (ImGui.Checkbox(channel.ToString(), ref recorded))
            {
                _ = recorded ? config.MutedChannels.Remove(channel) : config.MutedChannels.Add(channel);
                config.Save();
            }
        }
    }
}
