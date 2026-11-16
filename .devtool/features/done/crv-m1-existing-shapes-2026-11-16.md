---
id: "crv-m1-existing-shapes-2026-11-16"
status: "done"
priority: "high"
assignee: "claude"
epic: "godoocurves"
dueDate: null
created: "2026-11-16T12:50:52.000Z"
modified: "2026-11-16T12:57:52.000Z"
completedAt: "2026-11-16T12:57:52.000Z"
labels: ["v1.0.0", "m1", "core"]
order: "b2"
---
# CRV M1: The seven existing shapes

Straight, Parabolic, Circle, Semicircle, Loop, Wave and Spiral as defined in brief §4.1, built on `crv-m1-frame-segments`, with the wanted up per point (§4.4).

- Parabolic: exact (quadratic raised to cubic). Circle and Semicircle: exact arcs, Circle closed. Loop: Length from start to end, the loop in the middle, Side Shift as axis drift; wanted up faces the loop's centre inside it and is the frame's up on the run-in and run-out. Wave: 1 − cos crests of height Amplitude, level start and end. Spiral: helix around the forward axis with radius easing in over the first half turn and out over the last.
- Every shape starts heading along Direction (Parabolic excepted, board decision 2026-11-16: it leaves at its slope); Loop, Wave and Spiral also end along it.
- Parameter checks with messages (radius > 0, cycles ≥ 1, turns > 0, length > 0).

## Done when
- Core tests per shape: within 0.1 % of its size of the exact shape (dense sampling), for the defaults and a spread of settings; start (and where stated, end) tangent along Direction within 0.01°; the closed Circle closes; Loop's wanted up faces its centre; a Side Shift of 0 and of 2 m both work.
- Point counts stay small (reported on the card).

## Result
`Core/Shapes.cs`: `ShapeKind`, `ShapeParameters` (BezierPathGenerator's defaults), `ShapeResult`, and `Shapes.Build`, `Size`, `Check`. Arcs are exact (`Arcs.Add`); Wave and Spiral are fitted to half the 0.1 % accuracy. Wanted up: the frame's up, turned with the arc for Circle and Semicircle (so vertical arcs keep it across the track), toward the centre inside the Loop. Under one turn, Spiral's two eases each cover half the spiral, so its radius stays smooth (a 0.75-turn spiral went from 200 to 11 points).

Accuracy against exact formulas written separately in the test (largest distance either way, as a share of the shape's size; limit 0.1 %):

| Case | Points | Deviation |
|---|---|---|
| Straight 10 / 250 | 2 | 0 % |
| Parabolic 10×5 / 30×−2 | 2 | 0.031 % / 0.004 % |
| Circle r 5 / r 0.5 Bend Up | 4 | 0.014 % |
| Semicircle r 5 / r 40 Left | 3 | 0.014 % |
| Loop 10×6 / 20×8 shift 2 / 4×10 shift −1 | 7 | 0.008 / 0.013 / 0.018 % |
| Wave 10, A 3, 2 cycles / 40, A 1, 5 cycles Up / 6, A 8, 1 cycle Left | 13 / 11 / 9 | 0.031 / 0.024 / 0.027 % |
| Spiral 10, r 5, 2 turns / 30, r 4, 5 turns / 5, r 1, 0.75 turns | 25 / 51 / 11 | 0.043 / 0.023 / 0.019 % |

The Parabolic and Circle figures are the exact formula's sampling, not the curve's error. Start tangents within 0.01° of Direction (Parabolic: of its slope), ends of Loop, Wave, Spiral and Straight along Direction, Circle closed, Loop's wanted up toward its centre with Side Shift 0 and 2, vertical arcs turn their up, impossible settings give messages. 436 Core tests pass.
