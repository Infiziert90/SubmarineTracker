using Lumina.Data;
using Lumina.Excel.GeneratedSheets;
using Lumina.Excel;
using Lumina;

namespace SubmarineTracker;

public class SubmarineExplorationPretty : SubmarineExploration
{
    public Vector3 Position;

    public override void PopulateData( RowParser parser, GameData gameData, Language language )
    {
        base.PopulateData( parser, gameData, language );
        Position = new Vector3( X, Y, Z );
    }

    public uint GetSurveyTime(float speed)
    {
        return (uint)Math.Floor(SurveyDurationmin * 7000 / (speed * 100) * 60);
    }

    public uint GetVoyageTime(SubmarineExplorationPretty other, float speed)
    {
        return (uint)Math.Floor(Vector3.Distance( Position, other.Position ) * 3990 / (speed * 100) * 60);
    }

    public uint GetDistance( SubmarineExplorationPretty other )
    {
        return (uint)Math.Floor( Vector3.Distance( Position, other.Position ) * 0.035 );
    }
}
