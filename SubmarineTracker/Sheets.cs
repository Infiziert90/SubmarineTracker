using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace SubmarineTracker;

public static class Sheets
{
    public static readonly ExcelSheet<Item> ItemSheet;
    public static readonly ExcelSheet<SubmarineMap> MapSheet;
    public static readonly ExcelSheet<SubmarineRank> RankSheet;
    public static readonly ExcelSheet<SubmarinePart> PartSheet;
    public static readonly ExcelSheet<TerritoryType> TerritorySheet;
    public static readonly ExcelSheet<SubmarineExploration> ExplorationSheet;

    public static readonly uint LastRank;
    public static readonly SubmarineExploration[] PossiblePoints;

    static Sheets()
    {
        ItemSheet = Plugin.Data.GetExcelSheet<Item>();
        MapSheet = Plugin.Data.GetExcelSheet<SubmarineMap>();
        RankSheet = Plugin.Data.GetExcelSheet<SubmarineRank>();
        PartSheet = Plugin.Data.GetExcelSheet<SubmarinePart>();
        TerritorySheet = Plugin.Data.GetExcelSheet<TerritoryType>();
        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExploration>();

        LastRank = RankSheet.Last(t => t.Capacity != 0).RowId;
        PossiblePoints = ExplorationSheet.Where(r => r.ExpReward > 0).ToArray();
    }

    public static SubmarineExploration[] ToExplorationArray(IEnumerable<uint> sectors)
    {
        return sectors.Select(s => ExplorationSheet.GetRow(s)).ToArray();
    }
}
