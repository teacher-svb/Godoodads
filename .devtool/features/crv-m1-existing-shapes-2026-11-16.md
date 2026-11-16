---
id: "crv-m1-existing-shapes-2026-11-16"
status: "todo"
priority: "high"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T12:50:52.000Z"
modified: "2026-11-16T12:50:52.000Z"
completedAt: null
labels: ["v1.0.0", "m1", "core"]
order: "b2"
---
# CRV M1: The seven existing shapes

Straight, Parabolic, Circle, Semicircle, Loop, Wave and Spiral as defined in brief §4.1, built on `crv-m1-frame-segments`, with the wanted up per point (§4.4).

- Parabolic: exact (quadratic raised to cubic). Circle and Semicircle: exact arcs, Circle closed. Loop: Length from start to end, the loop in the middle, Side Shift as axis drift; wanted up faces the loop's centre inside it and is the frame's up on the run-in and run-out. Wave: 1 − cos crests of height Amplitude, level start and end. Spiral: helix around the forward axis with radius easing in over the first half turn and out over the last.
- Every shape starts heading along Direction; Loop, Wave and Spiral also end along it.
- Parameter checks with messages (radius > 0, cycles ≥ 1, turns > 0, length > 0).

## Done when
- Core tests per shape: within 0.1 % of its size of the exact shape (dense sampling), for the defaults and a spread of settings; start (and where stated, end) tangent along Direction within 0.01°; the closed Circle closes; Loop's wanted up faces its centre; a Side Shift of 0 and of 2 m both work.
- Point counts stay small (reported on the card).
