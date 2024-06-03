using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace SubmarineTracker;

public static class Sheets
{
    public static readonly ExcelSheet<Item> ItemSheet;
    public static readonly ExcelSheet<SubmarineRank> RankSheet;
    public static readonly ExcelSheet<SubmarinePart> PartSheet;
    public static readonly ExcelSheet<TerritoryType> TerritorySheet;
    public static readonly ExcelSheet<SubExplPretty> ExplorationSheet;

    public static readonly uint LastRank;
    public static readonly SubExplPretty[] PossiblePoints;

    static Sheets()
    {
        ItemSheet = Plugin.Data.GetExcelSheet<Item>()!;
        RankSheet = Plugin.Data.GetExcelSheet<SubmarineRank>()!;
        PartSheet = Plugin.Data.GetExcelSheet<SubmarinePart>()!;
        TerritorySheet = Plugin.Data.GetExcelSheet<TerritoryType>()!;
        ExplorationSheet = Plugin.Data.GetExcelSheet<SubExplPretty>()!;

        LastRank = RankSheet.Last(t => t.Capacity != 0).RowId;
        PossiblePoints = ExplorationSheet.Where(r => r.ExpReward > 0).ToArray();
    }

    public static SubExplPretty[] ToExplorationArray(IEnumerable<uint> sectors)
    {
        return sectors.Select(s => ExplorationSheet.GetRow(s)!).ToArray();
    }
}
