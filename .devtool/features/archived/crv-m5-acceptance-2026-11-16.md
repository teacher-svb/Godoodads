---
id: "crv-m5-acceptance-2026-11-16"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T14:51:48.000Z"
modified: "2026-11-16T14:53:37.000Z"
completedAt: "2026-11-16T14:53:37.000Z"
labels: ["v1.0.0", "m5", "acceptance"]
order: "f1"
---
# CRV M5: Acceptance pass

Go through brief §8 and record, for each criterion, the test or the board check that covers it, with a final run of every suite. Add a test for any criterion that is not covered yet.

## Done when
- A table on this card: criterion, evidence, result.

## Result (2026-11-16)

| Brief §8 criterion | Evidence | Result |
|---|---|---|
| **Accuracy:** every shape within 0.1 % of its size of the exact shape | `CurveShapeTests.Every_shape_stays_within_its_accuracy`: 36 cases over all open and curved shapes, against exact formulas written separately in the test, measured both ways. `Outlines_follow_their_rounded_polygon`: 6 rectangles and polygons against the ideal rounded outline, plus the perimeter. | Pass. Worst deviation 0.043 %. |
| **Joins:** Append keeps the position exactly, the direction within 0.01°, and the up and tilt | `CurveWriterTests.Append_joins_smoothly_and_leaves_the_original_alone`, `Append_carries_the_end_up_over`. `CurveShapeTests.Every_shape_starts_along_the_direction` covers all 36 cases (Parabolic and Helix at their slope, by board decision). `A_shifted_loop_enters_and_leaves_along_the_direction`. Board: the coaster rides smoothly (M4). | Pass |
| **Exact handles:** a quarter arc has handles of 0.5523·r | `CurveSegmentTests.A_quarter_arc_has_the_exact_handles` | Pass |
| **Closed shapes:** closed, with first and last point agreeing | `CurveSegmentTests.A_full_circle_closes_onto_its_first_point`, `CurveShapeTests.The_circle_is_closed_and_the_others_open` (all 16 kinds against `IsClosed`), `Outlines_follow_their_rounded_polygon`, `An_ellipse_with_equal_radii_is_the_circle`, `CurveWriterTests.Points_handles_and_the_closed_flag_are_written_exactly` | Pass |
| **User assets:** the path's original `Curve3D` is unchanged after Replace and Append | Replace always writes a new curve (`CurveWriter.Create`), and Append works on a duplicate: `Append_joins_smoothly_and_leaves_the_original_alone` checks every original point and handle. Board, M3 check 4: a curve shared by two paths stays unchanged on the other path. | Pass |
| **Settings:** stored settings survive a scene save and reload, and reopening the dialog shows them | `CurveSettingsTests.Settings_stored_on_a_path_survive_a_save_and_reload_of_the_scene`. Board: M3 checks 1 and 2, and M4 check 3 (the coaster opens on its last piece). | Pass |
| **Undo** (manual): undo restores the previous curve and settings, redo reapplies | Board, M3 check 2 | Pass |
| **Tilt** (manual): a `PathFollow3D` runs through the demo loop and corkscrew without flipping | Board, M2 round 2 and M4. Measured headless: in rotation mode XYZ the cart's up matches the curve's up within 0.0° at 40 places round the lap. Oriented mode is documented as unsuitable (brief §6.6, README). | Pass |

Final run: Core 501 tests (135 for godoocurves), engine 127 (16 for godoocurves), build without warnings or errors, editor loads headless cleanly. No criterion needed a new test.
