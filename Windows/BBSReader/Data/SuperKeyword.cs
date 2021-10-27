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
        [JsonProperty("tids")]
        public List<int> tids;
        [JsonProperty("read")]
        public int read;
        [JsonProperty("groups")]
        public List<List<string>> groups;
        [JsonIgnore]
        public List<Group> groupedTids;
    }

}
