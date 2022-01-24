using BBSReader.Data;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace BBSReader.PacketServer
{
    class PacketLoader
    {
        public static List<Packet> LoadPackets()
        {
            List<Packet> list = new List<Packet>();
            MetaData metaData = MetaDataLoader.Load();
            Grouper.GroupingSuperKeyword(metaData);
            foreach (SuperKeyword sk in metaData.superKeywords)
            {
                Packet packet = new Packet();
                if (sk.skType == SKType.Simple)
                    packet.title = sk.keyword;
                else if (sk.skType == SKType.Manual)
                {
                    if (sk.keyword == "*")
                        packet.title = "《独立篇章合集》";
                    else
                        packet.title = string.Format("静态：【{0}】", sk.keyword);
                }
                else if (sk.skType == SKType.Author)
                    packet.title = string.Format("【{0}】的作品集", sk.authors[0]);
                else if (sk.skType == SKType.Advanced)
                {
                    if (sk.authors[0] == "*")
                        packet.title = string.Format("专题：【{0}】", sk.keyword);
                    else
                        packet.title = string.Format("【{0}】系列", sk.keyword);
                }
                packet.author = sk.authors[0];
                packet.simple = sk.skType != SKType.Author;
                packet.chapters = new List<Chapter>();
                foreach (Group g in sk.groupedTids)
                {
                    BBSThread example = metaData.threads[g.exampleId];
                    string filename = example.siteId + "/" + example.threadId;
                    Chapter ch = new Chapter();
                    ch.id = filename;
                    ch.title = example.title;
                    ch.author = example.author;
                    ch.source = Constants.SITE_DEF[example.siteId].siteName;
                    ch.savePath = filename;
                    ch.timestamp = Utils.GetTimestamp(example.postTime);
                    packet.chapters.Add(ch);
                }
                packet.chapters.Sort((x1, x2) => x2.timestamp.CompareTo(x1.timestamp));
                packet.timestamp = packet.chapters[0].timestamp;
                packet.key = Utils.CalcKey(packet.title, packet.author, sk.skType != SKType.Author);
                packet.summary = Utils.CalcSumary(packet.title, packet.author, sk.skType != SKType.Author, packet.chapters, null);
                packet.source = "Forum";
                list.Add(packet);
            }
            list.Sort((x1, x2) => x2.timestamp.CompareTo(x1.timestamp));
            string packetFolder = Constants.LOCAL_PATH + "packets";
            foreach (string f in Directory.EnumerateFiles(packetFolder))
            {
                Packet packet = LoadPacketFromZip(f);
                list.Add(packet);
            }
            return list;
        }

        private static Packet LoadPacketFromZip(string f)
        {
            using (ZipArchive za = ZipFile.OpenRead(f))
            {
                ZipArchiveEntry metaEntry = FindEntryFromZip(za, ".META.json");
                ZipArchiveEntry chaptersEntry = FindEntryFromZip(za, ".CONTENT");
                Packet packet = LoadFromZae<Packet>(metaEntry);
                packet.chapters = LoadFromZae<List<Chapter>>(chaptersEntry);
                return packet;
            }
        }

        private static ZipArchiveEntry FindEntryFromZip(ZipArchive za, string name)
        {
            foreach (ZipArchiveEntry zae in za.Entries)
            {
                if (zae.FullName == name)
                {
                    return zae;
                }
            }
            return null;
        }

        private static T LoadFromZae<T>(ZipArchiveEntry zae)
        {
            Stream s = zae.Open();
            using (MemoryStream ms = new MemoryStream())
            {
                s.CopyTo(ms);
                byte[] bytes = ms.ToArray();
                string json = Encoding.UTF8.GetString(bytes);
                return JsonConvert.DeserializeObject<T>(json);
            }
        }

    }
}
