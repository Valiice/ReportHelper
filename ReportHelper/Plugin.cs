using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ReportHelper.Windows;

namespace ReportHelper;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/reporthelper";

    private readonly WindowSystem windowSystem = new("ReportHelper");
    private readonly MainWindow mainWindow;
    private Capture? capture;
    private bool attached;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Services>();
        var config = Configuration.Load(pluginInterface);
        var history = new SavedHistory(Path.Combine(pluginInterface.GetPluginConfigDirectory(), "history.jsonl"));
        this.mainWindow = new MainWindow(config, history.Session, history.Clear);
        this.windowSystem.AddWindow(this.mainWindow);

        // Dalamud never calls Dispose on a plugin whose ctor threw, so tear down ourselves.
        try
        {
            this.capture = new Capture(history, config);
            pluginInterface.UiBuilder.Draw += this.windowSystem.Draw;
            pluginInterface.UiBuilder.OpenMainUi += this.OpenMain;
            pluginInterface.UiBuilder.OpenConfigUi += this.OpenMain;
            Services.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, MainWindow.ReportForm, this.OnReportForm);
            Services.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, MainWindow.ReportForm, this.OnReportForm);
            Services.CommandManager.AddHandler(CommandName, new CommandInfo((_, _) => this.mainWindow.Toggle())
            {
                HelpMessage = "Open the Report Helper window.",
            });
        }
        catch
        {
            this.Teardown();
            throw;
        }
    }

    public void Dispose() => this.Teardown();

    private void Teardown()
    {
        Services.CommandManager.RemoveHandler(CommandName);
        Services.AddonLifecycle.UnregisterListener(this.OnReportForm);
        Services.PluginInterface.UiBuilder.OpenConfigUi -= this.OpenMain;
        Services.PluginInterface.UiBuilder.OpenMainUi -= this.OpenMain;
        Services.PluginInterface.UiBuilder.Draw -= this.windowSystem.Draw;
        this.windowSystem.RemoveAllWindows();
        this.capture?.Dispose();
        this.capture = null;
    }

    private void OpenMain() => this.mainWindow.IsOpen = true;

    private void OnReportForm(AddonEvent type, AddonArgs args)
    {
        if (type == AddonEvent.PostSetup)
        {
            this.attached = !this.mainWindow.IsOpen;
            this.mainWindow.IsOpen = true;
        }
        else if (this.attached)
        {
            this.mainWindow.IsOpen = false;
        }
    }
}
