using System.Net;

namespace BBSReader.PacketServer
{
    class MyHttpServer
    {
        private readonly int port;

        private ICGI cgi404;
        private ICGI cgiDoc;
        private ICGI cgiCon;

        public bool isRunning;

        public MyHttpServer(int port)
        {
            this.port = port;
            isRunning = false;
            cgi404 = new CGI404();
            cgiDoc = new CGIDoc();
            cgiCon = new CGICon();
        }

        public void HttpServerThread()
        {
            HttpListener server = new HttpListener();
            string prefix = string.Format("http://*:{0}/", port);
            server.Prefixes.Add(prefix);
            server.Start();

            while (isRunning)
            {
                HttpListenerContext context = server.GetContext();
                HttpListenerResponse response = context.Response;

                string path = context.Request.Url.LocalPath;
                string[] paras = path.Split('/');

                if (paras.Length <= 1)
                {
                    cgi404.Execute(response);
                }
                else if (paras[1] == "books")
                {
                    cgiDoc.Execute(response);
                }
                else if (paras[1] == "book" && paras.Length > 2)
                {
                    cgiCon.Execute(response, paras[2]);
                }
                else
                {
                    cgi404.Execute(response);
                }

                response.Close();
            }

            server.Stop();
        }
    }
}
