using Lumina.Excel.Sheets;
using Newtonsoft.Json;

namespace SubmarineTracker.Data;

public static class Build
{
    public struct SubmarineBuild
    {
        public SubmarineRank Bonus;
        public readonly SubmarinePart Hull;
        public readonly SubmarinePart Stern;
        public readonly SubmarinePart Bow;
        public readonly SubmarinePart Bridge;

        public SubmarineBuild(Submarine sub) : this(sub.Rank, sub.Hull, sub.Stern, sub.Bow, sub.Bridge) { }

        public SubmarineBuild(int rank, int hull, int stern, int bow, int bridge) : this()
        {
            Bonus = GetRank(rank);
            Hull = GetPart(hull);
            Stern = GetPart(stern);
            Bow = GetPart(bow);
            Bridge = GetPart(bridge);
        }

        public SubmarineBuild(RouteBuild build) : this()
        {
            Bonus = GetRank(build.Rank);
            Hull = GetPart(build.Hull);
            Stern = GetPart(build.Stern);
            Bow = GetPart(build.Bow);
            Bridge = GetPart(build.Bridge);
        }

        public SubmarineBuild UpdateRank(int rank)
        {
            Bonus = GetRank(rank);

            return this;
        }

        public int Surveillance => Bonus.SurveillanceBonus + Hull.Surveillance + Stern.Surveillance + Bow.Surveillance + Bridge.Surveillance;
        public int Retrieval => Bonus.RetrievalBonus + Hull.Retrieval + Stern.Retrieval + Bow.Retrieval + Bridge.Retrieval;
        public int Speed => Bonus.SpeedBonus + Hull.Speed + Stern.Speed + Bow.Speed + Bridge.Speed;
        public int Range => Bonus.RangeBonus + Hull.Range + Stern.Range + Bow.Range + Bridge.Range;
        public int Favor => Bonus.FavorBonus + Hull.Favor + Stern.Favor + Bow.Favor + Bridge.Favor;
        public int RepairCosts => Hull.RepairMaterials + Stern.RepairMaterials + Bow.RepairMaterials + Bridge.RepairMaterials;
        public int BuildCost => Hull.Components + Stern.Components + Bow.Components + Bridge.Components;

        public int HighestRankPart() => new[] { Hull.Rank, Stern.Rank, Bow.Rank, Bridge.Rank }.Max();
        public byte[] GetPartRanks() => new[] { Hull.Rank, Stern.Rank, Bow.Rank, Bridge.Rank };

        private SubmarineRank GetRank(int rank) => Sheets.RankSheet.GetRow((uint)rank)!;
        private SubmarinePart GetPart(int partId) => Sheets.PartSheet.GetRow((uint)partId)!;

        public string HullIdentifier => ToIdentifier((ushort)Hull.RowId);
        public string SternIdentifier => ToIdentifier((ushort)Stern.RowId);
        public string BowIdentifier => ToIdentifier((ushort)Bow.RowId);
        public string BridgeIdentifier => ToIdentifier((ushort)Bridge.RowId);

        public string FullIdentifier()
        {
            var identifier = $"{HullIdentifier}{SternIdentifier}{BowIdentifier}{BridgeIdentifier}";

            if (identifier.Count(l => l == '+') == 4)
                identifier = $"{identifier.Replace("+", "")}++";

            return identifier;
        }

        public bool EqualsSubmarine(Submarine other)
        {
            return Bonus.RowId == other.Rank && Hull.RowId == other.Hull && Stern.RowId == other.Stern && Bow.RowId == other.Bow && Bridge.RowId == other.Bridge;
        }

        public static implicit operator SubmarineBuild(RouteBuild build) => build.GetSubmarineBuild;
    }

    public struct RouteBuild : IEquatable<RouteBuild>
    {
        public int Rank = 1;
        public int Hull = 3;
        public int Stern = 4;
        public int Bow = 1;
        public int Bridge = 2;

        public int Map = 0;
        public List<uint> Sectors = new();

