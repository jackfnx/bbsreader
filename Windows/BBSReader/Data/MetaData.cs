using Newtonsoft.Json;
using System.Collections.Generic;

namespace BBSReader.Data
{
    public struct MetaData
    {
        [JsonProperty("timestamp")]
        public long timestamp;
        [JsonProperty("threads")]
        public List<BBSThread> threads;
        [JsonProperty("tags")]
        public Dictionary<string, List<int>> tags;
        [JsonProperty("superkeywords")]
        public List<SuperKeyword> superKeywords;
        [JsonProperty("blacklist")]
        public List<string> blacklist;
        [JsonProperty("manualTags")]
        public Dictionary<string, List<string>> manualTags;
    }

}
