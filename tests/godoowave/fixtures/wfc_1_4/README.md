A scene as godoowave 1.4 saved it after **Pin Generated Cells**: the stored `WfcSettings` still has
`PinOrigin` and `PinSize`, and the generated record (`godoodads_wfc_generated`) has the pinned bit
(0x80 of the orientation byte) on cells (0, 0, 0) and (1, 0, 0). Written by hand in the 1.4 format;
`WfcPinBoxTests` checks that 1.5 loads it and treats the old pins as ordinary generated cells (brief §16.4).
