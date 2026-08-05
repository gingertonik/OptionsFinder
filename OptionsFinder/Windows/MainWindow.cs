using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Config;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using OptionsFinder.Options;

namespace OptionsFinder.Windows;

public class MainWindow : Window, IDisposable
{
    // hard cap on rendered rows per frame for sanity
    private const int MaxResults = 200;

    private string searchText = string.Empty;

    // windows (GameOption.Window values) with edits pending a native Apply click - see NativeApply.
    private readonly HashSet<string> dirtyWindows = new();

    public MainWindow() : base("OptionsFinder###OptionsFinderMainWindow")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(480, 360),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##Search", "Search Character/System Configuration options...", ref searchText, 128);

        ImGui.Spacing();
        DrawSaveBar();
        ImGui.Spacing();

        // empty query text - instructional
        if (string.IsNullOrWhiteSpace(searchText))
        {
            ImGui.TextDisabled("Type to search config options.");
            return;
        }

        // plain LINQ substring filter every frame
        var matches = OptionCatalog.Options
            .Where(Matches)
            .OrderBy(o => o.Window, StringComparer.Ordinal)
            .ThenBy(o => o.Category, StringComparer.Ordinal)
            .ThenBy(o => o.DisplayName, StringComparer.Ordinal)
            .ToList();

        if (matches.Count == 0)
        {
            ImGui.TextDisabled("No matching options.");
            return;
        }

        using var child = ImRaii.Child("OptionsFinderResults", Vector2.Zero, false);
        if (!child.Success)
            return;

