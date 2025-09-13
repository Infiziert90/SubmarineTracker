using System.Collections;
using System.Data.Common;
using System.IO;
using MessagePack;
using System.Data.SQLite;

namespace SubmarineTracker;

internal static class DbExtensions
{
    internal static void Execute(this DbConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}

public class Database : IDisposable
{
    private string DbPath { get; }

    private SQLiteConnection Connection { get; }

    internal Database()
    {
        try
        {
            DbPath = DatabasePath();
            Connection = Connect();
            Migrate();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Database error");
            Dispose();
            throw new Exception();
        }
    }

    public void Dispose()
    {
        Connection.Close();
        Connection.Dispose();
        // Closing the connection doesn't immediately release the file.
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    public static string DatabasePath()
    {
        return Path.Join(Plugin.PluginInterface.ConfigDirectory.FullName, "submarine-sqlite.db");
    }

    private SQLiteConnection Connect()
    {
        var uriBuilder = new SQLiteConnectionStringBuilder()
        {
            DataSource = DbPath,
            DefaultTimeout = 5,
            Pooling = false,
            ReadOnly = false,

            JournalMode = SQLiteJournalModeEnum.Wal,
            SyncMode = SynchronizationModes.Normal,
            CacheSize = 32768
        };

        var conn = new SQLiteConnection(uriBuilder.ToString());
        conn.Open();

        return conn;
    }

    private void Migrate()
    {
        // Get current user_version.
        var cmd = Connection.CreateCommand();
        cmd.CommandText = "PRAGMA user_version;";
        var userVersion = Convert.ToInt32(cmd.ExecuteScalar());

        var migrationsToDo = new List<Action>();
        switch (userVersion)
        {
            case <= 0:
                migrationsToDo.Add(Migrate0);
                migrationsToDo.Add(Migrate1);
                break;
        }

        foreach (var migration in migrationsToDo)
            migration();
    }

    private void Migrate0()
    {
        Connection.Execute("""
                   CREATE TABLE IF NOT EXISTS freecompany (
                       FreeCompanyId BLOB PRIMARY KEY NOT NULL,            -- MessagePack encoded uint64
                       FreeCompanyTag TEXT NOT NULL,                       -- fc tag
                       World TEXT NOT NULL,                                -- fc world
                       CharacterName TEXT NOT NULL,                        -- last character name
                       UnlockedSectors BLOB NOT NULL,                      -- Dictionary<uint, bool>
                       ExploredSectors BLOB NOT NULL                       -- Dictionary<uint, bool>
                   );
                   """);

        Connection.Execute("""
                   CREATE TABLE IF NOT EXISTS loot (
                       FreeCompanyId BLOB NOT NULL,            -- MessagePack encoded uint64
                       SubmarineId INTEGER NOT NULL,           -- unix timestamp with second precision
                       Return INTEGER NOT NULL,                -- unix timestamp with second precision
                       Sector INTEGER NOT NULL,                -- uint
                       Rank INTEGER NOT NULL,                  -- int32
                       Surv INTEGER NOT NULL,                  -- int32
                       Ret INTEGER NOT NULL,                   -- int32
                       Fav INTEGER NOT NULL,                   -- int32
                       PrimarySurvProc INTEGER NOT NULL,       -- uint32
                       AdditionalSurvProc INTEGER NOT NULL,    -- uint32
                       PrimaryRetProc INTEGER NOT NULL,        -- uint32
                       AdditionalRetProc INTEGER NOT NULL,     -- uint32
                       FavProc INTEGER NOT NULL,               -- uint32
                       PrimaryItem INTEGER NOT NULL,           -- uint32
                       PrimaryCount INTEGER NOT NULL,          -- uint32
                       PrimaryHQ BOOLEAN NOT NULL,             -- Bool
                       AdditionalItem INTEGER NOT NULL,        -- uint32
                       AdditionalCount INTEGER NOT NULL,       -- uint32
                       AdditionalHQ BOOLEAN NOT NULL,          -- Bool
                       Unlocked INTEGER NOT NULL,              -- uint32
                       Date INTEGER NOT NULL,                  -- unix timestamp with second precision
                       Valid BOOLEAN NOT NULL,                  -- Bool

                       FOREIGN KEY (FreeCompanyId) REFERENCES freecompany(FreeCompanyId)
                   );

                   CREATE INDEX IF NOT EXISTS idx_loot_freeCompanyid ON loot (FreeCompanyId);
                   CREATE INDEX IF NOT EXISTS idx_loot_submarineid ON loot (SubmarineId);
                   CREATE INDEX IF NOT EXISTS idx_loot_return ON loot (Return);
                   CREATE INDEX IF NOT EXISTS idx_loot_sector ON loot (Sector);
                   CREATE INDEX IF NOT EXISTS idx_loot_date ON loot (Date);
                   CREATE INDEX IF NOT EXISTS idx_loot_Valid ON loot (Valid);
                   """);

        Connection.Execute("""
                   CREATE TABLE IF NOT EXISTS submarine (
                       FreeCompanyId BLOB NOT NULL,            -- MessagePack encoded uint64
                       SubmarineId INTEGER NOT NULL,           -- unix timestamp with second precision
                       Return INTEGER NOT NULL,                -- unix timestamp with second precision
                       Name TEXT NOT NULL,                     -- submarine name
                       Rank INTEGER NOT NULL,                  -- ushort
                       Route BLOB NOT NULL,                    -- uint array
                       Hull INTEGER NOT NULL,                  -- ushort
                       Stern INTEGER NOT NULL,                 -- ushort
                       Bow INTEGER NOT NULL,                   -- ushort
                       Bridge INTEGER NOT NULL,                -- ushort
                       CExp INTEGER NOT NULL,                  -- uint
                       NExp INTEGER NOT NULL,                  -- uint
                       HullDurability INTEGER NOT NULL,        -- ushort
                       SternDurability INTEGER NOT NULL,       -- ushort
                       BowDurability INTEGER NOT NULL,         -- ushort
                       BridgeDurability INTEGER NOT NULL,       -- ushort

                       PRIMARY KEY (FreeCompanyId, SubmarineId),
                       FOREIGN KEY (FreeCompanyId) REFERENCES freecompany(FreeCompanyId)
                   );

                   CREATE INDEX IF NOT EXISTS idx_submarine_return ON submarine (Return);
                   """);

        SetMigrationVersion(0);
    }

    private void Migrate1()
    {
        Connection.Execute(@"
            -- Migration 1: Add Counters Table
        ");

        var cmd = Connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='counters';";
        var reader = cmd.ExecuteReader();
        reader.Close();
        if (cmd.ExecuteReader().HasRows)
        {
            Plugin.Log.Error("Counters table already exists, corrupted database?");
            SetMigrationVersion(1);

            return;
        }

        Connection.Execute("""
                           CREATE TABLE IF NOT EXISTS counters (
                               Key TEXT NOT NULL,               -- Counter Name
                               Count INTEGER NOT NULL           -- int64
                           );

                           CREATE INDEX IF NOT EXISTS key_index ON counters(Key);

                           INSERT INTO counters (Key, Count) VALUES ('Loot', 0);

                           CREATE TRIGGER increase_loot_counter
                              AFTER INSERT ON loot
                           BEGIN
                              UPDATE counters
                                SET count = count + 1
                              WHERE Key = 'Loot';
                           END;
                           """);

        SetMigrationVersion(1);
    }

    private void SetMigrationVersion(int version)
    {
        var cmd = Connection.CreateCommand();
        // Parameters aren't supported for PRAGMA queries, and you can't set the
        // version with a pragma_ function.
        cmd.CommandText = $"PRAGMA user_version = {version};";
        cmd.ExecuteNonQuery();
    }

    internal void PerformMaintenance()
    {
        Connection.Execute(@"
            VACUUM;
            REINDEX messages;
            ANALYZE;
        ");
    }

    private string LogPath => DbPath + "-wal";
    internal long DatabaseSize() => !File.Exists(DbPath) ? 0 : new FileInfo(DbPath).Length;
    internal long DatabaseLogSize() => !File.Exists(LogPath) ? 0 : new FileInfo(LogPath).Length;

    internal void InsertLootEntry(Loot loot)
    {
        var cmd = Connection.CreateCommand();
        cmd.CommandText = """
                          INSERT INTO loot (
                              FreeCompanyId,
                              SubmarineId,
                              Return,
                              Sector,
                              Rank,
                              Surv,
                              Ret,
                              Fav,
                              PrimarySurvProc,
                              AdditionalSurvProc,
                              PrimaryRetProc,
                              AdditionalRetProc,
                              FavProc,
                              PrimaryItem,
                              PrimaryCount,
                              PrimaryHQ,
                              AdditionalItem,
                              AdditionalCount,
                              AdditionalHQ,
                              Unlocked,
                              Date,
                              Valid
                          ) VALUES (
                              $FreeCompanyId,
                              $SubmarineId,
                              $Return,
                              $Sector,
                              $Rank,
                              $Surv,
                              $Ret,
                              $Fav,
                              $PrimarySurvProc,
                              $AdditionalSurvProc,
                              $PrimaryRetProc,
                              $AdditionalRetProc,
                              $FavProc,
                              $PrimaryItem,
                              $PrimaryCount,
                              $PrimaryHQ,
                              $AdditionalItem,
                              $AdditionalCount,
                              $AdditionalHQ,
                              $Unlocked,
                              $Date,
                              $Valid
                          );
                          """;

        cmd.Parameters.AddWithValue("$FreeCompanyId", MessagePackSerializer.Serialize(loot.FreeCompanyId));
        cmd.Parameters.AddWithValue("$SubmarineId", loot.Register);
        cmd.Parameters.AddWithValue("$Return", loot.Return);
        cmd.Parameters.AddWithValue("$Sector", loot.Sector);
        cmd.Parameters.AddWithValue("$Rank", loot.Rank);
        cmd.Parameters.AddWithValue("$Surv", loot.Surv);
        cmd.Parameters.AddWithValue("$Ret", loot.Ret);
        cmd.Parameters.AddWithValue("$Fav", loot.Fav);
        cmd.Parameters.AddWithValue("$PrimarySurvProc", loot.PrimarySurvProc);
        cmd.Parameters.AddWithValue("$AdditionalSurvProc", loot.AdditionalSurvProc);
        cmd.Parameters.AddWithValue("$PrimaryRetProc", loot.PrimaryRetProc);
        cmd.Parameters.AddWithValue("$AdditionalRetProc", loot.AdditionalRetProc);
        cmd.Parameters.AddWithValue("$FavProc", loot.FavProc);
        cmd.Parameters.AddWithValue("$PrimaryItem", loot.Primary);
        cmd.Parameters.AddWithValue("$PrimaryCount", loot.PrimaryCount);
        cmd.Parameters.AddWithValue("$PrimaryHQ", loot.PrimaryHQ);
        cmd.Parameters.AddWithValue("$AdditionalItem", loot.Additional);
        cmd.Parameters.AddWithValue("$AdditionalCount", loot.AdditionalCount);
        cmd.Parameters.AddWithValue("$AdditionalHQ", loot.AdditionalHQ);
        cmd.Parameters.AddWithValue("$Unlocked", loot.Unlocked);
        cmd.Parameters.AddWithValue("$Date", ((DateTimeOffset)loot.Date).ToUnixTimeSeconds());
        cmd.Parameters.AddWithValue("$Valid", loot.Valid);

        cmd.ExecuteNonQuery();
    }

    internal void UpsertSubmarine(Submarine sub)
    {
        var cmd = Connection.CreateCommand();
        cmd.CommandText = """
                          INSERT INTO submarine (
                              FreeCompanyId,
                              SubmarineId,
                              Return,
                              Name,
                              Rank,
                              Route,
                              Hull,
                              Stern,
                              Bow,
                              Bridge,
                              CExp,
                              NExp,
                              HullDurability,
                              SternDurability,
                              BowDurability,
                              BridgeDurability
                          ) VALUES (
                              $FreeCompanyId,
                              $SubmarineId,
                              $Return,
                              $Name,
                              $Rank,
                              $Route,
                              $Hull,
                              $Stern,
                              $Bow,
                              $Bridge,
                              $CExp,
                              $NExp,
                              $HullDurability,
                              $SternDurability,
                              $BowDurability,
                              $BridgeDurability
                          )
                          ON CONFLICT (FreeCompanyId, SubmarineId) DO UPDATE SET
                              FreeCompanyId = excluded.FreeCompanyId,
                              SubmarineId = excluded.SubmarineId,
                              Return = excluded.Return,
                              Name = excluded.Name,
                              Route = excluded.Route,
                              Rank = excluded.Rank,
                              Hull = excluded.Hull,
                              Stern = excluded.Stern,
                              Bow = excluded.Bow,
                              Bridge = excluded.Bridge,
                              CExp = excluded.CExp,
                              NExp = excluded.NExp,
                              HullDurability = excluded.HullDurability,
                              SternDurability = excluded.SternDurability,
                              BowDurability = excluded.BowDurability,
                              BridgeDurability = excluded.BridgeDurability
                          ;
                          """;

        cmd.Parameters.AddWithValue("$FreeCompanyId", MessagePackSerializer.Serialize(sub.FreeCompanyId));
        cmd.Parameters.AddWithValue("$SubmarineId", sub.Register);
        cmd.Parameters.AddWithValue("$Return", sub.Return);
        cmd.Parameters.AddWithValue("$Name", sub.Name);
        cmd.Parameters.AddWithValue("$Rank", sub.Rank);
        cmd.Parameters.AddWithValue("$Route", MessagePackSerializer.Serialize(sub.Points));
        cmd.Parameters.AddWithValue("$Hull", sub.Hull);
        cmd.Parameters.AddWithValue("$Stern", sub.Stern);
        cmd.Parameters.AddWithValue("$Bow", sub.Bow);
        cmd.Parameters.AddWithValue("$Bridge", sub.Bridge);
        cmd.Parameters.AddWithValue("$CExp", sub.CExp);
        cmd.Parameters.AddWithValue("$NExp", sub.NExp);
        cmd.Parameters.AddWithValue("$HullDurability", sub.HullDurability);
        cmd.Parameters.AddWithValue("$SternDurability", sub.SternDurability);
        cmd.Parameters.AddWithValue("$BowDurability", sub.BowDurability);
        cmd.Parameters.AddWithValue("$BridgeDurability", sub.BridgeDurability);

        cmd.ExecuteNonQuery();
    }

    internal void UpsertFreeCompany(FreeCompany fc)
    {
        var cmd = Connection.CreateCommand();
        cmd.CommandText = """
                          INSERT INTO freecompany (
                              FreeCompanyId,
                              FreeCompanyTag,
                              World,
                              CharacterName,
                              UnlockedSectors,
                              ExploredSectors
                          ) VALUES (
                              $FreeCompanyId,
                              $FreeCompanyTag,
                              $World,
                              $CharacterName,
                              $UnlockedSectors,
                              $ExploredSectors
                          )
                          ON CONFLICT (FreeCompanyId) DO UPDATE SET
                              FreeCompanyId = excluded.FreeCompanyId,
                              FreeCompanyTag = excluded.FreeCompanyTag,
                              World = excluded.World,
                              CharacterName = excluded.CharacterName,
                              UnlockedSectors = excluded.UnlockedSectors,
                              ExploredSectors = excluded.ExploredSectors
                          ;
                          """;

        cmd.Parameters.AddWithValue("$FreeCompanyId", MessagePackSerializer.Serialize(fc.FreeCompanyId));
        cmd.Parameters.AddWithValue("$FreeCompanyTag", fc.Tag);
        cmd.Parameters.AddWithValue("$World", fc.World);
        cmd.Parameters.AddWithValue("$CharacterName", fc.CharacterName);
        cmd.Parameters.AddWithValue("$UnlockedSectors", MessagePackSerializer.Serialize(fc.UnlockedSectors));
        cmd.Parameters.AddWithValue("$ExploredSectors", MessagePackSerializer.Serialize(fc.ExploredSectors));

        cmd.ExecuteNonQuery();
    }

    internal LootReader GetLoot()
    {
        var cmd = Connection.CreateCommand();

        cmd.CommandText = "SELECT * FROM loot";
        cmd.CommandTimeout = 120;

        return new LootReader(cmd.ExecuteReader());
    }

    internal SubmarineReader GetSubmarines()
    {
        var cmd = Connection.CreateCommand();

        cmd.CommandText = "SELECT * FROM submarine";
        cmd.CommandTimeout = 120;

        return new SubmarineReader(cmd.ExecuteReader());
    }

    internal FCReader GetFreeCompanies()
    {
        var cmd = Connection.CreateCommand();

        cmd.CommandText = "SELECT * FROM freecompany";
        cmd.CommandTimeout = 120;

        return new FCReader(cmd.ExecuteReader());
    }

    internal long GetCounter(string counter)
    {
        var cmd = Connection.CreateCommand();

        cmd.CommandText = $"SELECT (count) FROM counters WHERE Key = '{counter}'";
        cmd.CommandTimeout = 120;

        return (long) (cmd.ExecuteScalar() ?? -1);
    }

    internal bool DeleteFreeCompany(ulong fcId)
    {
        var cmd = Connection.CreateCommand();
        cmd.CommandText = """
                          DELETE FROM loot
                          WHERE FreeCompanyId = $FreeCompanyId;

                          DELETE FROM submarine
                          WHERE FreeCompanyId = $FreeCompanyId;

                          DELETE FROM freecompany
                          WHERE FreeCompanyId = $FreeCompanyId;
                          """;

        cmd.Parameters.AddWithValue("$FreeCompanyId", MessagePackSerializer.Serialize(fcId));
        return cmd.ExecuteNonQuery() > 0;
    }
}

internal class LootReader(DbDataReader reader) : IEnumerable<Loot>
{
    public IEnumerator<Loot> GetEnumerator()
    {
        while (reader.Read())
        {
            Loot loot;
            try
            {
                loot = new Loot
                {
                    FreeCompanyId = MessagePackSerializer.Deserialize<ulong>(reader.GetFieldValue<byte[]>(0)),
                    Register = (uint)reader.GetInt32(1),
                    Return = (uint)reader.GetInt32(2),

                    Sector = (uint)reader.GetInt32(3),
                    Rank = reader.GetInt32(4),

                    Surv = reader.GetInt32(5),
                    Ret = reader.GetInt32(6),
                    Fav = reader.GetInt32(7),

                    PrimarySurvProc = (uint)reader.GetInt32(8),
                    AdditionalSurvProc = (uint)reader.GetInt32(9),
                    PrimaryRetProc = (uint)reader.GetInt32(10),
                    AdditionalRetProc = (uint)reader.GetInt32(11),
                    FavProc = (uint)reader.GetInt32(12),

                    Primary = (uint)reader.GetInt32(13),
                    PrimaryCount = (ushort)reader.GetInt16(14),
                    PrimaryHQ = reader.GetBoolean(15),

                    Additional = (uint)reader.GetInt32(16),
                    AdditionalCount = (ushort)reader.GetInt16(17),
                    AdditionalHQ = reader.GetBoolean(18),

                    Unlocked = (uint)reader.GetInt32(19),
                    Date = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(20)).DateTime,
                    Valid = reader.GetBoolean(21),
                };
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, "Unable to read loot entry from database");
                continue;
            }

            yield return loot;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

internal class SubmarineReader(DbDataReader reader) : IEnumerable<Submarine>
{
    public IEnumerator<Submarine> GetEnumerator()
    {
        while (reader.Read())
        {
            Submarine sub;
            try
            {
                var returnTime = (uint)reader.GetInt32(2);
                sub = new Submarine
                {
                    FreeCompanyId = MessagePackSerializer.Deserialize<ulong>(reader.GetFieldValue<byte[]>(0)),
                    Register = (uint)reader.GetInt32(1),
                    Return = returnTime,
                    ReturnTime = DateTime.UnixEpoch.AddSeconds(returnTime),

                    Name = reader.GetString(3),
                    Rank = (ushort)reader.GetInt16(4),
                    Points = MessagePackSerializer.Deserialize<List<uint>>(reader.GetFieldValue<byte[]>(5)),
                    Hull = (ushort)reader.GetInt16(6),
                    Stern = (ushort)reader.GetInt16(7),
                    Bow = (ushort)reader.GetInt16(8),
                    Bridge = (ushort)reader.GetInt16(9),

                    CExp = (uint)reader.GetInt32(10),
                    NExp = (uint)reader.GetInt32(11),

                    HullDurability = (ushort)reader.GetInt16(12),
                    SternDurability = (ushort)reader.GetInt16(13),
                    BowDurability = (ushort)reader.GetInt16(14),
                    BridgeDurability = (ushort)reader.GetInt16(15)
                };
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, "Unable to read submarine entry from database");
                continue;
            }

            yield return sub;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

internal class FCReader(DbDataReader reader) : IEnumerable<FreeCompany>
{
    public IEnumerator<FreeCompany> GetEnumerator()
    {
        while (reader.Read())
        {
            FreeCompany fc;
            try
            {
                fc = new FreeCompany
                {
                    FreeCompanyId = MessagePackSerializer.Deserialize<ulong>(reader.GetFieldValue<byte[]>(0)),
                    Tag = reader.GetString(1),
                    World = reader.GetString(2),
                    CharacterName = reader.GetString(3),

                    UnlockedSectors = MessagePackSerializer.Deserialize<Dictionary<uint, bool>>(reader.GetFieldValue<byte[]>(4)),
                    ExploredSectors = MessagePackSerializer.Deserialize<Dictionary<uint, bool>>(reader.GetFieldValue<byte[]>(5))
                };
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, "Unable to read FC entry from database");
                continue;
            }

            yield return fc;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
