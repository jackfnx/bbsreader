using System;
using System.Threading;

namespace BBSReader.PacketServer
{
    class MyServer
    {
        const int PORT = 5000;
        const int UDP_PORT = 4999;
        private static MyServer instance = new MyServer();
        public static MyServer GetInstance()
        {
            return instance;
        }

        private MyServer()
        {
            httpServer = new MyHttpServer(PORT);
            udpServer = new MyUdpServer(UDP_PORT);
            udpServer.ServerStarted += UdpServer_ServerStarted;
        }

        private void UdpServer_ServerStarted(object sender, EventArgs e)
        {
            if (!httpServer.isRunning)
            {
                httpServerThread = new Thread(httpServer.HttpServerThread);
                httpServer.isRunning = true;
                httpServerThread.Start();
            }
        }

        private MyHttpServer httpServer;
        private MyUdpServer udpServer;
        private Thread httpServerThread;
        private Thread udpThread;

        public void Start()
        {
            udpThread = new Thread(udpServer.UdpThread);
            udpServer.isRunning = true;
            udpThread.Start();
        }

        public void Stop()
        {
            udpServer.isRunning = false;
            httpServer.isRunning = false;
        }

    }
}
