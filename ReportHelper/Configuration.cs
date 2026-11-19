using Dalamud.Configuration;
using Dalamud.Plugin;
using ReportHelper.Core;

namespace ReportHelper;

public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public List<Template> Templates { get; set; } = [];

    public int SelectedTemplate { get; set; }

    public HashSet<ChatChannel> MutedChannels { get; set; } = [];

    public static Configuration Load(IDalamudPluginInterface pluginInterface)
    {
        var config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        if (config.Templates.Count == 0)
        {
            config.Templates.Add(Template.Default());
        }

        config.SelectedTemplate = Math.Clamp(config.SelectedTemplate, 0, config.Templates.Count - 1);
        return config;
    }

    public Template CurrentTemplate() => this.Templates[this.SelectedTemplate];

    public void Save() => Services.PluginInterface.SavePluginConfig(this);
}
