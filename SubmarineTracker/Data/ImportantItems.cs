using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace SubmarineTracker.Data;

public enum ImportantItems : uint
{
    Tanks = 10155,
    Kits = 10373,

    // Frames
    SharkClassBoFrame = 26508,
    SharkClassBrFrame = 26509,
    SharkClassPrFrame = 26510,
    SharkClassHuFrame = 26511,

    UnkiuClassBoFrame = 26512,
    UnkiuClassBrFrame = 26513,
    UnkiuClassPrFrame = 26514,
    UnkiuClassHuFrame = 26515,

    WhaleClassBoFrame = 26516,
    WhaleClassBrFrame = 26517,
    WhaleClassPrFrame = 26518,
    WhaleClassHuFrame = 26519,

    CoelacanthClassBoFrame = 26520,
    CoelacanthClassBrFrame = 26521,
    CoelacanthClassPrFrame = 26522,
    CoelacanthClassHuFrame = 26523,

    SyldraClassBoFrame = 26524,
    SyldraClassBrFrame = 26525,
    SyldraClassPrFrame = 26526,
    SyldraClassHuFrame = 26527,

    // Parts
    SharkClassBoPart = 21792,
    SharkClassBrPart = 21793,
    SharkClassPrPart = 21794,
    SharkClassHuPart = 21795,

    UnkiuClassBoPart = 21796,
    UnkiuClassBrPart = 21797,
    UnkiuClassPrPart = 21798,
    UnkiuClassHuPart = 21799,

    WhaleClassBoPart = 22526,
    WhaleClassBrPart = 22527,
    WhaleClassPrPart = 22528,
    WhaleClassHuPart = 22529,

    CoelacanthClassBoPart = 23903,
    CoelacanthClassBrPart = 23904,
    CoelacanthClassPrPart = 23905,
    CoelacanthClassHuPart = 23906,

    SyldraClassBoPart = 24344,
    SyldraClassBrPart = 24345,
    SyldraClassPrPart = 24346,
    SyldraClassHuPart = 24347,

    ModSharkClassBoPart = 24348,
    ModSharkClassBrPart = 24349,
    ModSharkClassPrPart = 24350,
    ModSharkClassHuPart = 24351,

    ModUnkiuClassBoPart = 24352,
    ModUnkiuClassBrPart = 24353,
    ModUnkiuClassPrPart = 24354,
    ModUnkiuClassHuPart = 24355,

    ModWhaleClassBoPart = 24356,
    ModWhaleClassBrPart = 24357,
    ModWhaleClassPrPart = 24358,
    ModWhaleClassHuPart = 24359,

    ModCoelacanthClassBoPart = 24360,
    ModCoelacanthClassBrPart = 24361,
    ModCoelacanthClassPrPart = 24362,
    ModCoelacanthClassHuPart = 24363,

    ModSyldraClassBoPart = 24364,
    ModSyldraClassBrPart = 24365,
    ModSyldraClassPrPart = 24366,
    ModSyldraClassHuPart = 24367,
}

internal static class ImportantItemsMethods
{
    private static ExcelSheet<Item> Item = null!;
    public static void Initialize() => Item = Plugin.Data.GetExcelSheet<Item>()!;

    public static Item GetItem(this ImportantItems item) => Item.GetRow((uint)item)!;
}
