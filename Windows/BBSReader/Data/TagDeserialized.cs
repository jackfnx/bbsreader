using Newtonsoft.Json;
using System.Collections.Generic;

namespace BBSReader.Data
{
    class TagDeserialized
    {
#pragma warning disable 0649
        [JsonProperty("tag")]
        public string tag;
        [JsonProperty("tids")]
        public List<int> tids;
#pragma warning restore 0649
    }
}
