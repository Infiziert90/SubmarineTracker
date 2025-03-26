// ReSharper disable ExplicitCallerInfoArgument

using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Newtonsoft.Json;

namespace SubmarineTracker.Data;

public static class Export
{
    private const string BaseUrl = "https://infi.ovh/api/";
    private const string AnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyb2xlIjoiYW5vbiJ9.Ur6wgi_rD4dr3uLLvbLoaEvfLCu4QFWdrF-uHRtbl_s";
    private static readonly HttpClient Client = new();

    private static readonly CsvConfiguration CsvConfig = new(CultureInfo.InvariantCulture) { HasHeaderRecord = false };
    private static readonly CsvConfiguration CsvReadConfig = new(CultureInfo.InvariantCulture) { HasHeaderRecord = true, PrepareHeaderForMatch = args => args.Header.Replace("_", "").ToLower() };

    static Export()
    {
        Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {AnonKey}");
        Client.DefaultRequestHeaders.Add("Prefer", "return=minimal");
    }

    public class Upload
    {
        [Ignore]
        [JsonIgnore]
        public string Table;

        [JsonProperty("version")]
        public string Version = Plugin.PluginInterface.Manifest.AssemblyVersion.ToString();

        public Upload(string table)
        {
            Table = table;
        }
    }

    public class Loot : Upload
    {
        [JsonProperty("sector")]
        public uint Sector { get; set; }

        [JsonProperty("unlocked")]
        public uint Unlocked { get; set; }

        [JsonProperty("primary")]
        public uint Primary { get; set; }
        [JsonProperty("primary_count")]
        public ushort PrimaryCount { get; set; }
        [JsonProperty("additional")]
        public uint Additional { get; set; }
        [JsonProperty("additional_count")]
        public ushort AdditionalCount { get; set; }

        [JsonProperty("rank")]
        public int Rank { get; set; }
        [JsonProperty("surv")]
        public int Surv { get; set; }
        [JsonProperty("ret")]
        public int Ret { get; set; }
        [JsonProperty("fav")]
        public int Fav { get; set; }

        [JsonProperty("primary_surv_proc")]
        public uint PrimarySurvProc { get; set; }
        [JsonProperty("additional_surv_proc")]
        public uint AdditionalSurvProc { get; set; }
        [JsonProperty("primary_ret_proc")]
        public uint PrimaryRetProc { get; set; }
        [JsonProperty("fav_proc")]
        public uint FavProc { get; set; }

        [Format("s")]
        [JsonIgnore]
        public DateTime Date { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; } = "";

        // Used by CSV import
        public Loot() : base("Loot") {}

        public Loot(SubmarineTracker.Loot loot) : base("Loot")
        {
            Sector = loot.Sector;
            Unlocked = loot.Unlocked;

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
        public ExportLootMap(bool ignoreDate = false, bool ignoreHash = false)
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

            Map(m => m.Unlocked).Ignore();
        }
    }

    public class SubNotify : Upload
    {
        [JsonProperty("webhook")]
        public string Webhook;

        [JsonProperty("content")]
        public string Content;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("mention")]
        public ulong Mention;

        [JsonProperty("role_mention")]
        public ulong RoleMention;

        [JsonProperty("return_time")]
        public uint ReturnTime;

        public SubNotify(string webhook, string content, string name, ulong mention, ulong roleMention, uint returnTime) : base("SubNotify")
        {
            Webhook = webhook;
            Mention = mention;
            RoleMention = roleMention;
            ReturnTime = returnTime;
            Name = name;
            Content = content;
        }
    }

    public static string ExportToString(List<SubmarineTracker.Loot> fcLootList, bool excludeDate, bool excludeHash)
    {
        if (fcLootList.Count == 0)
            return string.Empty;

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
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Error while exporting to string");
            return string.Empty;
        }
    }

    // For internal debug only
    private static readonly HashSet<string> LootHashes = [];
    public static IEnumerable<Loot> Import(FileInfo file)
    {
        using var reader = file.OpenText();
        using var csv = new CsvReader(reader, CsvReadConfig);

        csv.Context.RegisterClassMap(new ExportLootMap(true));
        foreach (var loot in csv.GetRecords<Loot>())
        {
            if (LootHashes.Add(loot.Hash))
                yield return loot;
        }
    }

    public static async void UploadLoot(SubmarineTracker.Loot newLoot)
    {
        try
        {
            var lootEntry = new Loot(newLoot);
            var content = new StringContent(JsonConvert.SerializeObject(lootEntry), Encoding.UTF8, "application/json");
            var response = await Client.PostAsync($"{BaseUrl}{lootEntry.Table}", content);

            if (response.StatusCode != HttpStatusCode.Created)
                Plugin.Log.Debug($"Table {lootEntry.Table} | Content: {response.Content.ReadAsStringAsync().Result}");

            Plugin.Log.Debug($"Sector {newLoot.Sector} | StatusCode {response.StatusCode.ToString()}");
            Plugin.Log.Debug($"Sector {newLoot.Sector} | Content {response.Content.ReadAsStringAsync().Result}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Upload failed!");
        }
    }

    public static async void UploadNotify(SubNotify notify)
    {
        try
        {
            var content = new StringContent(JsonConvert.SerializeObject(notify), Encoding.UTF8, "application/json");
            var response = await Client.PostAsync($"{BaseUrl}{notify.Table}", content);

            if (response.StatusCode != HttpStatusCode.Created)
                Plugin.Log.Debug($"Table {notify.Table} | Content: {response.Content.ReadAsStringAsync().Result}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Upload failed!");
        }
    }
}
