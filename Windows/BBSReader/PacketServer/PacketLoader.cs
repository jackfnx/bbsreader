﻿using BBSReader.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BBSReader.PacketServer
{
    class PacketLoader
    {
        public static List<Packet> LoadPackets()
        {
            string metaPath = Constants.LOCAL_PATH + "meta.json";
            using (StreamReader sr = new StreamReader(metaPath, Encoding.UTF8))
            {
                string json = sr.ReadToEnd();
                MetaData metaData = JsonConvert.DeserializeObject<MetaData>(json);
                Grouper.GroupingSuperKeyword(metaData);
                List<Packet> list = new List<Packet>();
                foreach (SuperKeyword sk in metaData.superKeywords)
                {
                    Packet packet = new Packet();
                    if (sk.simple)
                        packet.title = sk.keyword;
                    else if (sk.keyword == "*")
                        packet.title = string.Format("【{0}】的作品集", sk.authors[0]);
                    else if (sk.authors[0] == "*")
                        packet.title = string.Format("专题：【{0}】", sk.keyword);
                    else
                        packet.title = string.Format("【{0}】系列", sk.keyword);
                    packet.author = sk.authors[0];
                    packet.simple = sk.simple;
                    packet.chapters = new List<PackChapter>();
                    foreach (Group g in sk.groupedTids)
                    {
                        BBSThread example = metaData.threads[g.exampleId];
                        PackChapter ch = new PackChapter();
                        ch.title = example.title;
                        ch.author = example.author;
                        ch.source = Constants.SITE_DEF[example.siteId].siteName;
                        ch.filename = example.siteId + "/" + example.threadId;
                        ch.time = example.postTime;
                        packet.chapters.Add(ch);
                    }
                    packet.key = CalcKey(packet.title, packet.author, sk.simple, packet.chapters);
                    list.Add(packet);
                }
                return list;
            }
        }
        
        private static string CalcKey(string title, string author, bool simple, List<PackChapter> chapters)
        {
            string chaptersFileName = string.Join(":", chapters.ConvertAll(x => x.filename));
            string rawSign = string.Format("{0}/{1}/{2}:{3}", title, author, simple, chaptersFileName);
            return GenerateMD5(rawSign);
        }

        private static string GenerateMD5(string s)
        {
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            byte[] data = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(s));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
    }
}
