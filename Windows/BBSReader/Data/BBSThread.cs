using Newtonsoft.Json;

namespace BBSReader.Data
{
    public struct BBSThread
    {
        [JsonProperty("siteId")]
        public string siteId;
        [JsonProperty("threadId")]
        public string threadId;
        [JsonProperty("title")]
        public string title;
        [JsonProperty("author")]
        public string author;
        [JsonProperty("postTime")]
        public string postTime;
        [JsonProperty("link")]
        public string link;
    }

}
