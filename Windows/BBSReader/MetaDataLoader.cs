using BBSReader.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BBSReader
{
    class MetaDataLoader
    {
        public static MetaData Load()
        {
            MetaData metaData = new MetaData();
            string metaPath = Constants.LOCAL_PATH + "meta/";

            string timestampPath = metaPath + "timestamp.json";
            using (StreamReader sr = new StreamReader(timestampPath, Encoding.UTF8))
            {
                string json = sr.ReadToEnd();
                metaData.timestamp = JsonConvert.DeserializeObject<long>(json);
            }

            string threadsDir = metaPath + "threads/";
            var threadsFs = Directory.GetFiles(threadsDir, "*.json");
            Array.Sort(threadsFs, (x, y) => (int.Parse(Path.GetFileNameWithoutExtension(x).Split('-').First()) - int.Parse(Path.GetFileNameWithoutExtension(y).Split('-').First())));
            metaData.threads = new List<BBSThread>();
            foreach (string threadsPath in threadsFs)
            {
                using (StreamReader sr = new StreamReader(threadsPath, Encoding.UTF8))
                {
                    string json = sr.ReadToEnd();
                    metaData.threads.AddRange(JsonConvert.DeserializeObject<List<BBSThread>>(json));
                }
            }

            string tagsDir = metaPath + "tags/";
            var tagsFs = Directory.GetFiles(tagsDir, "*.json");
            metaData.tags = new Dictionary<string, List<int>>();
            foreach (string tagsPath in tagsFs)
            {
                using (StreamReader sr = new StreamReader(tagsPath, Encoding.UTF8))
                {
                    string json = sr.ReadToEnd();
                    var dic = JsonConvert.DeserializeObject<Dictionary<string, List<int>>>(json);
                    metaData.tags = metaData.tags.Concat(dic).ToDictionary(k => k.Key, v => v.Value);
                }
            }

            string superkeywordsPath = metaPath + "superkeywords.json";
            using (StreamReader sr = new StreamReader(superkeywordsPath, Encoding.UTF8))
            {
                string json = sr.ReadToEnd();
                metaData.superKeywords = JsonConvert.DeserializeObject<List<SuperKeyword>>(json);
            }

            string blacklistPath = metaPath + "blacklist.json";
            using (StreamReader sr = new StreamReader(blacklistPath, Encoding.UTF8))
            {
                string json = sr.ReadToEnd();
                metaData.blacklist = JsonConvert.DeserializeObject<List<string>>(json);
            }

            Grouper.GroupingSuperKeywords(metaData);
            return metaData;
        }

        public static void Save(MetaData metaData)
        {
            string superkeywordsPath = Constants.LOCAL_PATH + "meta/superkeywords.json";
            using (StreamWriter sw = new StreamWriter(superkeywordsPath, false, new UTF8Encoding(false)))
            {
                string json = JsonConvert.SerializeObject(metaData.superKeywords);
                sw.Write(json);
            }

            string blacklistPath = Constants.LOCAL_PATH + "meta/blacklist.json";
            using (StreamWriter sw = new StreamWriter(blacklistPath, false, new UTF8Encoding(false)))
            {
                string json = JsonConvert.SerializeObject(metaData.blacklist);
                sw.Write(json);
            }
        }

    }
}
