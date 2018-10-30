using Newtonsoft.Json;

namespace BBSReader.Data
{
    struct PackChapter
    {
        [JsonProperty]
        public string title;
        [JsonProperty]
        public string author;
        [JsonProperty]
        public string source;
        [JsonProperty]
        public string filename;
        [JsonProperty]
        public long timestamp;
    }
}
