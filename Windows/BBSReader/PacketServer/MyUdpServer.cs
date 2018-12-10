using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace BBSReader.PacketServer
{
    class MyUdpServer
    {
        const string CODES_WORD = "Naive Reader Pack Server RUNNING.";
        const int STARTUP_TIMEOUT = 5000;
        const int STARTUP_RETRY = 3;

        private readonly int port;

        public bool isRunning;

        public event EventHandler ServerStarted;

        private bool findServer;

        public MyUdpServer(int port)
        {
            this.port = port;
            findServer = false;
        }
        
        public void UdpListenThread()
        {
            using (Socket udpListener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                udpListener.ReceiveTimeout = STARTUP_TIMEOUT;
                udpListener.Bind(new IPEndPoint(IPAddress.Any, port));

                int error = 0;
                while (isRunning)
                {
                    try
                    {
                        EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                        byte[] buffer = new byte[1024];
                        int length = udpListener.ReceiveFrom(buffer, ref remote);
                        string message = Encoding.UTF8.GetString(buffer, 1, length - 1);
                        if (message == CODES_WORD)
                        {
                            error = 0;
                            findServer = true;
                        }
                        else
                        {
                            error++;
                        }
                    }
                    catch (SocketException)
                    {
                        error++;
                        continue;
                    }
                    if (error > 1000)
                    {
                        findServer = false;
                    }
                }
            }
        }

        public void UdpBroadcastThread()
        {
            Socket broadcastSocket = null;

            Thread.Sleep(STARTUP_TIMEOUT * STARTUP_RETRY);

            while (isRunning)
            {
                if (!findServer)
                {
                    if (broadcastSocket == null)
                    {
                        broadcastSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                        broadcastSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                    }

                    IPEndPoint iep = new IPEndPoint(IPAddress.Broadcast, port);
                    byte[] bytes = Encoding.UTF8.GetBytes(CODES_WORD);
                    byte[] buffer = new byte[bytes.Length + 1];
                    buffer[0] = (byte)bytes.Length;
                    bytes.CopyTo(buffer, 1);
                    broadcastSocket.SendTo(buffer, iep);

                    ServerStarted(this, EventArgs.Empty);
                }

                Thread.Sleep(1000);
            }

            if (broadcastSocket != null)
            {
                broadcastSocket.Close();
            }
        }
    }
}
