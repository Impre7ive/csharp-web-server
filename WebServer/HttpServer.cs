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
		private List<Task> tasks = new List<Task>();

		public HttpServer(IPAddress ip, int port, ManualResetEvent signal) { 
			listener = new TcpListener(ip, port);
			this.signal = signal;
		}
		public void Listen() {
			listener.Start();

			do
			{
				var client = listener.AcceptTcpClient();						
				Task.Run(() => HandleHttpsRequest(client));
				Thread.Sleep(50);
			} 
			while (!signal.WaitOne(0));

			listener.Stop();
		}

		private int HandleHttpsRequest(TcpClient client)
		{
			NetworkStream stream = client.GetStream();
			var buffer = new byte[1024];
			var bytesRead = stream.Read(buffer, 0, buffer.Length);
			var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
			
			var requestHeaders = Parse(request);
			var response = getResponse(requestHeaders);

			sendResponse(stream, response);
			client.Close();

			return 0;
		}

		private void sendResponse(NetworkStream stream, string response)
		{
			var responseBuffer = Encoding.UTF8.GetBytes(response);
			stream.Write(responseBuffer, 0, responseBuffer.Length);
		}

		private string getResponse(Dictionary<string, string> requestHeaders)
		{
			string response = "HTTP/1.1 200 OK\r\nContent-Type: text/html\r\n\r\n";
			response += "<html><body>Hello from Server!</body></html>";

			return response;
		}

		private Dictionary<string, string> Parse(string request)
		{
			return new Dictionary<string, string> {
				{"Path", "Mock"}
			};
		}
	}
}
