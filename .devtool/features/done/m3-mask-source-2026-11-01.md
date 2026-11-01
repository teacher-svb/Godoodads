---
id: "m3-mask-source-2026-11-01"
status: "done"
priority: "high"
assignee: "claude"
epic: "M3 Mask texture"
dueDate: null
created: "2026-11-01T01:07:27.000Z"
modified: "2026-11-01T01:09:04.000Z"
completedAt: "2026-11-01T01:09:04.000Z"
labels: ["adapters"]
order: "a1"
---
# M3: Texture to MaskImage (channel, compressed textures)

`Adapters/MaskSource.cs`: `Texture2D.GetImage()` returns a copy; if it is compressed, decompress the copy (never the user's texture). Convert to RGBA8 and extract the channel: **Luminance** (default, Rec. 709 weights on the stored values), R, G, B or A.

Errors are reported to the dialog: the texture has no image data, or it cannot be decompressed.

**Engine tests:** each channel, a compressed image, and that the source image is unchanged.

## Result
`Adapters/MaskSource.cs`: `GetImage()` copy, decompress if needed, convert to RGBA8, extract the channel (Rec. 709 luminance on the stored values). Errors for empty or undecompressable images. Engine tests in `MaskSourceTests`: all channels, L8 input and row order (source not converted), S3TC-compressed image decompressed on a copy.
