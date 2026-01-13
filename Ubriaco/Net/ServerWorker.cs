/*---------------------------------------------------------------------------------------------
 *  Copyright (c) by Alessandro Ubriaco. All rights reserved.
 *--------------------------------------------------------------------------------------------*/
 
 

using System.Net;
using System.Net.Sockets;

namespace Ubriaco.Net
{
    public abstract class ServerWorker
    {
        private TcpListener Listener;
        private bool Stopped = false;
        private int ThreadCount = 0;

        public ServerWorker(int port)
        {
            Listener = new TcpListener(new IPEndPoint(IPAddress.Any, port));
        }

        public void Start()
        {
            Stopped = false;
            Listener.Start();
            while (!Stopped)
            {
                if (!Listener.Pending())
                    Thread.Sleep(10);
                var client = Listener.AcceptTcpClient();
                ThreadCount++;
                new Thread(() => RunNode(client)).Start();
            }
            Listener.Stop();
        }

        public void StartThread()
        {
            new Thread(() => Start());
        }

        public void Stop()
        {
            Stopped = true;
        }

        public void RunNode(TcpClient client)
        {
            try
            {
                NodeEntry(client);
                client.Dispose();
                ThreadCount--;
            }
            catch(Exception e)
            {
                client.Dispose();
                ThreadCount--;
                throw new Exception("RunNode exception: " + e.Message);
            }
        }

        public abstract void NodeEntry(TcpClient client);
    }
}
