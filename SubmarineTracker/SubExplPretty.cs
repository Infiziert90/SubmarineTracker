using Lumina.Data;
using Lumina.Excel.GeneratedSheets;
using Lumina.Excel;
using Lumina;

namespace SubmarineTracker;

public class SubExplPretty : SubmarineExploration
{
    public Vector3 Position;

    public override void PopulateData( RowParser parser, GameData gameData, Language language )
    {
        base.PopulateData( parser, gameData, language );
        Position = new Vector3( X, Y, Z );
    }

    public uint GetSurveyTime(float speed)
    {
        if (speed < 1)
            speed = 1;

        return (uint) Math.Floor(SurveyDurationmin * 7000 / (speed * 100) * 60);
    }

    public uint GetVoyageTime(SubExplPretty other, float speed)
    {
        if (speed < 1)
            speed = 1;

        return (uint) Math.Floor(Vector3.Distance( Position, other.Position ) * 3990 / (speed * 100) * 60);
    }

    public uint GetDistance(SubExplPretty other)
    {
        return (uint) Math.Floor(Vector3.Distance( Position, other.Position ) * 0.035);
    }

    public string ConvertDestination() => Utils.UpperCaseStr(Destination);

    public uint CalcTime(SubExplPretty other, float speed) => GetVoyageTime(other, speed) + other.GetSurveyTime(speed);
}
