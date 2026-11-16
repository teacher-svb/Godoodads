# godoocurves

Generate preset Bezier shapes on a `Path3D`, straight from the Godot editor: arcs, S-bends, waves, loops, helixes, corkscrews, banked turns and closed outlines. Each shape either replaces the path's curve or is appended at its end, so a track can be built from pieces that join smoothly. Shapes that lean, like banked turns and corkscrews, set the curve's tilt.

**Status:** in development (milestone M0). This version only adds the menu entry and an empty dialog. The plan is in [docs/briefs/godoocurves.md](../../docs/briefs/godoocurves.md).

**Requirements:** Godot 4.7 or later, the **.NET** edition, and the [.NET SDK](https://dotnet.microsoft.com/download) 8.0 or later.
