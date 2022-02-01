using Newtonsoft.Json;
using System.Collections.Generic;

namespace BBSReader.Data
{
    public struct SuperKeyword
    {
        [JsonProperty("skType")]
        public string skType;
        [JsonProperty("keyword")]
        public string keyword;
        [JsonProperty("author")]
        public List<string> authors;
        [JsonProperty("alias")]
        public List<string> alias;
        [JsonProperty("subKeywords")]
        public List<List<string>> subKeywords;
        [JsonProperty("tids")]
        public List<int> tids;
        [JsonProperty("groups")]
        public List<List<string>> groups;
        [JsonProperty("kws")]
        public List<List<int>> kws;
        [JsonProperty("read")]
        public int read;
        [JsonProperty("subReads")]
        public List<int> subReads;
        [JsonIgnore]
        public List<Group> groupedTids;
        [JsonIgnore]
        public List<SuperKeyword> subSKs;
        [JsonIgnore]
        public List<Group> noSKGTids;
    }

}
