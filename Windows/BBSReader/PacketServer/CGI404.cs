using System.Net;

namespace BBSReader.PacketServer
{
    internal class CGI404 : ICGI
    {
        public void Execute(HttpListenerResponse response, params object[] paras)
        {
            response.StatusCode = 404;
        }
    }
}