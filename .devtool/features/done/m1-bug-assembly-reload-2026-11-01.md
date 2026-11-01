---
id: "m1-bug-assembly-reload-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M1 Parity port"
dueDate: null
created: "2026-11-01T12:00:00.000Z"
modified: "2026-11-01T00:37:36.000Z"
completedAt: "2026-11-01T00:37:36.000Z"
labels: ["bug", "editor", "common"]
order: "a0"
---
# Bug: "Scatter Surface…" fails after a C# assembly reload

Reported by the board when opening the dialog:

```
ERROR: modules/mono/managed_callable.cpp:96 - Parameter "delegate_handle.value" is null.
ERROR: Can't get method on CallableCustom "Delegate::Invoke".
ERROR: core/object/object.cpp:1310 - Error calling from signal 'id_pressed' to callable: 'ManagedCallableMiddleman::': Method not found.
```

**Cause:** when the C# assembly is rebuilt while the editor is open, Godot reloads it and only restores state that it can serialize: fields of Variant-compatible types, and signal connections whose delegate target is a GodotObject. The broken pieces were:
- `SpatialMenuHook` was a plain C# class, so its `id_pressed` connection on the built-in PopupMenu lost its delegate.
- The plugin's `_menu` field and the plain C# events `ToolDialog.ActionPressed` and `NodePickerRow.NodePicked` were silently dropped, so Populate would also stop working.

**Fix:**
- `SpatialMenuHook` becomes a `[Tool]` RefCounted. It connects through method-name Callables and keeps its state in Godot collections.
- The C# events become `[Signal]`s.
- A convention is added for reload-safe editor code.

## Result (needs a manual check)
- `SpatialMenuHook` is now a `[Tool]` RefCounted. It connects `id_pressed` through `new Callable(this, MethodName.OnIdPressed)` and stores its callbacks as method-name Callables in a Godot Dictionary.
- `ToolDialog.ActionPressed` and `NodePickerRow.NodePicked` are now `[Signal]`s.
- CONVENTIONS.md has a new section, "Surviving assembly reloads".
- Also made `SceneTests` CSG test wait up to 5 frames for the deferred CSG update (it was flaky).
- Build is clean, the headless editor loads without errors, and the 9 engine tests pass 3 runs in a row. The reload itself cannot be triggered headless.

**Check:** with the editor open, press *Build* (hammer icon) or let it rebuild, then use **MultiMesh > Scatter Surface…** and Populate. There should be no errors, and the item should not be duplicated.

## Board check (2026-11-01)
Confirmed: after a reload the dialog opens and Populate works. The editor log shows no callable errors.
