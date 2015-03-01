using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LightNode.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Versioning;

namespace NuGetCalcWeb
{
    public class InternalApi : LightNodeContract
    {
        public struct AutocompleteResult
        {
            public AutocompleteResult(string value)
            {
                this.Value = value;
            }

            [JsonProperty("value")]
            public string Value;
        }

        public async Task<IEnumerable<AutocompleteResult>> Autocomplete(string q)
        {
            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(
                    "https://api-v3search-0.nuget.org/autocomplete?prerelease=true&q="
                    + Uri.EscapeDataString(q)
                ).ConfigureAwait(false);
                return JObject.Parse(json)["data"]
                    .Select(x => new AutocompleteResult((string)x));
            }
        }

        public async Task<IEnumerable<AutocompleteResult>> VersionAutocomplete(string package)
        {
            using (var client = new HttpClient())
            {
                var json = await client.GetStringAsync(
                    string.Format("https://api.nuget.org/v3/registration0/{0}/index.json", package.ToLowerInvariant())
                ).ConfigureAwait(false);

                return JObject.Parse(json)["items"]
                    .SelectMany(x => x["items"].Select(item => (string)item["catalogEntry"]["version"]))
                    .OrderByDescending(x => new NuGetVersion(x))
                    .Select(x => new AutocompleteResult(x));
            }
        }
    }
}