        public RouteBuild() { }

        public RouteBuild(RouteBuild build)
        {
            Rank = build.Rank;
            Hull = build.Hull;
            Stern = build.Stern;
            Bow = build.Bow;
            Bridge = build.Bridge;
        }

        public RouteBuild(int rank, int hull, int stern, int bow, int bridge)
        {
            Rank = rank;
            Hull = hull;
            Stern = stern;
            Bow = bow;
            Bridge = bridge;
        }

        public RouteBuild(int rank, RouteBuild prevBuild)
        {
            Rank = rank;
            Hull = prevBuild.Hull;
            Stern = prevBuild.Stern;
            Bow = prevBuild.Bow;
            Bridge = prevBuild.Bridge;
        }

        public RouteBuild(Items hull, Items stern, Items bow, Items bridge)
        {
            Rank = 1;
            Hull = hull.GetPartId();
            Stern = stern.GetPartId();
            Bow = bow.GetPartId();
            Bridge = bridge.GetPartId();
        }

        public RouteBuild(Submarine sub)
        {
            Rank = sub.Rank;
            Hull = sub.Hull;
            Stern = sub.Stern;
            Bow = sub.Bow;
            Bridge = sub.Bridge;
        }

        [JsonIgnore] public int OriginalSub = 0;

        [JsonIgnore] public uint OptimizedDistance = 0;
        [JsonIgnore] public SubmarineExploration[] OptimizedRoute = [];
        [JsonIgnore] public SubmarineBuild GetSubmarineBuild => new(this);
        [JsonIgnore] public static RouteBuild Empty => new();

        [JsonIgnore] public int FuelCost => OptimizedRoute.Length != 0 ? OptimizedRoute.Select(p => (int) p.CeruleumTankReq).Sum() : 0;

        [JsonIgnore] public string HullIdentifier => ToIdentifier((ushort)Hull);
        [JsonIgnore] public string SternIdentifier => ToIdentifier((ushort)Stern);
        [JsonIgnore] public string BowIdentifier => ToIdentifier((ushort)Bow);
        [JsonIgnore] public string BridgeIdentifier => ToIdentifier((ushort)Bridge);

        [JsonIgnore] private int[] PartArray => [Bow, Bridge, Hull, Stern];

        public void UpdateBuild(Submarine sub)
        {
            Rank = sub.Rank;
            Hull = sub.Hull;
            Stern = sub.Stern;
            Bow = sub.Bow;
            Bridge = sub.Bridge;
        }

        public void UpdateBuild(SubmarineBuild build, int currentRank)
        {
            Rank = currentRank;
            Hull = (int)build.Hull.RowId;
            Stern = (int)build.Stern.RowId;
            Bow = (int)build.Bow.RowId;
            Bridge = (int)build.Bridge.RowId;
        }

        public void ChangeMap(int newMap)
        {
            Map = newMap;

            Sectors.Clear();
            OptimizedRoute = [];
            OptimizedDistance = 0;
        }

        public void UpdateOptimized(Voyage.BestRoute route)
        {
            Sectors = route.Path.ToList();

            OptimizedRoute = route.PathPretty;
            OptimizedDistance = route.Distance;
        }

        public void NotOptimized()
        {
            Sectors = [];

            OptimizedRoute = [];
            OptimizedDistance = 0;
        }

        public int CalculateUntilRepair()
        {
            var dmg = VoyageDamage();
            if (dmg == 1)
                return -1;

            var voyages = 0;
            var health = 30000;
            while (health > 0)
            {
                voyages += 1;
                health -= dmg;
            }

            return voyages;
        }

        public int VoyageDamage()
        {
            var highestDamage = 1;
            foreach (var part in PartArray)
            {
                var damaged = 0;
                foreach (var sector in OptimizedRoute)
                    damaged += (335 + sector.RankReq - Sheets.PartSheet.GetRow((uint) part)!.Rank) * 7;

                if (highestDamage < damaged)
                    highestDamage = damaged;
            }

            return highestDamage;
        }

