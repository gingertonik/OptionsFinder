using System.Collections.Generic;

namespace OptionsFinder.Options;

public enum ConfigValueKind
{
    UInt,
    Float,
    String,
}


// this controls the kinds of data each enum contains
public sealed record GameOption(
    string Section,
    string InternalName,
    string Window,
    string Category,
    string DisplayName,
    ConfigValueKind Kind,
    IReadOnlyList<string>? Labels = null,
    bool ReadOnly = false,
    // device-mode changes (screen mode/resolution/refresh rate) make the game show its own
    // "Keep these settings?" confirmation dialog when Apply is clicked for real - our
    // synthetic Apply click doesn't trigger that dialog, so these are excluded from the Save
    // button entirely rather than risk an unconfirmed change. Set() still writes the value;
    // the user has to open the native window and click its Apply themselves for these.
    bool RequiresManualApply = false);
