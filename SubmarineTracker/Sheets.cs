using System.Runtime.CompilerServices;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Extensions;

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

    static Sheets()
    {
        ItemSheet = Plugin.Data.GetExcelSheet<Item>();
        MapSheet = Plugin.Data.GetExcelSheet<SubmarineMap>();
        RankSheet = Plugin.Data.GetExcelSheet<SubmarineRank>();
        PartSheet = Plugin.Data.GetExcelSheet<SubmarinePart>();
        TerritorySheet = Plugin.Data.GetExcelSheet<TerritoryType>();
        ExplorationSheet = Plugin.Data.GetExcelSheet<SubmarineExploration>();

        LastRank = RankSheet.Last(t => t.Capacity != 0).RowId;
    }

    /// <summary>
    /// Grabs an item from the sheet.
    /// </summary>
    /// <param name="itemId">Specific item to get</param>
    /// <returns>Item from the sheet</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Item GetItem(uint itemId) => ItemSheet.GetRow(itemId);
}
