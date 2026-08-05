using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Config;
using Dalamud.Plugin.Services;

namespace OptionsFinder.Options;

// this is a tripwire that runs on load. the function is to detect if a game patch adds
// or removes config options. if tripped, the plugin sends a message to the dalamud log.

public static class CatalogDriftCheck
{
    public static void Run(IPluginLog log)
    {
        var known = new HashSet<string>(OptionCatalog.Options.Select(o => o.InternalName));
        known.UnionWith(RejectedOptionNames.Names);

        var live = Enum.GetNames<SystemConfigOption>()
            .Concat(Enum.GetNames<UiConfigOption>())
            .Concat(Enum.GetNames<UiControlOption>())
            .ToList();

        // in the live enums but not cataloged or queued 

        var newlyAdded = live.Where(n => !known.Contains(n)).OrderBy(n => n, StringComparer.Ordinal).ToList();
        if (newlyAdded.Count > 0)
        {
            log.Warning(
                "OptionsFinder: {Count} Dalamud config option(s) aren't in OptionCatalog.cs: {Names}. " +
                "Re-run the enum audit and sort them in.",
                newlyAdded.Count,
                string.Join(", ", newlyAdded));
        }

        // in the catalog but not showing in game

        var liveSet = new HashSet<string>(live);
        var stale = OptionCatalog.Options
            .Select(o => o.InternalName)
            .Where(n => !liveSet.Contains(n))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
        if (stale.Count > 0)
        {
            log.Warning(
                "OptionsFinder: {Count} option(s) in OptionCatalog.cs no longer exist in Dalamud's config " +
                "enums {Names}. These will show up empty in search.",
                stale.Count,
                string.Join(", ", stale));
        }
    }
}
