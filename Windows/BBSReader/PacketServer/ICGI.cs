using System.Net;

namespace BBSReader.PacketServer
{
    interface ICGI
    {
        void Execute(HttpListenerResponse response, params object[] paras);
    }
}
