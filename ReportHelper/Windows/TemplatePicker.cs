using Dalamud.Bindings.ImGui;

namespace ReportHelper.Windows;

internal static class TemplatePicker
{
    public static void Draw(Configuration config)
    {
        if (!ImGui.BeginCombo("Template", config.CurrentTemplate().Name))
        {
            return;
        }

        for (var i = 0; i < config.Templates.Count; i++)
        {
            if (ImGui.Selectable($"{config.Templates[i].Name}##{i}", i == config.SelectedTemplate))
            {
                config.SelectedTemplate = i;
                config.Save();
            }
        }

        ImGui.EndCombo();
    }
}
