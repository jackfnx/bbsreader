using BBSReader.Data;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace BBSReader.PacketServer
{
    class CGIDoc : ICGI
    {
        private List<Packet> packets;

        public void Execute(HttpListenerResponse response, params object[] paras)
        {
            packets = PacketLoader.LoadPackets();
            JsonSerializerSettings jss = new JsonSerializerSettings();
            jss.ContractResolver = new LimitPropsContractResolver(new string[]{ "chapters" }, false);
            string json = JsonConvert.SerializeObject(packets, jss);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
    }
}
