using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WebServer
{
	internal class HttpServer
	{
		private ManualResetEvent signal;
		private TcpListener listener;

		public HttpServer(IPAddress ip, int port, ManualResetEvent signal) { 
			listener = new TcpListener(ip, port);
			this.signal = signal;
		}
		public void Listen() {
			listener.Start();

			do
			{
				var client = listener.AcceptTcpClient();
				Console.WriteLine($"Incoming request from: {client.Client.RemoteEndPoint}");
				Thread.Sleep(1000);
			} 
			while (!signal.WaitOne());

			listener.Stop();
		}
	}
}