        public bool SameBuild(RouteBuild other) => Rank == other.Rank && SameBuildWithoutRank(other);
        public bool SameBuildWithoutRank(RouteBuild other) => Hull == other.Hull && Stern == other.Stern && Bow == other.Bow && Bridge == other.Bridge;

        public override string ToString()
        {
            var identifier = $"{HullIdentifier}{SternIdentifier}{BowIdentifier}{BridgeIdentifier}";

            if (identifier.Count(l => l == '+') == 4)
                identifier = $"{identifier.Replace("+", "")}++";

            return identifier;
        }

        public static explicit operator RouteBuild(string s)
        {
            if (s.Length < 4)
                return new RouteBuild();

            var allMod = s.EndsWith("++");

            var parts = new List<string>();
            for (var i = 0; i < s.Length; i++)
            {
                var part = s[i].ToString();
                if (part == "+")
                    continue;

                if ((i + 1 < s.Length && s[i + 1] == '+') || allMod)
                    part += "+";

                parts.Add(part);
            }

            return new RouteBuild
            {
                Hull = FromIdentifier(parts[0]) + 3,
                Stern = FromIdentifier(parts[1]) + 4,
                Bow = FromIdentifier(parts[2]) + 1,
                Bridge = FromIdentifier(parts[3]) + 2
            };
        }

        public static explicit operator string(RouteBuild build) => build.ToString();

        public override bool Equals(object? obj)
        {
            if (obj is RouteBuild other)
                return other.SameBuildWithoutRank(this);
            return false;
        }

        public bool Equals(RouteBuild other)
        {
            return other.SameBuildWithoutRank(this);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Rank);
            hashCode.Add(Hull);
            hashCode.Add(Stern);
            hashCode.Add(Bow);
            hashCode.Add(Bridge);
            hashCode.Add(Map);
            hashCode.Add(Sectors);
            hashCode.Add(OriginalSub);
            hashCode.Add(OptimizedDistance);
            hashCode.Add(OptimizedRoute);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(RouteBuild left, RouteBuild right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RouteBuild left, RouteBuild right)
        {
            return !(left == right);
        }

        public bool IsSubComponent(RouteBuild other)
        {
            var curParts = ToString().Replace("+", "").ToCharArray();
            var otherParts = other.ToString().Replace("+", "").ToCharArray();

            for (var i = 0; i < curParts.Length; i++)
            {
                if (curParts[i] != otherParts[i] && curParts[i] != 'S')
                    return false;
            }

            return true;
        }

        public bool IsValidSubBuild(RouteBuild original, bool ignoreShark, bool ignoreUnmodded)
        {
            for (var i = 0; i < 4; i++)
            {
                var cur = PartArray[i];
                var org = original.PartArray[i];

                // current is shark part and always valid
                if (!ignoreShark && cur <= 3)
                    continue;

                // current is same
                if (cur == org)
                    continue;

                // original is modded, so we try unmodded part
                if (!ignoreUnmodded && org > 20 && cur == org - 20)
                    continue;

                return false;
            }

            return true;
        }
    }

    public static string ToIdentifier(ushort partId)
    {
        return ((partId - 1) / 4) switch
        {
            0 => "S",
            1 => "U",
            2 => "W",
            3 => "C",
            4 => "Y",

            5 => $"{ToIdentifier((ushort)(partId - 20))}+",
            6 => $"{ToIdentifier((ushort)(partId - 20))}+",
            7 => $"{ToIdentifier((ushort)(partId - 20))}+",
            8 => $"{ToIdentifier((ushort)(partId - 20))}+",
            9 => $"{ToIdentifier((ushort)(partId - 20))}+",
            _ => "Unknown"
        };
    }

    public static int FromIdentifier(string s)
    {
        var k = s[0] switch
        {
            'S' => 0,
            'U' => 1,
            'W' => 2,
            'C' => 3,
            'Y' => 4,
            _ => 0
        };

        if (s[^1] == '+')
            k += 5;

        return k * 4;
    }
}
