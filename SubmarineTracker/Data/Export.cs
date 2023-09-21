// ReSharper disable ExplicitCallerInfoArgument

using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Dalamud.Logging;
using Postgrest;
using Postgrest.Attributes;
using Postgrest.Models;
using static SubmarineTracker.Data.Loot;

namespace SubmarineTracker.Data;

public static class Export
{
    private const string SupabaseUrl = "https://xzwnvwjxgmaqtrxewngh.supabase.co";
    private const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Inh6d252d2p4Z21hcXRyeGV3bmdoIiwicm9sZSI6ImFub24iLCJpYXQiOjE2ODk3NzcwMDIsImV4cCI6MjAwNTM1MzAwMn0.aNYTnhY_Sagi9DyH5Q9tCz9lwaRCYzMC12SZ7q7jZBc";

    private static readonly Supabase.Client Client;
    private static readonly CsvConfiguration CsvConfig = new(CultureInfo.InvariantCulture) { HasHeaderRecord = false };

    static Export()
    {
        Client = new Supabase.Client(SupabaseUrl, SupabaseAnonKey);
    }

    [Table("Loot")]
    public class Loot : BaseModel
    {
        [Column("sector")]
        public uint Sector { get; set; }

        [Column("primary")]
        public uint Primary { get; set; }
        [Column("primary_count")]
        public ushort PrimaryCount { get; set; }
        [Column("additional")]
        public uint Additional { get; set; }
        [Column("additional_count")]
        public ushort AdditionalCount { get; set; }

        [Column("rank")]
        public int Rank { get; set; }
        [Column("surv")]
        public int Surv { get; set; }
        [Column("ret")]
        public int Ret { get; set; }
        [Column("fav")]
        public int Fav { get; set; }

        [Column("primary_surv_proc")]
        public uint PrimarySurvProc { get; set; }
        [Column("additional_surv_proc")]
        public uint AdditionalSurvProc { get; set; }
        [Column("primary_ret_proc")]
        public uint PrimaryRetProc { get; set; }
        [Column("fav_proc")]
        public uint FavProc { get; set; }

        [Format("s")]
        [Column(ignoreOnInsert: true, ignoreOnUpdate: true)]
        public DateTime Date { get; set; }

        [Column("hash")]
        public string Hash { get; set; } = "";

        public Loot() {}

        public Loot(DetailedLoot loot)
        {
            Sector = loot.Sector;

            Primary = loot.Primary;
            PrimaryCount = loot.PrimaryCount;
            Additional = loot.Additional;
            AdditionalCount = loot.AdditionalCount;

            Rank = loot.Rank;
            Surv = loot.Surv;
            Ret = loot.Ret;
            Fav = loot.Fav;

            PrimarySurvProc = loot.PrimarySurvProc;
            AdditionalSurvProc = loot.AdditionalSurvProc;
            PrimaryRetProc = loot.PrimaryRetProc;
            FavProc = loot.FavProc;
            Date = loot.Date;

            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(Date.Ticks);
                writer.Write(Sector);
            }
            stream.Position = 0;

            using (var hash = SHA256.Create())
            {
                var result = hash.ComputeHash(stream);
                Hash = string.Join("", result.Select(b => $"{b:X2}"));
            }
        }
    }

    public sealed class ExportLootMap : ClassMap<Loot>
    {
        public ExportLootMap(bool ignoreDate, bool ignoreHash)
        {
            Map(m => m.Sector).Index(0).Name("Sector");
            Map(m => m.Primary).Index(1).Name("Primary");
            Map(m => m.PrimaryCount).Index(2).Name("PrimaryCount");
            Map(m => m.Additional).Index(3).Name("Additional");
            Map(m => m.AdditionalCount).Index(4).Name("AdditionalCount");
            Map(m => m.Rank).Index(5).Name("Rank");
            Map(m => m.Surv).Index(6).Name("Surv");
            Map(m => m.Ret).Index(7).Name("Ret");
            Map(m => m.Fav).Index(8).Name("Fav");
            Map(m => m.PrimarySurvProc).Index(9).Name("PrimarySurvProc");
            Map(m => m.AdditionalSurvProc).Index(10).Name("AdditionalSurvProc");
            Map(m => m.PrimaryRetProc).Index(11).Name("PrimaryRetProc");
            Map(m => m.FavProc).Index(12).Name("FavProc");

            if (ignoreDate)
                Map(m => m.Date).Ignore();
            else
                Map(m => m.Date).Index(13).Name("Date");

            if (ignoreHash)
                Map(m => m.Hash).Ignore();
            else
                Map(m => m.Hash).Index(99).Name("Hash");
        }
    }

    public static string ExportToString(List<DetailedLoot> fcLootList, bool excludeDate, bool excludeHash)
    {
        try
        {
            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, CsvConfig);

            csv.Context.RegisterClassMap(new ExportLootMap(excludeDate, excludeHash));

            csv.WriteHeader<Loot>();
            csv.NextRecord();

            foreach (var detailedLoot in fcLootList)
            {
                csv.WriteRecord(new Loot(detailedLoot));
                csv.NextRecord();
            }

            return writer.ToString();
        }
        catch (Exception e)
        {
            PluginLog.Error(e.StackTrace ?? "No Stacktrace");
            Plugin.ChatGui.Print(Utils.ErrorMessage($"{e.Message}. For further information /xllog."));

            return string.Empty;
        }
    }

    public static async void UploadFullExport(List<DetailedLoot> fcLootList)
    {
        var s = ExportToString(fcLootList, true, false);
        if (s != string.Empty)
        {
            try
            {
                await Client.InitializeAsync();
                var bucket = Client.Storage.From("Loot Data");
                var result = await bucket.Upload(Encoding.UTF8.GetBytes(s), $"{DateTime.Now.Ticks}_dump.csv");

                PluginLog.Debug(result);
            }
            catch (Exception e)
            {
                PluginLog.Error(e.Message);
                PluginLog.Error(e.StackTrace ?? "No Stacktrace");
            }
        }
    }

    public static async void UploadEntry(DetailedLoot newLoot)
    {
        var lootEntry = new Loot(newLoot);
        try
        {
            await Client.InitializeAsync();
            var result = await Client.From<Loot>().Insert(lootEntry, new QueryOptions {
                Returning = QueryOptions.ReturnType.Minimal
            });

            PluginLog.Debug(result.ResponseMessage?.StatusCode.ToString() ?? "Unknown");
            PluginLog.Debug(result.Content ?? "None");
        }
        catch (Exception e)
        {
            PluginLog.Error(e.Message);
            PluginLog.Error(e.StackTrace ?? "No Stacktrace");
        }
    }
}
