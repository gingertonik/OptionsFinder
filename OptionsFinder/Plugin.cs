using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using OptionsFinder.Options;
using OptionsFinder.Windows;

namespace OptionsFinder;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService] internal static IGameConfig GameConfig { get; private set; } = null!;
    // used by CatalogDriftCheck's startup warning.
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    // used by NativeApply to find/click the native Apply button and to open its window if closed.
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    // used by NativeApply to delay closing the window until Apply's effect has actually landed.
    [PluginService] internal static IFramework Framework { get; private set; } = null!;

    private const string CommandName = "/ofinder";
    private const string CommandAlias = "/pfind";

    public readonly WindowSystem WindowSystem = new("OptionsFinder");
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        MainWindow = new MainWindow();
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Search and edit Character/System Configuration options. Also available as /pfind"
        });

        CommandManager.AddHandler(CommandAlias, new CommandInfo(OnCommand)
        {
            HelpMessage = "Search and edit Character/System Configuration options."
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;

        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        //so i don't get that validation issue without a config window
        PluginInterface.UiBuilder.OpenConfigUi += ToggleMainUi;

        CatalogDriftCheck.Run(Log);
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();

        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(CommandAlias);
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.Toggle();
    }

    public void ToggleMainUi() => MainWindow.Toggle();
}
