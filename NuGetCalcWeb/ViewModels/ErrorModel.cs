using Newtonsoft.Json;

namespace NuGetCalcWeb.ViewModels
{
    public class ErrorModel
    {
        [JsonIgnore]
        public int StatusCode { get; set; }

        [JsonProperty("error")]
        public string Header { get; set; }

        [JsonProperty("detail")]
        public string Detail { get; set; }
    }
}