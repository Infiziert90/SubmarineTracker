namespace SubmarineTracker.Data;

// All this data is taken from:
// https://docs.google.com/spreadsheets/d/1-j0a-I7bQdjnXkplP9T4lOLPH2h3U_-gObxAiI4JtpA
// Credits to Mystic Spirit and other contributors from the submarine discord
public static class LootTable
{
    public record Breakpoints(int T2, int T3, int Normal, int Optimal, int Favor)
    {
        public static Breakpoints Empty => new(0, 0, 0, 0, 0);
    };

    public static readonly Dictionary<uint, Breakpoints> MapBreakpoints = new()
    {
        { 001, new Breakpoints(020, 080, 050, 080, 070) },
        { 002, new Breakpoints(020, 080, 050, 080, 070) },
        { 003, new Breakpoints(020, 085, 055, 085, 070) },
        { 004, new Breakpoints(020, 085, 055, 085, 070) },
        { 005, new Breakpoints(025, 090, 060, 090, 080) },
        { 006, new Breakpoints(025, 090, 060, 090, 080) },
        { 007, new Breakpoints(030, 095, 065, 095, 090) },
        { 008, new Breakpoints(030, 100, 070, 100, 090) },
        { 009, new Breakpoints(035, 110, 075, 105, 090) },
        { 010, new Breakpoints(050, 115, 080, 110, 090) },
        { 011, new Breakpoints(050, 090, 080, 110, 070) },
        { 012, new Breakpoints(055, 095, 090, 120, 080) },
        { 013, new Breakpoints(060, 100, 100, 130, 075) },
        { 014, new Breakpoints(060, 100, 100, 130, 085) },
        { 015, new Breakpoints(080, 115, 120, 160, 090) },
        { 016, new Breakpoints(060, 100, 100, 130, 085) },
        { 017, new Breakpoints(065, 105, 110, 140, 090) },
        { 018, new Breakpoints(085, 120, 135, 175, 095) },
        { 019, new Breakpoints(075, 110, 120, 155, 095) },
        { 020, new Breakpoints(090, 125, 140, 180, 100) },
        { 021, new Breakpoints(090, 120, 135, 175, 095) },
        { 022, new Breakpoints(105, 130, 140, 180, 100) },
        { 023, new Breakpoints(110, 140, 140, 180, 105) },
        { 024, new Breakpoints(120, 130, 145, 190, 105) },
        { 025, new Breakpoints(120, 135, 145, 190, 105) },
        { 026, new Breakpoints(135, 140, 150, 195, 110) },
        { 027, new Breakpoints(130, 145, 150, 195, 110) },
        { 028, new Breakpoints(130, 150, 155, 200, 120) },
        { 029, new Breakpoints(135, 150, 160, 200, 130) },
        { 030, new Breakpoints(140, 155, 170, 215, 135) },

        { 032, new Breakpoints(135, 150, 165, 205, 140) },
        { 033, new Breakpoints(140, 155, 170, 205, 140) },
        { 034, new Breakpoints(140, 160, 175, 210, 145) },
        { 035, new Breakpoints(145, 165, 180, 220, 145) },
        { 036, new Breakpoints(145, 160, 185, 220, 150) },
        { 037, new Breakpoints(145, 165, 180, 220, 145) },
        { 038, new Breakpoints(150, 170, 180, 220, 140) },
        { 039, new Breakpoints(160, 175, 190, 225, 150) },
        { 040, new Breakpoints(155, 170, 190, 220, 140) },
        { 041, new Breakpoints(160, 175, 190, 225, 150) },
        { 042, new Breakpoints(155, 170, 185, 230, 160) },
        { 043, new Breakpoints(160, 175, 185, 235, 165) },
        { 044, new Breakpoints(160, 170, 190, 240, 175) },
        { 045, new Breakpoints(165, 190, 195, 245, 170) },
        { 046, new Breakpoints(170, 185, 205, 250, 175) },
        { 047, new Breakpoints(165, 180, 185, 235, 165) },
        { 048, new Breakpoints(165, 180, 185, 235, 165) },
        { 049, new Breakpoints(170, 185, 190, 240, 165) },
        { 050, new Breakpoints(175, 190, 200, 250, 175) },
        { 051, new Breakpoints(180, 190, 200, 250, 175) },

        { 053, new Breakpoints(180, 190, 200, 250, 175) },
        { 054, new Breakpoints(180, 190, 200, 250, 175) },
        { 055, new Breakpoints(180, 190, 200, 250, 175) },
        { 056, new Breakpoints(180, 195, 205, 260, 178) },
        { 057, new Breakpoints(180, 195, 210, 260, 185) },
        { 058, new Breakpoints(180, 195, 210, 265, 185) },
        { 059, new Breakpoints(180, 195, 215, 270, 185) },
        { 060, new Breakpoints(180, 195, 220, 270, 185) },
        { 061, new Breakpoints(180, 195, 220, 270, 185) },
        { 062, new Breakpoints(180, 195, 220, 270, 185) },
        { 063, new Breakpoints(185, 200, 225, 275, 190) },
        { 064, new Breakpoints(185, 200, 230, 280, 190) },
        { 065, new Breakpoints(185, 200, 230, 280, 190) },
        { 066, new Breakpoints(190, 205, 235, 285, 195) },
        { 067, new Breakpoints(195, 210, 240, 290, 200) },
        { 068, new Breakpoints(195, 210, 245, 295, 200) },
        { 069, new Breakpoints(200, 215, 255, 300, 205) },
        { 070, new Breakpoints(205, 220, 255, 300, 210) },
        { 071, new Breakpoints(205, 220, 260, 305, 210) },
        { 072, new Breakpoints(205, 220, 260, 305, 210) },

        { 074, new Breakpoints(205, 220, 260, 305, 210) },
        { 075, new Breakpoints(205, 220, 260, 305, 210) },
        { 076, new Breakpoints(205, 220, 260, 305, 210) },
        { 077, new Breakpoints(210, 225, 265, 310, 215) },
        { 078, new Breakpoints(210, 225, 265, 310, 215) },
        { 079, new Breakpoints(210, 225, 265, 310, 215) },
        { 080, new Breakpoints(210, 225, 265, 310, 215) },
        { 081, new Breakpoints(215, 230, 270, 315, 220) },
        { 082, new Breakpoints(215, 230, 270, 315, 220) },
        { 083, new Breakpoints(215, 230, 270, 315, 220) },
        { 084, new Breakpoints(215, 230, 270, 315, 220) },
        { 085, new Breakpoints(215, 230, 270, 315, 220) },
        { 086, new Breakpoints(215, 230, 270, 315, 220) },
        { 087, new Breakpoints(220, 235, 275, 320, 225) },
        { 088, new Breakpoints(220, 235, 275, 320, 225) },
        { 089, new Breakpoints(220, 235, 275, 320, 225) },
        { 090, new Breakpoints(220, 235, 275, 320, 225) },
        { 091, new Breakpoints(220, 235, 275, 320, 225) },
        { 092, new Breakpoints(220, 235, 275, 320, 225) },
        { 093, new Breakpoints(220, 235, 275, 320, 225) },
    };

    public static Breakpoints CalculateBreakpoints(List<uint> points)
    {
        // more than 5 points isn't allowed ingame
        if (points.Count is 0 or > 5)
            return Breakpoints.Empty;

        var breakpoints = new List<Breakpoints>();
        foreach (var point in points)
        {
            if (!MapBreakpoints.TryGetValue(point, out var br))
                return Breakpoints.Empty;
            breakpoints.Add(br);
        }

        // every map can have different max, so we have to check every single one
        var t2 = breakpoints.Max(b => b.T2);
        var t3 = breakpoints.Max(b => b.T3);
        var normal = breakpoints.Max(b => b.Normal);
        var optimal = breakpoints.Max(b => b.Optimal);
        var favor = breakpoints.Max(b => b.Favor);

        return new Breakpoints(t2, t3, normal, optimal, favor);
    }
}
