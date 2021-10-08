using System.ComponentModel;
using System.Threading;

namespace BBSReader.PacketServer
{
    class MyServer : INotifyPropertyChanged
    {
        const int PORT = 5000;

        private static MyServer instance = new MyServer();
        public static MyServer GetInstance()
        {
            return instance;
        }

        private MyServer()
        {
            httpServer = new MyHttpServer(PORT);
        }

        public bool IsRunning
        {
            get
            {
                return httpServer.isRunning;
            }
        }

        private MyHttpServer httpServer;
        private Thread httpServerThread;

        public event PropertyChangedEventHandler PropertyChanged;

        public void Start()
        {
            httpServerThread = new Thread(httpServer.HttpServerThread);
            httpServer.isRunning = true;
            httpServerThread.Start();
            PropertyChanged(this, e: new PropertyChangedEventArgs("IsRunning"));
        }

        public void Stop()
        {
            httpServer.isRunning = false;
            if (httpServer.server != null)
            {
                httpServer.server.Abort();
            }
            PropertyChanged(this, e: new PropertyChangedEventArgs("IsRunning"));
        }

    }
}
