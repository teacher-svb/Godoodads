---
id: "m3-mask-filter-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M3 Mask texture"
dueDate: null
created: "2026-11-01T01:07:27.000Z"
modified: "2026-11-01T01:08:28.000Z"
completedAt: "2026-11-01T01:08:28.000Z"
labels: ["core"]
order: "a0"
---
# M3: Mask image and mask filter (Core)

Brief §4.3.
- `Core/MaskImage.cs`: a single float channel (width × height), bilinear sampling at UV with pixel centres at (i + 0.5) / size and repeat wrapping. Godot's UV origin is the top-left, as in images.
- `Core/MaskFilter.cs`: an `ICandidateFilter` named "mask". Options: UV set (UV1/UV2), mode **Threshold** (accept value >= threshold) or **Density** (accept with probability = value), and invert.
- Runs after the slope filter (which works through triangle weights), as in §4.6. Rejected attempts are retried up to the existing attempt limit (Amount × 20 per needed candidate), and the shortfall is reported.
- No triangle pre-weighting by average mask: combined with rejection it would make density follow mask², which breaks Density mode.

**Tests (§8)**
- Checkerboard mask on a plane in Threshold mode places 0 instances on black squares.
- A 50% grey mask in Density mode accepts 50% ± 3% of attempts over 10,000 samples.
- Bilinear sampling and wrapping are correct.
- Invert works.
- Determinism.

## Result
`Core/MaskImage.cs` (bilinear sampling, pixel centres, repeat wrapping) and `Core/MaskFilter.cs` ("mask" filter: Threshold/Density, invert, UV1/UV2). Tests in `MaskTests`: checkerboard threshold places 0 on black (2000 instances), 50% grey density accepts 47–53% of 10k attempts, bilinear and wrap, invert, black mask hits the attempt limit (Amount × 20) and counts every rejection, determinism, UV2.
