using System;

namespace Godoodads.Dungeon.Core;

/// <summary>
/// The oldest WFC Generator that can sit next to godoodungeon. From 1.3.3 on, WFC Generator uses
/// the shared tile set classes in editor_tools_common; older versions ship their own copies, whose
/// duplicate uids make tile sets load as the old types (brief godoodungeon §4.1, §10.2). From 1.4.0
/// on it is named godoowave and lives in addons/godoowave.
/// </summary>
public static class WfcGeneratorVersion
{
    public static readonly Version Minimum = new(1, 3, 3);

    /// <summary>True when <paramref name="installed"/> (a plugin.cfg version) is older than <see cref="Minimum"/>. Unreadable versions are not reported.</summary>
    public static bool NeedsUpdate(string installed) =>
        Version.TryParse(installed?.Trim(), out var version) && version < Minimum;
}
