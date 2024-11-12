using Lumina.Excel.Sheets;

namespace SubmarineTracker;

public static class SubmarineExplorationExtensions
{
    public static string ToName(this SubmarineExploration sheet) =>
        Utils.UpperCaseStr(sheet.Destination);

    public static uint GetSurveyTime(this SubmarineExploration sheet, float speed)
    {
        if (speed < 1)
            speed = 1;

        return (uint) Math.Floor(sheet.SurveyDurationmin * 7000 / (speed * 100) * 60);
    }

    public static uint GetVoyageTime(this SubmarineExploration sheet, SubmarineExploration otherSheet, float speed)
    {
        if (speed < 1)
            speed = 1;

        return (uint) Math.Floor(Vector3.Distance(new Vector3(sheet.X, sheet.Y, sheet.Z), new Vector3(otherSheet.X, otherSheet.Y, otherSheet.Z)) * 3990 / (speed * 100) * 60);
    }

    public static uint GetDistance(this SubmarineExploration sheet, SubmarineExploration otherSheet) =>
        (uint)Math.Floor(Vector3.Distance(new Vector3(sheet.X, sheet.Y, sheet.Z), new Vector3(otherSheet.X, otherSheet.Y, otherSheet.Z)) * 0.035);

    public static uint CalcTime(this SubmarineExploration sheet, SubmarineExploration otherSheet, float speed) =>
        GetVoyageTime(sheet, otherSheet, speed) + otherSheet.GetSurveyTime(speed);
}
