using BBSReader.Data;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace BBSReader.PacketServer
{
    class CGICon : ICGI
    {
        public void Execute(HttpListenerResponse response, params object[] paras)
        {
            string key = paras[0] as string;
            List<Packet> packets = PacketLoader.LoadPackets();
            if (!packets.Exists(x => x.key == key))
            {
                response.StatusCode = 404;
                return;
            }

            Packet packet = packets.Find(x => x.key == key);
            string fileName = packet.key + ".zip";
            byte[] packetData = ZipPacket(packet);

            response.ContentType = "application/zip";
            response.AddHeader("Content-Disposition", "attachment;FileName=" + fileName);
            response.ContentLength64 = packetData.Length;
            response.OutputStream.Write(packetData, 0, packetData.Length);
        }

        private static byte[] ZipPacket(Packet packet)
        {
            string metaJson = JsonConvert.SerializeObject(packet);
            using (MemoryStream ms = new MemoryStream())
            {
                using (ZipArchive zip = new ZipArchive(ms, ZipArchiveMode.Create))
                {
                    ZipArchiveEntry meta = zip.CreateEntry(".META.json");
                    using (StreamWriter sw = new StreamWriter(meta.Open()))
                    {
                        sw.Write(metaJson);
                    }
                    foreach (PackChapter ch in packet.chapters)
                    {
                        ZipArchiveEntry zae = zip.CreateEntry(ch.filename);
                        string src = string.Format("{0}/{1}.txt", Constants.LOCAL_PATH, ch.filename);
                        using (FileStream ins = new FileStream(src, FileMode.Open))
                        using (Stream outs = zae.Open())
                        {
                            ins.CopyTo(outs);
                        }
                    }
                }
                
                return ms.ToArray();
            }
        }
    }
}
