# Changelog

All notable changes to godoocurves are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses [Semantic Versioning](https://semver.org/).

## [1.0.0] - 2026-11-16

First release.

### Added
- **Generate Curve (GDDS)** for a selected `Path3D`, on its own **Path3D** toolbar button. It opens a dialog whose settings are stored on the path, so reopening it continues where you left off.
- **16 shapes:** Straight, Parabolic, Circle, Semicircle, Loop, Wave, Spiral, Arc, S-bend, Ellipse, Helix, Corkscrew, Banked Turn, Rounded Rectangle, Polygon and Figure Eight. Each is made of exact arcs or fitted cubic segments, within 0.1 % of the exact shape. Only the settings a shape uses are shown.
- **Replace** puts the shape at Start, heading along Direction with Up as its up direction. **Append** adds it at the end of the path's curve, continuing its direction, up and tilt, so a track can be built piece by piece without kinks. Every shape starts along its direction, except Parabolic and Helix, which leave at their slope.
- **Tilt:** banked turns, corkscrews and loops set the curve's tilt, measured against Godot's own up vectors, so `CSGPolygon3D` (Path Follow) and `PathFollow3D` (rotation mode XYZ) lean with the track.
- **Preview:** while the dialog is open, the shape is drawn where Generate puts it, with ticks showing its up direction.
- Every Generate is one undo step and restores the stored settings too. Generate never changes the path's old `Curve3D`, so curves that are shared or saved as files stay as they were.
- Demo: a gallery of every shape, a coaster appended from 15 pieces with a train to ride, and closed outlines in use (`demo/godoocurves/`).
