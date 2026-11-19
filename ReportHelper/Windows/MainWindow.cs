using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using ReportHelper.Core;

namespace ReportHelper.Windows;

public class MainWindow : Window
{
    // The game uses this form for cheating, bug and suggestion reports as well.
    public const string ReportForm = "SupportDeskEdit";

    private const float DockGap = 8;

    private readonly ReportTab reportTab;
    private readonly TemplateTab templateTab;
    private readonly SettingsTab settingsTab;

    public MainWindow(Configuration config, Session session, Action clearHistory)
        : base("Report Helper###ReportHelperMain")
    {
        this.reportTab = new ReportTab(config, session, clearHistory);
        this.templateTab = new TemplateTab(config);
        this.settingsTab = new SettingsTab(config);
        this.Size = new Vector2(900, 650);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void PreDraw()
    {
        var form = Services.GameGui.GetAddonByName(ReportForm);
        if (!form.IsNull && form.IsVisible)
        {
            this.Position = new Vector2(form.X + form.ScaledWidth + DockGap, form.Y);
            this.PositionCondition = ImGuiCond.Always;
        }
        else
        {
            this.Position = null;
        }
    }

    public override void Draw()
    {
        if (!ImGui.BeginTabBar("tabs"))
        {
            return;
        }

        if (ImGui.BeginTabItem("Report"))
        {
            this.reportTab.Draw();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Templates"))
        {
            this.templateTab.Draw();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Settings"))
        {
            this.settingsTab.Draw();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }
}
