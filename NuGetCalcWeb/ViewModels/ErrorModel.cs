using Newtonsoft.Json;

namespace NuGetCalcWeb.ViewModels
{
    public class ErrorModel
    {
        [JsonProperty("error")]
        public string Header { get; set; }

        [JsonProperty("detail")]
        public string Detail { get; set; }
    }
}