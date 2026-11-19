using System.Numerics;
using Dalamud.Bindings.ImGui;
using ReportHelper.Core;

namespace ReportHelper.Windows;

internal sealed class TemplateTab(Configuration config)
{
    private const int MaxNameBytes = 64;
    private const int MaxTitleBytes = 512;
    private const int MaxBodyBytes = 65536;

    public void Draw()
    {
        TemplatePicker.Draw(config);
        this.DrawAddAndDelete();

        var template = config.CurrentTemplate();
        var name = template.Name;
        if (ImGui.InputText("Name", ref name, MaxNameBytes))
        {
            template.Name = name;
        }

        var title = template.Title;
        if (ImGui.InputText("Title", ref title, MaxTitleBytes))
        {
            template.Title = title;
        }

        ImGui.TextWrapped($"Placeholders: {Template.Placeholders}");
        var body = template.Body;
        var height = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing();
        if (ImGui.InputTextMultiline("##body", ref body, MaxBodyBytes, new Vector2(-1, height)))
        {
            template.Body = body;
        }

        if (ImGui.Button("Save"))
        {
            config.Save();
        }
    }

    private void DrawAddAndDelete()
    {
        ImGui.SameLine();
        if (ImGui.Button("New"))
        {
            var added = Template.Default();
            added.Name = "New template";
            config.Templates.Add(added);
            config.SelectedTemplate = config.Templates.Count - 1;
            config.Save();
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(config.Templates.Count == 1);
        if (ImGui.Button("Delete"))
        {
            config.Templates.RemoveAt(config.SelectedTemplate);
            config.SelectedTemplate = Math.Min(config.SelectedTemplate, config.Templates.Count - 1);
            config.Save();
        }

        ImGui.EndDisabled();
    }
}
