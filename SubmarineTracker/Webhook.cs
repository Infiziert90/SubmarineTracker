using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;
using Newtonsoft.Json;

namespace SubmarineTracker;

public static class Webhook
{
    private static Configuration Configuration = null!;
    private static readonly HttpClient Client = new();

    public struct WebhookContent
    {
        [JsonProperty("username")] public string Username = "[Submarine Tracker]";
        [JsonProperty("avatar_url")] public string AvatarUrl ="https://raw.githubusercontent.com/Infiziert90/SubmarineTracker/master/SubmarineTracker/images/icon.png";
        [JsonProperty("embeds")] public List<object> Embeds = new();

        public WebhookContent() { }
    }

    public static void Init(Configuration configuration)
    {
        Configuration = configuration;
    }

    public static void PostMessage(WebhookContent webhookContent)
    {
        Task.Run(async () =>
        {
            try
            {
                var response = await Client.PostAsync(Configuration.WebhookUrl,new StringContent(JsonConvert.SerializeObject(webhookContent), Encoding.UTF8, "application/json"));
                if (!response.IsSuccessStatusCode)
                {
                    PluginLog.Error(response.StatusCode.ToString());
                    PluginLog.Error(response.Content.ReadAsStringAsync().Result);
                }
            }
            catch (Exception e)
            {
                PluginLog.Error("Webhook post failed");
                PluginLog.Error(e.Message);
            }
        });
    }
}
