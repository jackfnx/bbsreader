using System;
using System.ComponentModel;
using System.Threading;

namespace BBSReader.PacketServer
{
    class MyServer : INotifyPropertyChanged
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
                PropertyChanged(this, e: new PropertyChangedEventArgs("IsRunning"));
            }
        }

        public bool IsRunning
        {
            get
            {
                return httpServer.isRunning;
            }
        }

        private MyHttpServer httpServer;
        private MyUdpServer udpServer;
        private Thread httpServerThread;
        private Thread udpServerThread;

        public event PropertyChangedEventHandler PropertyChanged;

        public void Start()
        {
            udpServerThread = new Thread(udpServer.UdpThread);
            udpServer.isRunning = true;
            udpServerThread.Start();
        }

        public void Stop()
        {
            udpServer.isRunning = false;
            httpServer.isRunning = false;
            if (httpServer.server != null)
            {
                httpServer.server.Abort();
            }
            PropertyChanged(this, e: new PropertyChangedEventArgs("IsRunning"));
        }

    }
}
