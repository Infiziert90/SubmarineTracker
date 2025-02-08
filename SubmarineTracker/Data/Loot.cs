using SubmarineTracker.Resources;

namespace SubmarineTracker.Data;

public static class Loot
{
    public static string ProcToText(uint proc)
    {
        return proc switch
        {
            // Surveillance Procs
            4 => Language.SurvTermT3High,
            5 => Language.SurvTermT2High,
            6 => Language.SurvTermT1High,
            7 => Language.SurvTermT2Mid,
            8 => Language.SurvTermT1Mid,
            9 => Language.SurvTermT1Low,

            // Retrieval Procs
            14 => Language.RetTermOptimal,
            15 => Language.RetTermNormal,
            16 => Language.RetTermPoor,

            // Favor Procs
            18 => Language.FavorTermYes,
            19 => Language.FavorTermStatsEnoughButFailed,
            20 => Language.FavorTermLow,

            _ => Language.TermUnknown
        };
    }
}