        var shown = 0;
        foreach (var option in matches)
        {
            if (shown >= MaxResults)
            {
                ImGui.TextDisabled($"Showing first {MaxResults} of {matches.Count} matches. Refine your search to see more.");
                break;
            }

            DrawRow(option);
            shown++;
        }
    }

    // how it matches to search. uses display name, internal Dalamud key, category, and window
    private bool Matches(GameOption option)
    {
        return option.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || option.InternalName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || option.Category.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || option.Window.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    // pressing this simulates a click on the real native Apply button for each window with
    // pending edits - see NativeApply for how, and the plan notes for why Set() alone isn't enough.
    private void DrawSaveBar()
    {
        var hasChanges = dirtyWindows.Count > 0;
        using (ImRaii.Disabled(!hasChanges))
        {
            if (ImGui.Button("Save Changes"))
            {
                foreach (var window in dirtyWindows)
                    NativeApply.Trigger(window);
                dirtyWindows.Clear();
            }
        }

        if (hasChanges)
        {
            ImGui.SameLine();
            ImGui.TextDisabled($"Unapplied: {string.Join(", ", dirtyWindows)}");
        }
    }

    // marks option.Window dirty so the Save button knows to trigger that window's native
    // Apply. Options in windows NativeApply doesn't know how to trigger, or that are flagged
    // RequiresManualApply, are left untracked - Set() already wrote the value, there's just
    // no safe way to auto-apply it yet (see GameOption.RequiresManualApply for why).
    private void MarkDirty(GameOption option)
    {
        if (!option.RequiresManualApply && NativeApply.IsKnownWindow(option.Window))
            dirtyWindows.Add(option.Window);
    }

    private void DrawRow(GameOption option)
    {
        // PushId on the internal name. otherwise, ##value would collide and all edit as one.
        using var id = ImRaii.PushId(option.InternalName);
        using var group = ImRaii.Group();

        ImGui.TextDisabled($"{option.Window} › {option.Category}");
        ImGui.TextUnformatted(option.DisplayName);
        ImGui.SameLine();
        DrawEditor(option);

        if (option.RequiresManualApply)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("(!)");
            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                ImGui.TextUnformatted(
                    $"This changes your display mode/resolution, which the game confirms with " +
                    $"its own \"Keep these settings?\" dialog - Save can't trigger that safely, " +
                    $"so open {option.Window} > {option.Category} and click its Apply yourself.");
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
    }

    private void DrawEditor(GameOption option)
    {
        var section = ResolveSection(option);

        switch (option.Kind)
        {
            case ConfigValueKind.UInt:
                DrawUIntEditor(section, option);
                break;
            case ConfigValueKind.Float:
                DrawFloatEditor(section, option);
                break;
            case ConfigValueKind.String:
                DrawStringEditor(section, option);
                break;
        }
    }

    // GameOption.Section is just "System"/"UiConfig"/"UiControl" as a string (see
    // OptionCatalog.cs) rather than the actual Dalamud enum type. this avoids needing three
    // repeated paths for SystemConfigOption vs UiConfigOption vs UiControlOption
    private static GameConfigSection ResolveSection(GameOption option) => option.Section switch
    {
        "System" => Plugin.GameConfig.System,
        "UiConfig" => Plugin.GameConfig.UiConfig,
        "UiControl" => Plugin.GameConfig.UiControl,
        _ => throw new ArgumentOutOfRangeException(nameof(option), option.Section, "Unknown IGameConfig section"),
    };

    // this makes sure only legitimate values are accepted - returns the game's own min/max

    private void DrawUIntEditor(GameConfigSection section, GameOption option)
    {
        if (!section.TryGetUInt(option.InternalName, out var value))
            return;

        ImGui.SetNextItemWidth(200);

        // Label = dropdown
        if (option.Labels is { Count: > 0 } labels)
        {
            DrawComboEditor(section, option, value, labels);
            return;
        }

        if (!section.TryGetProperties(option.InternalName, out UIntConfigProperties? props) || props is null)
            return;

        // This is a checkbox option

        if (props.Minimum == 0 && props.Maximum == 1)
        {
            var boolValue = value != 0;
            if (ImGui.Checkbox("##value", ref boolValue))
            {
                section.Set(option.InternalName, boolValue ? 1u : 0u);
                MarkDirty(option);
            }

            return;
        }

        // some options report a min/max that isn't a usable slider range.
        // fall back to a read-only state.
        if (props.Maximum <= props.Minimum || props.Maximum > int.MaxValue)
        {
            ImGui.TextUnformatted(value.ToString());
            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                ImGui.TextUnformatted(
                    "The game reported an invalid or unusable range for this option, so it's shown " +
                    "read-only rather than risking a write outside what the native menu would accept.");
            }

            return;
        }

        var intValue = (int)value;
        if (ImGui.SliderInt("##value", ref intValue, (int)props.Minimum, (int)props.Maximum, "%d", ImGuiSliderFlags.AlwaysClamp))
        {
            // make sure the plugin doesn't write outside native bounds. clamp to the range.
            var clamped = Math.Clamp(intValue, (int)props.Minimum, (int)props.Maximum);
            section.Set(option.InternalName, (uint)clamped);
            MarkDirty(option);
        }
    }

    // this renders the dropdown according to the specified labels
    private void DrawComboEditor(GameConfigSection section, GameOption option, uint value, IReadOnlyList<string> labels)
    {
        var currentIndex = value < labels.Count ? (int)value : 0;

        using var combo = ImRaii.Combo("##value", labels[currentIndex]);
        if (!combo.Success)
            return;

        for (var i = 0; i < labels.Count; i++)
        {
            var selected = i == currentIndex;
            if (ImGui.Selectable(labels[i], selected))
            {
                section.Set(option.InternalName, (uint)i);
                MarkDirty(option);
            }

            if (selected)
                ImGui.SetItemDefaultFocus();
        }
    }

    private void DrawFloatEditor(GameConfigSection section, GameOption option)
    {
        if (!section.TryGetFloat(option.InternalName, out var value))
            return;

        if (!section.TryGetProperties(option.InternalName, out FloatConfigProperties? props) || props is null)
            return;

        ImGui.SetNextItemWidth(200);

        // same protection as DrawUIntEditor
        if (props.Maximum <= props.Minimum)
        {
            ImGui.TextUnformatted(value.ToString("0.###"));
            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                ImGui.TextUnformatted(
                    "The game reported an invalid or unusable range for this option, so it's shown " +
                    "read-only rather than risking a write outside what the native menu would accept.");
            }

            return;
        }

        if (ImGui.SliderFloat("##value", ref value, props.Minimum, props.Maximum, "%.3f", ImGuiSliderFlags.AlwaysClamp))
        {
            var clamped = Math.Clamp(value, props.Minimum, props.Maximum);
            section.Set(option.InternalName, clamped);
            MarkDirty(option);
        }
    }

    private void DrawStringEditor(GameConfigSection section, GameOption option)
    {
        if (!section.TryGetString(option.InternalName, out var value))
            return;

        // no length/format bound exposed for strings at all, and for some of these,
        // garbage text is potentially risky. point at the native menu instead.

        if (option.ReadOnly)
        {
            ImGui.TextUnformatted(value);
            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                ImGui.TextUnformatted(
                    $"This option isn't safely editable as free text through the plugin. " +
                    $"Open the native menu ({option.Window} > {option.Category}) to change it.");
            }

            return;
        }
        // for entries where garbage input is less risky, allow free text with warning.
        ImGui.SetNextItemWidth(200);
        if (ImGui.InputText("##value", ref value, 256))
        {
            section.Set(option.InternalName, value);
            MarkDirty(option);
        }

        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            using var tooltip = ImRaii.Tooltip();
            ImGui.TextUnformatted(
                "The game doesn't expose a length or format bound for this option through Dalamud, " +
                "so unlike the numeric fields above, this one isn't range-checked against what the " +
                "native menu would accept.");
        }
    }
}
