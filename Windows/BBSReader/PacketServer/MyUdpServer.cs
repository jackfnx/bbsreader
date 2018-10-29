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

        private readonly int port;

        public bool isRunning;

        public event EventHandler ServerStarted;

        public MyUdpServer(int port)
        {
            this.port = port;
        }
        
        private bool FindServer(int retry)
        {
            using (Socket udpListener = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                udpListener.ReceiveTimeout = 5000;
                udpListener.Bind(new IPEndPoint(IPAddress.Any, port));

                for (int i = 0; i < retry; i++)
                {
                    try
                    {
                        EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                        byte[] buffer = new byte[1024];
                        int length = udpListener.ReceiveFrom(buffer, ref remote);
                        string message = Encoding.UTF8.GetString(buffer, 1, length - 1);
                        if (message == CODES_WORD)
                        {
                            return true;
                        }
                    }
                    catch (SocketException e)
                    {
                        continue;
                    }
                }
                return false;
            }
        }

        public void UdpThread()
        {
            bool iamServer = false;
            Socket broadcastSocket = null;

            while (isRunning)
            {
                if (!iamServer)
                {
                    if (!FindServer(3))
                    {
                        iamServer = true;
                    }
                }

                if (iamServer)
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

            if (iamServer && broadcastSocket != null)
            {
                broadcastSocket.Close();
            }
        }
    }
}
