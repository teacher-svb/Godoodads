---
id: "wfc-1-3-socket-editing-2026-11-10"
status: "done"
priority: "high"
assignee: "claude"
epic: "WFC 1.3"
dueDate: null
created: "2026-11-10T13:00:00.000Z"
modified: "2026-11-10T13:34:44.000Z"
completedAt: "2026-11-10T13:34:44.000Z"
labels: ["editor"]
order: "e0"
---
# WFC 1.3: Socket editing in the preview

Click a face label in the 3D tile set preview to edit the socket in place, with suggestions from the labels used in the tile set (including one-sided partners). Edits go to the tile's own face, also for rotated variants, through undo. Faces no variant can face are drawn in red.

Brief: §13.1.

## Done when
- Engine tests: a rotated-variant edit lands on the right local face; suggestions include partners; unmatched faces are found.
- Manual: edit, undo and red labels in the editor, also after an assembly reload.

## Result
Core `SocketEditing` (local face, label turned back, suggestions with partners, unmatched faces) with 4 Core tests; `WfcTile.SocketProperty`/`GetSocket` with 2 engine tests. The preview: a click (not a drag) on a face label opens a popup with a LineEdit and suggestions; the edit is one undo action on the tile property (in the tile set history), plus EmitChanged and NotifyPropertyListChanged so the inspector refreshes. Red labels for faces no variant can face. Core 199, engine 54 pass. Waiting for the manual editor check.

## Result
Manual check passed (2026-11-10): click opens the editor (not on drag), suggestions and Enter apply, undo restores, an edit at 90° lands on the tile's own face, red labels for unmatched faces (note: a plain side label on a four-way tile is matched by its own 180° rotation, so it is correctly not red), works after a reload.
