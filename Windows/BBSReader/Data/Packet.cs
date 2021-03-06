﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace BBSReader.Data
{
    struct Packet
    {
        [JsonProperty]
        public string title;
        [JsonProperty]
        public string author;
        [JsonProperty]
        public bool simple;
        [JsonProperty]
        public string key;
        [JsonProperty]
        public string summary;
        [JsonProperty]
        public string source;
        [JsonProperty]
        public long timestamp;
        [JsonProperty]
        public List<string> regexps;
        public List<Chapter> chapters;
    }
}
