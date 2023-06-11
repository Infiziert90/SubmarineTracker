using System.Runtime.InteropServices;

namespace SubmarineTracker.Data;

public struct Gathered
{
    [StructLayout(LayoutKind.Explicit, Size = 56)]
    public struct GatheredData
    {
        [FieldOffset(0)] public byte Point;
        [FieldOffset(8)] public byte FavorProc;
        [FieldOffset(12)] public uint ExpGained;

        [FieldOffset(16)] public uint ItemIdPrimary;
        [FieldOffset(20)] public uint ItemIdAdditional;
        [FieldOffset(24)] public ushort ItemCountPrimary;
        [FieldOffset(26)] public ushort ItemCountAdditional;
        [FieldOffset(28)] public bool ItemHQPrimary;
        [FieldOffset(29)] public bool ItemHQAdditional;

        [FieldOffset(32)] public uint PrimarySurvProc;
        [FieldOffset(36)] public uint AdditionalSurvProc;

        [FieldOffset(40)] public uint PrimaryRetProc;
        [FieldOffset(44)] public uint AdditionalRetProc;

        [FieldOffset(48)] public uint PrimaryLootProc;
        [FieldOffset(52)] public uint AdditionalLootProc;
    }
}
