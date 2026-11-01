# Conventions

These rules apply to every addon in the Godot Editor Tools suite.

## Engine and language

- Godot **4.7+ .NET**, C# targeting `net8.0`. One Godot project (`Godoodads.csproj`) serves as the demo and test project for all addons.
- Wrap editor-only code (anything that touches `EditorPlugin`, `EditorInterface` or editor controls) in `#if TOOLS`.
- Godot classes are `partial`. Classes that run inside the editor carry `[Tool]`. A script that is referenced by path (`plugin.cfg`, `.tres`) has a file name that matches its class name.

### Surviving assembly reloads

The editor reloads the C# assembly after every rebuild. Afterwards Godot recreates script instances with their **parameterless constructor**, and it restores only fields of Variant-compatible types and signal connections whose delegate target is a GodotObject. So in editor code:

- Give every `[Tool]` GodotObject class a parameterless constructor.
- Do not keep editor state in plain C# classes. Make them `[Tool]` GodotObjects (for example `RefCounted`), and use Godot collections (`Godot.Collections.Dictionary/Array`) instead of `List`/`Dictionary` for state that must survive.
- Use `[Signal]` instead of C# `event`s on Godot classes.
- When connecting to editor-owned objects (built-in menus, editor singletons), use method-name Callables: `obj.Connect(signal, new Callable(this, MethodName.X))`.
- No lambdas in signal connections or `Callable.From` that outlive a reload: compiler-generated lambda names change between builds. Use named methods. For per-item actions, store the data in fields of a small `[Tool]` control and override a virtual such as `_Pressed` (see `ToolRowButton`).
- Clear references to other GodotObjects when an editor helper is removed. It may stay alive until garbage collection, and the next reload serializes it.

## Addon layout

```
addons/<addon>/
  plugin.cfg
  README.md, CHANGELOG.md
  Core/        Pure logic. No engine objects: only GodotSharp value types (Vector3, Basis, Quaternion, ...)
               and plain C#. Unit tested without the engine.
  Resources/   Engine-side data types (settings Resources, presets). Not editor-only.
  Adapters/    Engine glue between Core and Godot objects (Mesh -> SurfaceData, MultiMesh writer). Not editor-only.
  Editor/      EditorPlugin, dialogs, menu integration. Everything inside #if TOOLS.
addons/editor_tools_common/
  Editor/      Shared editor UI: ToolDialog, SpatialMenuHook, NodePickerRow, Notify, UndoHelpers.
               This folder is a code library, not a plugin, and has no plugin.cfg.
```

Namespaces: `Godoodads.Common` for shared code, `Godoodads.<Addon>` for resources, `Godoodads.<Addon>.Core` for pure logic, `Godoodads.<Addon>.Adapters` for engine glue, and `Godoodads.<Addon>.Editor` for editor code.

## Editor UI pattern

The UX follows Godot's built-in *MultiMesh > Populate Surface*:

1. **Menu entry in the 3D viewport toolbar.** `SpatialMenuHook` adds items to the matching built-in menu button, found by its editor icon. If that button is missing, it adds a separate button and the plugin shows or hides it from `_MakeVisible`.
2. **Popup dialog** (`ToolDialog`). It holds node pickers in its header, an embedded `EditorInspector` for the settings and a status area. It stays open after the action and after errors.
3. **Settings are a `Resource`** with `[Export]` properties. `[ExportGroup]` defines the collapsible sections, `PropertyHint.Range` the slider and spinbox, and `_ValidateProperty` hides options that do not apply. Do not hand-build controls for things the inspector already renders.
4. **Node selection** uses `NodePickerRow`, which wraps `EditorInterface.PopupNodeSelector` with type filtering. Small related actions are `[ExportToolButton]`s grouped into one row with `ToolButtonRowInspectorPlugin` (registered by the plugin).
5. **Only the tool's settings appear in the dialog.** `DialogInspectorFilterPlugin` hides script and resource rows for the edited object, and `ToolDialog` hides inherited class sections and Add Metadata. Give enums readable labels through `_ValidateProperty` hint strings.
6. **Every change to the scene is undoable**, through `EditorUndoRedoManager` (see `UndoHelpers`).
7. **Errors and warnings** go to the dialog status area and the editor toaster (`Notify`), never only to the console.
8. **Never modify user assets.** Work on copies, for example for tangent generation or decompressed textures.

## Testing

| Kind | Where | Command |
|---|---|---|
| Core unit tests (no engine) | `tests/Core.Tests/` (compiles `addons/*/Core/**`) | `dotnet test tests/Core.Tests` |
| Engine tests (gdUnit4, headless Godot) | `tests/<addon>/` (compiled into the Godot project) | `dotnet test Godoodads.csproj --settings .runsettings` |

- Run a single test with `--filter "FullyQualifiedName~<name>"`.
- `.runsettings` points `GODOT_BIN` at the Godot 4.7 .NET binary. It is machine-specific and git-ignored: create it once with `cp .runsettings.example .runsettings` and set your own path.
- Headless runs use a dummy rendering server. `MultiMesh.GetInstanceTransform` returns zeros there, so assert on `MultiMesh.Buffer`. Mesh surface arrays and `Image` work normally.
- Put math in Core so it can be tested quickly. Use engine tests only for code that has to touch engine objects.
- UI is checked by hand in the editor. Record the results on the card or the milestone review card.

## Documentation

- Each addon has a `README.md` (usage, options, screenshots) and a `CHANGELOG.md` ([Keep a Changelog](https://keepachangelog.com) format, semantic versioning). The version in `plugin.cfg` matches the latest CHANGELOG entry.

## Work tracking

- Work is planned and tracked as kanban cards in `.devtool/features/`, using the VS Code extension *Kanban Markdown* (`lachyfs.kanban-markdown`).
- One card is one small, self-contained ticket. The `epic` field is the milestone, for example `"M1 Parity port"`.
- Columns are `backlog`, `todo`, `in-progress`, `review` and `done`. Cards with status `done` live in `.devtool/features/done/`.
- Update `status` and `modified` when work starts and ends. Set `completedAt` when a card is done. Add results and findings to the card body.
- Cards that need a manual check in the editor go to `review`. The board moves milestone review cards to `done`.
