using Newtonsoft.Json;

namespace BBSReader.Data
{
    struct Chapter
    {
        [JsonProperty]
        public string id;
        [JsonProperty]
        public string title;
        [JsonProperty]
        public string author;
        [JsonProperty]
        public string source;
        [JsonProperty]
        public string savePath;
        [JsonProperty]
        public long timestamp;
    }
}
