using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace OptionsFinder.Options;

// Simulates pressing the native "Apply" button for the config windows
public static class NativeApply
{
    // slight wait to close the window
    private const int CloseDelayTicks = 30;

    private readonly record struct Target(string AddonName, uint ApplyNodeId, uint ApplyEventParam, uint MainCommandId);

    private static readonly Dictionary<string, Target> Targets = new()
    {
        ["System Configuration"] = new Target("ConfigSystem", 590, 2, 19),
        ["Character Configuration"] = new Target("ConfigCharacter", 37, 0, 34),
    };

    public static bool IsKnownWindow(string window) => Targets.ContainsKey(window);

    // Triggers Apply for the native window.  If that window isn't already open this session,
    // opens it first via the game's own Main Command system, then clicks Apply once it's
    // finished setup - this will visibly flash the native window open, then close it again
    public static unsafe void Trigger(string window)
    {
        if (!Targets.TryGetValue(window, out var target))
            return;

        var addon = Plugin.GameGui.GetAddonByName<AtkUnitBase>(target.AddonName);
        if (addon != null)
        {
            ClickApply(addon, target);
            return;
        }

        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, target.AddonName, OnPostSetup);
        UIModule.Instance()->ExecuteMainCommand(target.MainCommandId);
        return;

        void OnPostSetup(AddonEvent type, AddonArgs args)
        {
            Plugin.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, target.AddonName, OnPostSetup);
            var opened = (AtkUnitBase*)(nint)args.Addon;
            if (opened != null)
                ClickApply(opened, target);
        }
    }

    private static unsafe void ClickApply(AtkUnitBase* addon, Target target)
    {
        var node = addon->GetNodeById(target.ApplyNodeId);
        if (node == null)
            return;

        var atkEvent = new AtkEvent
        {
            Node = node,
            Listener = (AtkEventListener*)addon,
            Param = target.ApplyEventParam,
        };
        var eventData = new AtkEventData();
        addon->ReceiveEvent(AtkEventType.ButtonClick, (int)target.ApplyEventParam, &atkEvent, &eventData);
        
        // closes the native config window after waiting. DisableHideTransition skips the fade-out
        // that was eating the next open command
        var addonName = target.AddonName;
        Plugin.Framework.RunOnTick(() =>
        {
            var stillOpen = Plugin.GameGui.GetAddonByName<AtkUnitBase>(addonName);
            if (stillOpen != null)
            {
                stillOpen->DisableHideTransition = true;
                stillOpen->Close(false);
            }
        }, delayTicks: CloseDelayTicks);
    }
}
